using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Consents;
using RowingClub.Scheduling.Domain.Logs;

namespace RowingClub.Scheduling.Application.Members;

public sealed record ConsentStateDto(
    string Key, string Title, string Body, string Scope, bool Required, string Icon,
    bool Accepted, DateTimeOffset? AcceptedAtUtc, string? IpAddress);

/// <summary>
/// Beyan kataloğu + (CustomerId verilirse) üyenin güncel onay durumları.
/// Kayıtsız (misafir) taraf için CustomerId=null: yalnızca katalog döner, her randevuda yeniden
/// onaylatılır. Kayıtlı üye için bir kez onay yeterlidir.
/// </summary>
public sealed record GetConsentStatusQuery(Guid? CustomerId) : IRequest<List<ConsentStateDto>>;

public sealed record ConsentEntry(string Key, bool Accepted);

/// <summary>Üyenin beyan onaylarını kaydeder (tarih + IP ile). İsteğe bağlı rızalar geri çekilebilir.</summary>
public sealed record SubmitConsentsCommand(Guid CustomerId, List<ConsentEntry> Entries, string? IpAddress)
    : ICommand<List<ConsentStateDto>>;

public sealed class GetConsentStatusQueryHandler(IConsentRecordRepository repository)
    : IRequestHandler<GetConsentStatusQuery, List<ConsentStateDto>>
{
    public async Task<List<ConsentStateDto>> Handle(
        GetConsentStatusQuery request, CancellationToken cancellationToken)
    {
        var latest = request.CustomerId is { } customerId
            ? await repository.GetLatestByCustomerAsync(customerId, cancellationToken)
            : new Dictionary<string, ConsentRecord>();

        return ConsentCatalog.All.Select(def =>
        {
            latest.TryGetValue(def.Key, out var record);
            return new ConsentStateDto(
                def.Key, def.Title, def.Body, def.Scope.ToString(), def.Required, def.Icon,
                record?.Accepted ?? false, record?.AcceptedAtUtc, record?.IpAddress);
        }).ToList();
    }
}

public sealed class SubmitConsentsCommandHandler(
    IConsentRecordRepository repository,
    IMemberLogRepository memberLogRepository,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<SubmitConsentsCommand, List<ConsentStateDto>>
{
    public async Task<List<ConsentStateDto>> Handle(
        SubmitConsentsCommand request, CancellationToken cancellationToken)
    {
        foreach (var entry in request.Entries)
        {
            var def = ConsentCatalog.Find(entry.Key)
                ?? throw new DomainException("unknown_consent", $"Bilinmeyen beyan: {entry.Key}");

            if (def.Required && !entry.Accepted)
                throw new DomainException("consent_required", $"'{def.Title}' zorunlu bir beyandır; reddedilemez.");

            repository.Add(ConsentRecord.Create(request.CustomerId, entry.Key, entry.Accepted, request.IpAddress));
        }

        if (request.Entries.Count > 0)
        {
            memberLogRepository.Add(MemberLog.Record(
                request.CustomerId, "CONSENTS_SUBMITTED",
                string.Join(", ", request.Entries.Select(e => $"{e.Key}={(e.Accepted ? "kabul" : "ret")}"))));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var latest = await repository.GetLatestByCustomerAsync(request.CustomerId, cancellationToken);
        return ConsentCatalog.All.Select(def =>
        {
            latest.TryGetValue(def.Key, out var record);
            return new ConsentStateDto(
                def.Key, def.Title, def.Body, def.Scope.ToString(), def.Required, def.Icon,
                record?.Accepted ?? false, record?.AcceptedAtUtc, record?.IpAddress);
        }).ToList();
    }
}

/// <summary>Randevu/kayıt akışlarının ortak beyan denetimi.</summary>
public static class ConsentGuard
{
    /// <summary>
    /// Gerekli beyanların tamamının (mevcut kayıtlar + bu istekle gelenler) kabul edildiğini
    /// doğrular; eksikse hangi beyanların gerektiğini söyleyen hata fırlatır. Gelen onayları
    /// tarih+IP ile kayıt eder.
    /// </summary>
    public static async Task EnforceAsync(
        IConsentRecordRepository repository,
        Guid customerId,
        ConsentScope scope,
        IReadOnlyCollection<string> acceptedNow,
        string? ipAddress,
        bool trustExisting,
        CancellationToken cancellationToken)
    {
        var required = ConsentCatalog.All
            .Where(c => c.Scope == scope && c.Required)
            .Select(c => c.Key)
            .ToList();

        var existing = trustExisting
            ? await repository.GetLatestByCustomerAsync(customerId, cancellationToken)
            : new Dictionary<string, ConsentRecord>();

        var missing = required
            .Where(key => !acceptedNow.Contains(key) &&
                          !(existing.TryGetValue(key, out var r) && r.Accepted))
            .ToList();

        if (missing.Count > 0)
        {
            var titles = string.Join(", ", missing.Select(k => ConsentCatalog.Find(k)!.Title));
            throw new DomainException("consents_required", $"Devam etmek için şu beyanları onaylamalısınız: {titles}");
        }

        foreach (var key in acceptedNow.Where(k => ConsentCatalog.Find(k) is not null))
        {
            // Kayıtlı üyede zaten kabul edilmişse yeniden yazma (tarih korunur).
            if (trustExisting && existing.TryGetValue(key, out var r) && r.Accepted)
                continue;

            repository.Add(ConsentRecord.Create(customerId, key, accepted: true, ipAddress));
        }
    }
}

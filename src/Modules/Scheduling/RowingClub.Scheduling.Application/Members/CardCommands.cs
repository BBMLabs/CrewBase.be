using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Cards;
using RowingClub.Scheduling.Domain.Logs;

namespace RowingClub.Scheduling.Application.Members;

public sealed record CardDto(
    string Type,
    string? CardNumber,
    string CompanyName,
    string? ExpiryDate,
    string Status,
    bool HasPhoto,
    string? PhotoBase64,
    string? PhotoContentType,
    DateTimeOffset UpdatedAtUtc);

/// <summary>Üyenin kartları; IncludePhotos=false listede fotoğraf yükünü taşımaz.</summary>
public sealed record GetMyCardsQuery(Guid CustomerId, bool IncludePhotos) : IRequest<List<CardDto>>;

/// <summary>
/// Multisport/Meditopia kartını üye KENDİSİ kaydeder/günceller (tür başına tek kart).
/// Multisport'ta kart numarası zorunlu; Meditopia'da yalnızca şirket adı.
/// </summary>
public sealed record UpsertCardCommand(
    Guid CustomerId,
    string Type,
    string? CardNumber,
    string CompanyName,
    string? ExpiryDate,
    string Status,
    string? PhotoBase64,
    string? PhotoContentType) : ICommand<CardDto>;

public sealed class GetMyCardsQueryHandler(IMembershipCardRepository repository)
    : IRequestHandler<GetMyCardsQuery, List<CardDto>>
{
    public async Task<List<CardDto>> Handle(GetMyCardsQuery request, CancellationToken cancellationToken)
    {
        var cards = await repository.GetByCustomerAsync(request.CustomerId, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        return cards.Select(c => new CardDto(
            c.Type.ToString(),
            c.CardNumber,
            c.CompanyName,
            c.ExpiryDate?.ToString("yyyy-MM-dd"),
            c.EffectiveStatus(today).ToString(),
            c.PhotoBase64 is not null,
            request.IncludePhotos ? c.PhotoBase64 : null,
            c.PhotoContentType,
            c.UpdatedAtUtc)).ToList();
    }
}

public sealed class UpsertCardCommandHandler(
    IMembershipCardRepository repository,
    IMemberLogRepository memberLogRepository,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<UpsertCardCommand, CardDto>
{
    public async Task<CardDto> Handle(UpsertCardCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<MembershipCardType>(request.Type, ignoreCase: true, out var type))
            throw new DomainException("invalid_card_type", "Üyelik türü Multisport veya Meditopia olmalıdır.");

        if (!Enum.TryParse<MembershipCardStatus>(request.Status, ignoreCase: true, out var status) ||
            status == MembershipCardStatus.Expired)
        {
            status = MembershipCardStatus.Active;
        }

        DateOnly? expiry = null;
        if (!string.IsNullOrWhiteSpace(request.ExpiryDate))
        {
            if (!DateOnly.TryParseExact(request.ExpiryDate, "yyyy-MM-dd", out var parsed))
                throw new DomainException("invalid_date", "Geçerlilik tarihi YYYY-AA-GG biçiminde olmalıdır.");
            expiry = parsed;
        }

        var card = await repository.GetAsync(request.CustomerId, type, cancellationToken);
        if (card is null)
        {
            card = MembershipCard.Create(request.CustomerId, type);
            repository.Add(card);
        }

        card.Update(request.CardNumber, request.CompanyName, expiry, status,
            request.PhotoBase64, request.PhotoContentType);

        memberLogRepository.Add(MemberLog.Record(
            request.CustomerId, "CARD_UPDATED", $"{type} - {card.CompanyName}"));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        return new CardDto(
            card.Type.ToString(), card.CardNumber, card.CompanyName,
            card.ExpiryDate?.ToString("yyyy-MM-dd"), card.EffectiveStatus(today).ToString(),
            card.PhotoBase64 is not null, null, card.PhotoContentType, card.UpdatedAtUtc);
    }
}

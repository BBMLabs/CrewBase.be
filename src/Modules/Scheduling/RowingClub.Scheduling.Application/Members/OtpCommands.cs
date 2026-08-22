using System.Security.Cryptography;
using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Logs;

namespace RowingClub.Scheduling.Application.Members;

/// <summary>OTP iletimi kompozisyon katmanında yapılır (e-posta; SMS sağlayıcısı takılabilir).</summary>
public interface IOtpSender
{
    Task SendAsync(string emailTo, string purposeLabel, string code, CancellationToken cancellationToken);
}

/// <summary>E-posta veya telefon doğrulaması için 6 haneli kod üretip gönderir.</summary>
public sealed record RequestMemberOtpCommand(Guid CustomerId, string Purpose) : ICommand<Unit>;

public sealed record VerifyMemberOtpCommand(Guid CustomerId, string Purpose, string Code) : ICommand<MemberDto>;

internal static class Otp
{
    public static string GenerateCode() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    public static VerificationPurpose ParsePurpose(string raw) => raw.Trim().ToLowerInvariant() switch
    {
        "email" or "e-posta" => VerificationPurpose.Email,
        "phone" or "telefon" => VerificationPurpose.Phone,
        _ => throw new DomainException("invalid_purpose", "Doğrulama türü email veya phone olmalıdır."),
    };
}

public sealed class RequestMemberOtpCommandHandler(
    ICustomerRepository customerRepository,
    IVerificationCodeRepository codeRepository,
    IRefreshTokenHasher hasher,
    IOtpSender otpSender,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<RequestMemberOtpCommand, Unit>
{
    public async Task<Unit> Handle(RequestMemberOtpCommand request, CancellationToken cancellationToken)
    {
        var purpose = Otp.ParsePurpose(request.Purpose);

        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException("Customer", request.CustomerId.ToString());

        if (customer.Email is null)
            throw new DomainException("email_required", "Doğrulama kodu için önce e-posta adresi eklemelisiniz.");

        // Aktif eski kod varsa geçersiz kılınır; her istek yeni kod üretir.
        var existing = await codeRepository.GetActiveAsync(customer.Id, purpose, cancellationToken);
        if (existing is not null)
            codeRepository.Remove(existing);

        var code = Otp.GenerateCode();
        codeRepository.Add(VerificationCode.Issue(customer.Id, purpose, hasher.Hash(code)));

        // SMS sağlayıcısı bağlanana kadar telefon kodu da kayıtlı e-postaya gönderilir.
        var label = purpose == VerificationPurpose.Email ? "E-posta Doğrulama" : "Telefon Doğrulama";
        await otpSender.SendAsync(customer.Email, label, code, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public sealed class VerifyMemberOtpCommandHandler(
    ICustomerRepository customerRepository,
    IVerificationCodeRepository codeRepository,
    IMemberLogRepository memberLogRepository,
    IRefreshTokenHasher hasher,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<VerifyMemberOtpCommand, MemberDto>
{
    public async Task<MemberDto> Handle(VerifyMemberOtpCommand request, CancellationToken cancellationToken)
    {
        var purpose = Otp.ParsePurpose(request.Purpose);

        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException("Customer", request.CustomerId.ToString());

        var verification = await codeRepository.GetActiveAsync(customer.Id, purpose, cancellationToken)
            ?? throw new DomainException("otp_not_found", "Aktif bir doğrulama kodu yok; yeni kod isteyin.");

        var matched = verification.TryConsume(hasher.Hash(request.Code.Trim()));
        if (!matched)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken); // deneme sayacını kalıcıla
            throw new DomainException("otp_invalid", "Kod hatalı. Lütfen tekrar deneyin.");
        }

        customer.MarkVerified(purpose);
        memberLogRepository.Add(MemberLog.Record(
            customer.Id, purpose == VerificationPurpose.Email ? "EMAIL_VERIFIED" : "PHONE_VERIFIED"));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return RegisterMemberCommandHandler.ToDto(customer);
    }
}

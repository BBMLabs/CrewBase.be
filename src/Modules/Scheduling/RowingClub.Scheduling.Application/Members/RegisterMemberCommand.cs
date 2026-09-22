using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Branches;
using RowingClub.Scheduling.Domain.Consents;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Logs;

namespace RowingClub.Scheduling.Application.Members;

public sealed record RegisterMemberCommand(
    string FullName, string Phone, string Email,
    List<string> AcceptedConsents, string? IpAddress, string? BranchCode,
    string CompanyName, string Subdomain) : ICommand<MemberDto>;

public sealed class RegisterMemberCommandValidator : AbstractValidator<RegisterMemberCommand>
{
    public RegisterMemberCommandValidator()
    {
        RuleFor(c => c.FullName).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Phone).NotEmpty().MaximumLength(20);
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(254);
    }
}

public sealed class RegisterMemberCommandHandler(
    ICustomerRepository customerRepository,
    IBranchRepository branchRepository,
    IConsentRecordRepository consentRepository,
    IMemberLogRepository memberLogRepository,
    IMemberPasswordSetupTokenRepository setupTokenRepository,
    IOpaqueTokenGenerator tokenGenerator,
    IRefreshTokenHasher tokenHasher,
    IMemberWelcomeEmailSender welcomeEmailSender,
    ISchedulingUnitOfWork unitOfWork,
    ITenantDatabase tenantDatabase,
    ILogger<RegisterMemberCommandHandler> logger)
    : IRequestHandler<RegisterMemberCommand, MemberDto>
{
    public async Task<MemberDto> Handle(RegisterMemberCommand request, CancellationToken cancellationToken)
    {
        if (await customerRepository.GetByEmailAsync(request.Email, cancellationToken) is not null)
            throw new DomainException("email_taken", "Bu e-posta adresiyle bir üyelik zaten var.");

        Guid? branchId = null;
        if (!string.IsNullOrWhiteSpace(request.BranchCode))
        {
            var branch = await branchRepository.GetByCodeAsync(request.BranchCode, cancellationToken);
            if (branch is null || !branch.IsActive)
                throw new DomainException("invalid_branch", "Seçilen şube bulunamadı.");
            branchId = branch.Id;
        }

        var customer = await customerRepository.GetByPhoneAsync(request.Phone, cancellationToken);
        if (customer is null)
        {
            if (await customerRepository.CountAsync(cancellationToken) >= tenantDatabase.MaxMembers)
                throw new DomainException("member_limit_reached",
                    $"Üye limitine ulaşıldı. Mevcut paketiniz en fazla {tenantDatabase.MaxMembers} üyeye izin verir; devam etmek için kulübünüz paketini yükseltmelidir.");

            customer = Customer.Create(request.FullName, request.Phone, request.Email, branchId);
            customerRepository.Add(customer);
        }
        else if (customer.HasAccount)
        {
            throw new DomainException("member_exists", "Bu telefon numarasıyla bir üyelik zaten var. Giriş yapın.");
        }
        else
        {
            customer.UpdateContact(request.FullName, request.Email);
            if (branchId is not null)
                customer.SetBranch(branchId);
        }

        await ConsentGuard.EnforceAsync(
            consentRepository, customer.Id, ConsentScope.Member,
            request.AcceptedConsents, request.IpAddress, trustExisting: true, cancellationToken);

        await MemberCodeAssigner.EnsureCodeAsync(customer, customerRepository, cancellationToken);
        memberLogRepository.Add(MemberLog.Record(customer.Id, MemberEvents.Registered));

        var rawSetupToken = tokenGenerator.Generate();
        setupTokenRepository.Add(
            MemberPasswordSetupToken.Issue(customer.Id, tokenHasher.Hash(rawSetupToken), TimeSpan.FromDays(7)));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await welcomeEmailSender.SendRegistrationSetupAsync(
                customer.Email!, customer.FullName, request.CompanyName, rawSetupToken, request.Subdomain,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Üye şifre oluşturma e-postası gönderilemedi: {CustomerId}", customer.Id);
        }

        return ToDto(customer);
    }

    internal static MemberDto ToDto(Customer c) => new(
        c.Id, c.FullName, c.Phone, c.Email, c.Level, RowingLevels.LabelOf(c.Level),
        c.MemberCode, c.EmailVerifiedAtUtc is not null, c.PhoneVerifiedAtUtc is not null,
        c.DefaultReminderMinutes, c.CreatedAtUtc);
}

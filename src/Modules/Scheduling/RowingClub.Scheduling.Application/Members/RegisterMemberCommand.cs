using FluentValidation;
using MediatR;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Passwords;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Branches;
using RowingClub.Scheduling.Domain.Consents;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Logs;

namespace RowingClub.Scheduling.Application.Members;

public sealed record RegisterMemberCommand(
    string FullName, string Phone, string Email, string Password,
    List<string> AcceptedConsents, string? IpAddress, string? BranchCode) : ICommand<MemberDto>;

public sealed class RegisterMemberCommandValidator : AbstractValidator<RegisterMemberCommand>
{
    public RegisterMemberCommandValidator()
    {
        RuleFor(c => c.FullName).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Phone).NotEmpty().MaximumLength(20);
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(c => c.Password).MinimumLength(8).MaximumLength(128);
    }
}

public sealed class RegisterMemberCommandHandler(
    ICustomerRepository customerRepository,
    IBranchRepository branchRepository,
    IConsentRecordRepository consentRepository,
    IMemberLogRepository memberLogRepository,
    IPasswordHasher passwordHasher,
    ISchedulingUnitOfWork unitOfWork,
    ITenantDatabase tenantDatabase)
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

        // Üyelik beyanları (KVKK, sağlık verisi vb.): zorunlular kabul edilmiş olmalı.
        await ConsentGuard.EnforceAsync(
            consentRepository, customer.Id, ConsentScope.Member,
            request.AcceptedConsents, request.IpAddress, trustExisting: true, cancellationToken);

        customer.AttachAccount(request.Email, passwordHasher.Hash(request.Password));
        await MemberCodeAssigner.EnsureCodeAsync(customer, customerRepository, cancellationToken);
        memberLogRepository.Add(MemberLog.Record(customer.Id, MemberEvents.Registered));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(customer);
    }

    internal static MemberDto ToDto(Customer c) => new(
        c.Id, c.FullName, c.Phone, c.Email, c.Level, RowingLevels.LabelOf(c.Level),
        c.MemberCode, c.EmailVerifiedAtUtc is not null, c.PhoneVerifiedAtUtc is not null,
        c.DefaultReminderMinutes, c.CreatedAtUtc);
}

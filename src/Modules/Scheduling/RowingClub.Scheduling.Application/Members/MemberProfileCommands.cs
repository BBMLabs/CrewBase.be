using FluentValidation;
using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Passwords;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Logs;

namespace RowingClub.Scheduling.Application.Members;

public sealed record GetMemberProfileQuery(Guid CustomerId) : IRequest<MemberDto>;

/// <summary>Profil + bildirim tercihi güncelleme (DefaultReminderMinutes: null=firma varsayılanı, 0=istemez).</summary>
public sealed record UpdateMemberProfileCommand(
    Guid CustomerId, string FullName, string? Email, int? DefaultReminderMinutes) : ICommand<MemberDto>;

public sealed class UpdateMemberProfileCommandValidator : AbstractValidator<UpdateMemberProfileCommand>
{
    public UpdateMemberProfileCommandValidator()
    {
        RuleFor(c => c.FullName).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Email).EmailAddress().MaximumLength(254).When(c => !string.IsNullOrWhiteSpace(c.Email));
    }
}

/// <summary>
/// Üye hesabını KALICI siler (hard delete): üye kaydı, randevuları, paket bakiyeleri ve logları
/// tenant veritabanından fiziksel olarak kaldırılır. Geri dönüşü yoktur; şifre doğrulaması şarttır.
/// </summary>
public sealed record DeleteMemberAccountCommand(Guid CustomerId, string Password) : ICommand<Unit>;

public sealed record RecordMemberActivityCommand(Guid CustomerId) : ICommand<Unit>;

public sealed class GetMemberProfileQueryHandler(ICustomerRepository customerRepository)
    : IRequestHandler<GetMemberProfileQuery, MemberDto>
{
    public async Task<MemberDto> Handle(GetMemberProfileQuery request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException("Customer", request.CustomerId.ToString());

        return RegisterMemberCommandHandler.ToDto(customer);
    }
}

public sealed class UpdateMemberProfileCommandHandler(
    ICustomerRepository customerRepository,
    IMemberLogRepository memberLogRepository,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<UpdateMemberProfileCommand, MemberDto>
{
    public async Task<MemberDto> Handle(UpdateMemberProfileCommand request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException("Customer", request.CustomerId.ToString());

        if (!string.IsNullOrWhiteSpace(request.Email) &&
            !string.Equals(request.Email.Trim(), customer.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await customerRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (existing is not null && existing.Id != customer.Id)
                throw new DomainException("email_taken", "Bu e-posta adresi başka bir üyede kayıtlı.");
        }

        customer.UpdateContact(request.FullName, request.Email);
        customer.SetDefaultReminder(request.DefaultReminderMinutes);

        memberLogRepository.Add(MemberLog.Record(customer.Id, MemberEvents.ProfileUpdated));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return RegisterMemberCommandHandler.ToDto(customer);
    }
}

public sealed class DeleteMemberAccountCommandHandler(
    ICustomerRepository customerRepository,
    IPasswordHasher passwordHasher,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<DeleteMemberAccountCommand, Unit>
{
    public async Task<Unit> Handle(DeleteMemberAccountCommand request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException("Customer", request.CustomerId.ToString());

        if (customer.PasswordHash is null || !passwordHasher.Verify(request.Password, customer.PasswordHash))
            throw new AuthenticationFailedException("Şifre hatalı; hesap silinmedi.");

        // Hard delete: randevular/paketler/loglar FK cascade ile birlikte silinir.
        customerRepository.Remove(customer);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public sealed class RecordMemberActivityCommandHandler(
    ICustomerRepository customerRepository,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<RecordMemberActivityCommand, Unit>
{
    public async Task<Unit> Handle(RecordMemberActivityCommand request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException("Customer", request.CustomerId.ToString());

        customer.Touch();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Passwords;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Branches;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Logs;

namespace RowingClub.Scheduling.Application.Members;

public sealed record MemberLoginCommand(string Email, string Password) : ICommand<MemberDto>;

public sealed class MemberLoginCommandHandler(
    ICustomerRepository customerRepository,
    IBranchRepository branchRepository,
    IMemberLogRepository memberLogRepository,
    IPasswordHasher passwordHasher,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<MemberLoginCommand, MemberDto>
{
    public async Task<MemberDto> Handle(MemberLoginCommand request, CancellationToken cancellationToken)
    {
        // Tek tip hata: e-postanın kayıtlı olup olmadığı sızdırılmaz (user enumeration önlemi).
        var invalid = new AuthenticationFailedException("E-posta veya şifre hatalı.");

        var customer = await customerRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (customer is null || customer.PasswordHash is null)
            throw invalid;

        customer.EnsureCanAuthenticate();

        if (customer.BranchId is { } branchId)
        {
            var branch = await branchRepository.GetByIdAsync(branchId, cancellationToken);
            if (branch is not null && !branch.IsActive)
                throw new DomainException("branch_inactive", "Şubeniz şu anda hizmet dışıdır. Lütfen kulübünüzle iletişime geçin.");
        }

        if (!passwordHasher.Verify(request.Password, customer.PasswordHash))
            throw invalid;

        memberLogRepository.Add(MemberLog.Record(customer.Id, MemberEvents.Login));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return RegisterMemberCommandHandler.ToDto(customer);
    }
}

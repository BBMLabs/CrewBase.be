using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Passwords;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Logs;

namespace RowingClub.Scheduling.Application.Members;

public sealed record MemberLoginCommand(string Email, string Password) : ICommand<MemberDto>;

public sealed class MemberLoginCommandHandler(
    ICustomerRepository customerRepository,
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

        if (!passwordHasher.Verify(request.Password, customer.PasswordHash))
            throw invalid;

        memberLogRepository.Add(MemberLog.Record(customer.Id, MemberEvents.Login));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return RegisterMemberCommandHandler.ToDto(customer);
    }
}

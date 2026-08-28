using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Customers;

namespace RowingClub.Scheduling.Application.Panel;

/// <summary>Üyenin derecesini belirler (0 = derecesiz). Seans gruplaması bu dereceye göre yapılır.</summary>
public sealed record SetCustomerLevelCommand(Guid CustomerId, int Level) : ICommand<CustomerDto>;

public sealed class SetCustomerLevelCommandHandler(
    ICustomerRepository customerRepository,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<SetCustomerLevelCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(SetCustomerLevelCommand request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException("Customer", request.CustomerId.ToString());

        customer.SetLevel(request.Level);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CustomerDto(
            customer.Id, customer.FullName, customer.Phone, customer.Email, customer.Level,
            customer.BranchId, customer.IsBlocked, customer.CreatedAtUtc, customer.MemberCode);
    }
}

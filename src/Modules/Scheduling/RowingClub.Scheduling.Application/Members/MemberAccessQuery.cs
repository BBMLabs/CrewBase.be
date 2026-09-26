using MediatR;
using RowingClub.Scheduling.Domain.Customers;

namespace RowingClub.Scheduling.Application.Members;

/// <summary>
/// Üye token'ı hâlâ geçerli bir üyeye mi ait? Silinmiş ya da kulüp tarafından engellenmiş üyenin
/// elindeki (en fazla 15 dk ömürlü) access token'ı süresi dolana kadar tam erişim sağlıyordu; her üye
/// isteğinde bu kontrol edilir.
/// </summary>
public sealed record GetMemberAccessQuery(Guid CustomerId) : IRequest<bool>;

public sealed class GetMemberAccessQueryHandler(ICustomerRepository customerRepository)
    : IRequestHandler<GetMemberAccessQuery, bool>
{
    public async Task<bool> Handle(GetMemberAccessQuery request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        return customer is { IsBlocked: false, HasAccount: true };
    }
}

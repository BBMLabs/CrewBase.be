using FluentAssertions;
using NSubstitute;
using RowingClub.Scheduling.Application.Members;
using RowingClub.Scheduling.Domain.Customers;

namespace RowingClub.UnitTests.Scheduling.Application.Members;

public sealed class GetMemberAccessQueryHandlerTests
{
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();

    private GetMemberAccessQueryHandler CreateHandler() => new(_customerRepository);

    private static Customer CreateMember()
    {
        var customer = Customer.Create("Ada Lovelace", "+905551112233", "ada@example.com");
        customer.AttachAccount("ada@example.com", "hash");
        return customer;
    }

    private Task<bool> Access(Customer? customer)
    {
        var id = customer?.Id ?? Guid.NewGuid();
        _customerRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(customer);
        return CreateHandler().Handle(new GetMemberAccessQuery(id), CancellationToken.None);
    }

    [Fact]
    public async Task Active_member_with_an_account_has_access() =>
        (await Access(CreateMember())).Should().BeTrue();

    [Fact]
    public async Task Blocked_member_loses_access_even_with_a_valid_token()
    {
        // Regresyon: engellenen üyenin token'ı süresi dolana kadar tüm üye uçlarına erişebiliyordu.
        var member = CreateMember();
        member.Block();

        (await Access(member)).Should().BeFalse();
    }

    [Fact]
    public async Task Deleted_member_has_no_access() =>
        (await Access(null)).Should().BeFalse();

    [Fact]
    public async Task Customer_without_an_account_has_no_access() =>
        (await Access(Customer.Create("Misafir", "+905550000000", null))).Should().BeFalse();
}

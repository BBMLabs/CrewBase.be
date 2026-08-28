using FluentAssertions;
using NSubstitute;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Passwords;
using RowingClub.Scheduling.Application.Members;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Branches;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Logs;

namespace RowingClub.UnitTests.Scheduling.Application.Members;

public sealed class MemberLoginCommandHandlerTests
{
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IMemberLogRepository _memberLogRepository = Substitute.For<IMemberLogRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ISchedulingUnitOfWork _unitOfWork = Substitute.For<ISchedulingUnitOfWork>();

    private MemberLoginCommandHandler CreateHandler() =>
        new(_customerRepository, _branchRepository, _memberLogRepository, _passwordHasher, _unitOfWork);

    private Customer CreateCustomer(Guid? branchId)
    {
        var customer = Customer.Create("Ada Lovelace", "+905551112233", "ada@example.com", branchId);
        customer.AttachAccount("ada@example.com", "hashed-password");
        return customer;
    }

    [Fact]
    public async Task Login_fails_when_customers_branch_is_inactive()
    {
        var branch = Branch.Create("968847RG", "Şube 1", null, null);
        branch.Update(branch.Name, branch.Address, branch.Phone, isActive: false,
            branch.ManagerName, branch.ManagerPhone, branch.ManagerEmail, branch.TaxNumber, branch.Description);
        var customer = CreateCustomer(branch.Id);

        _customerRepository.GetByEmailAsync(customer.Email!, Arg.Any<CancellationToken>()).Returns(customer);
        _branchRepository.GetByIdAsync(branch.Id, Arg.Any<CancellationToken>()).Returns(branch);

        var handler = CreateHandler();
        var act = () => handler.Handle(new MemberLoginCommand(customer.Email!, "irrelevant"), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<DomainException>();
        exception.Which.ErrorCode.Should().Be("branch_inactive");

        _passwordHasher.DidNotReceive().Verify(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Login_succeeds_when_customer_has_no_branch()
    {
        var customer = CreateCustomer(branchId: null);

        _customerRepository.GetByEmailAsync(customer.Email!, Arg.Any<CancellationToken>()).Returns(customer);
        _passwordHasher.Verify("correct-password", customer.PasswordHash!).Returns(true);

        var handler = CreateHandler();
        var result = await handler.Handle(new MemberLoginCommand(customer.Email!, "correct-password"), CancellationToken.None);

        result.CustomerId.Should().Be(customer.Id);
        await _branchRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_succeeds_when_customers_branch_is_active()
    {
        var branch = Branch.Create("008976VO", "Şube 2", null, null);
        var customer = CreateCustomer(branch.Id);

        _customerRepository.GetByEmailAsync(customer.Email!, Arg.Any<CancellationToken>()).Returns(customer);
        _branchRepository.GetByIdAsync(branch.Id, Arg.Any<CancellationToken>()).Returns(branch);
        _passwordHasher.Verify("correct-password", customer.PasswordHash!).Returns(true);

        var handler = CreateHandler();
        var result = await handler.Handle(new MemberLoginCommand(customer.Email!, "correct-password"), CancellationToken.None);

        result.CustomerId.Should().Be(customer.Id);
    }
}

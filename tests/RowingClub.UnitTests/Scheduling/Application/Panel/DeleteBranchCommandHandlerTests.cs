using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Application.Files;
using RowingClub.Scheduling.Application.Members;
using RowingClub.Scheduling.Application.Panel;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Boats;
using RowingClub.Scheduling.Domain.Branches;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Instructors;

namespace RowingClub.UnitTests.Scheduling.Application.Panel;

public sealed class DeleteBranchCommandHandlerTests
{
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IBoatRepository _boatRepository = Substitute.For<IBoatRepository>();
    private readonly IInstructorRepository _instructorRepository = Substitute.For<IInstructorRepository>();
    private readonly IMemberBranchTransferEmailSender _transferEmailSender = Substitute.For<IMemberBranchTransferEmailSender>();
    private readonly IFileStorageService _fileStorage = Substitute.For<IFileStorageService>();
    private readonly ISchedulingUnitOfWork _unitOfWork = Substitute.For<ISchedulingUnitOfWork>();
    private readonly ITenantDatabase _tenantDatabase = Substitute.For<ITenantDatabase>();

    private DeleteBranchCommandHandler CreateHandler() => new(
        _branchRepository, _customerRepository, _boatRepository, _instructorRepository,
        _transferEmailSender, _fileStorage, _unitOfWork, _tenantDatabase, NullLogger<DeleteBranchCommandHandler>.Instance);

    private Branch CreateBranch(string code) => Branch.Create(code, $"Şube {code}", null, null);

    private Customer CreateMember(Guid branchId, string? email = "member@example.com") =>
        Customer.Create("Test Üye", "+905551112233", email, branchId);

    private void SetUpBranch(Branch branch, List<Customer> members, List<Boat> boats, List<Instructor> instructors)
    {
        _branchRepository.GetByIdAsync(branch.Id, Arg.Any<CancellationToken>()).Returns(branch);
        _customerRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(members);
        _boatRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(boats);
        _instructorRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(instructors);
    }

    [Fact]
    public async Task Throws_invalid_member_action_for_unknown_action()
    {
        var branch = CreateBranch("968847RG");
        SetUpBranch(branch, [], [], []);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new DeleteBranchCommand(branch.Id, "burn-it-down", null, "Firma", "firma"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_member_action");
    }

    [Fact]
    public async Task Throws_invalid_target_branch_when_transferring_with_members_and_no_target_selected()
    {
        var branch = CreateBranch("968847RG");
        SetUpBranch(branch, [CreateMember(branch.Id)], [], []);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new DeleteBranchCommand(branch.Id, "transfer", null, "Firma", "firma"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_target_branch");
    }

    [Fact]
    public async Task Throws_target_branch_not_found_when_target_is_inactive()
    {
        var branch = CreateBranch("968847RG");
        var target = CreateBranch("008976VO");
        target.Update(target.Name, target.Address, target.Phone, isActive: false,
            target.ManagerName, target.ManagerPhone, target.ManagerEmail, target.TaxNumber, target.Description);
        SetUpBranch(branch, [CreateMember(branch.Id)], [], []);
        _branchRepository.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new DeleteBranchCommand(branch.Id, "transfer", target.Id, "Firma", "firma"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("target_branch_not_found");
    }

    [Fact]
    public async Task Blocks_transfer_and_reports_exact_count_when_company_is_over_member_capacity()
    {
        var branch = CreateBranch("968847RG");
        var target = CreateBranch("008976VO");
        var members = new List<Customer> { CreateMember(branch.Id), CreateMember(branch.Id) };
        SetUpBranch(branch, members, [], []);
        _branchRepository.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);
        _customerRepository.CountAsync(Arg.Any<CancellationToken>()).Returns(60);
        _tenantDatabase.MaxMembers.Returns(50);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new DeleteBranchCommand(branch.Id, "transfer", target.Id, "Firma", "firma"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("member_capacity_insufficient");
        ex.Which.Message.Should().Contain("2 üye aktarılamıyor");

        // Kapasite aşımı, herhangi bir mutasyondan ÖNCE kontrol edilir - hiçbir DB izi bırakmamalı.
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        _branchRepository.DidNotReceive().Remove(Arg.Any<Branch>());
    }

    [Fact]
    public async Task Transfers_members_hard_deletes_boats_instructors_logo_and_removes_branch()
    {
        var branch = CreateBranch("968847RG");
        branch.SetLogo("/uploads/subeler/968847RG/logo.jpg");
        var target = CreateBranch("008976VO");
        var member = CreateMember(branch.Id);
        var boat = Boat.Create("Tekne 1", BoatClass.Single1x, branch.Id);
        var instructor = Instructor.Create("Eğitmen 1", null, null, branch.Id);
        SetUpBranch(branch, [member], [boat], [instructor]);
        _branchRepository.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);
        _customerRepository.CountAsync(Arg.Any<CancellationToken>()).Returns(1);
        _tenantDatabase.MaxMembers.Returns(50);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new DeleteBranchCommand(branch.Id, "transfer", target.Id, "Firma", "firma"), CancellationToken.None);

        result.TransferredMemberCount.Should().Be(1);
        result.DeletedMemberCount.Should().Be(0);
        result.DeletedBoatCount.Should().Be(1);
        result.DeletedInstructorCount.Should().Be(1);
        member.BranchId.Should().Be(target.Id);
        _boatRepository.Received(1).Remove(boat);
        _instructorRepository.Received(1).Remove(instructor);
        await _fileStorage.Received(1).DeleteAsync("/uploads/subeler/968847RG/logo.jpg", Arg.Any<CancellationToken>());
        _branchRepository.Received(1).Remove(branch);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _transferEmailSender.Received(1).SendAsync(
            member.Email!, member.FullName, "Firma", branch.Name, target.Name, "firma", target.Code, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deletes_members_when_member_action_is_delete()
    {
        var branch = CreateBranch("968847RG");
        var member = CreateMember(branch.Id);
        SetUpBranch(branch, [member], [], []);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new DeleteBranchCommand(branch.Id, "delete", null, "Firma", "firma"), CancellationToken.None);

        result.DeletedMemberCount.Should().Be(1);
        result.TransferredMemberCount.Should().Be(0);
        _customerRepository.Received(1).Remove(member);
        _branchRepository.Received(1).Remove(branch);
        await _transferEmailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}

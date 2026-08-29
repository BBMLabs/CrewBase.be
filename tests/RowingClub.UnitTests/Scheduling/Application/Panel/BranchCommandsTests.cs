using FluentAssertions;
using NSubstitute;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.Scheduling.Application.Panel;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Branches;

namespace RowingClub.UnitTests.Scheduling.Application.Panel;

public sealed class CreateBranchCommandHandlerTests
{
    private readonly IBranchRepository _repository = Substitute.For<IBranchRepository>();
    private readonly ISchedulingUnitOfWork _unitOfWork = Substitute.For<ISchedulingUnitOfWork>();
    private readonly ITenantDatabase _tenantDatabase = Substitute.For<ITenantDatabase>();

    private CreateBranchCommandHandler CreateHandler() => new(_repository, _unitOfWork, _tenantDatabase);

    private static CreateBranchCommand ValidCommand() =>
        new("Merkez Şube", null, null, null, null, null, null, null);

    [Fact]
    public async Task Throws_invalid_name_when_name_is_whitespace()
    {
        _tenantDatabase.MaxBranches.Returns(4);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new CreateBranchCommand("   ", null, null, null, null, null, null, null), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_name");

        _repository.DidNotReceive().Add(Arg.Any<Branch>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_branch_limit_reached_when_active_count_meets_plan_maximum()
    {
        _repository.CountActiveAsync(Arg.Any<CancellationToken>()).Returns(4);
        _tenantDatabase.MaxBranches.Returns(4);

        var handler = CreateHandler();
        var act = () => handler.Handle(ValidCommand(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("branch_limit_reached");

        _repository.DidNotReceive().Add(Arg.Any<Branch>());
    }

    [Fact]
    public async Task Throws_code_generation_failed_when_every_generated_code_already_exists()
    {
        _repository.CountActiveAsync(Arg.Any<CancellationToken>()).Returns(0);
        _tenantDatabase.MaxBranches.Returns(4);
        _repository.ExistsByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var handler = CreateHandler();
        var act = () => handler.Handle(ValidCommand(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("code_generation_failed");

        _repository.DidNotReceive().Add(Arg.Any<Branch>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Creates_branch_with_unique_code_and_persists_when_valid()
    {
        _repository.CountActiveAsync(Arg.Any<CancellationToken>()).Returns(0);
        _tenantDatabase.MaxBranches.Returns(4);
        _repository.ExistsByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.Name.Should().Be("Merkez Şube");
        result.Code.Should().HaveLength(8);
        _repository.Received(1).Add(Arg.Is<Branch>(b => b!.Name == "Merkez Şube"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public sealed class UpdateBranchCommandHandlerTests
{
    private readonly IBranchRepository _repository = Substitute.For<IBranchRepository>();
    private readonly ISchedulingUnitOfWork _unitOfWork = Substitute.For<ISchedulingUnitOfWork>();

    private UpdateBranchCommandHandler CreateHandler() => new(_repository, _unitOfWork);

    [Fact]
    public async Task Throws_not_found_when_branch_does_not_exist()
    {
        var branchId = Guid.NewGuid();
        _repository.GetByIdAsync(branchId, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new UpdateBranchCommand(branchId, "Yeni Ad", null, null, true, null, null, null, null, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Updates_branch_and_persists_when_found()
    {
        var branch = Branch.Create("968847RG", "Eski Ad", null, null);
        _repository.GetByIdAsync(branch.Id, Arg.Any<CancellationToken>()).Returns(branch);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new UpdateBranchCommand(branch.Id, "Yeni Ad", "Adres", "+905551112233", false, null, null, null, null, null),
            CancellationToken.None);

        result.Name.Should().Be("Yeni Ad");
        result.IsActive.Should().BeFalse();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public sealed class SetBranchLogoCommandHandlerTests
{
    private readonly IBranchRepository _repository = Substitute.For<IBranchRepository>();
    private readonly ISchedulingUnitOfWork _unitOfWork = Substitute.For<ISchedulingUnitOfWork>();

    private SetBranchLogoCommandHandler CreateHandler() => new(_repository, _unitOfWork);

    [Fact]
    public async Task Throws_not_found_when_branch_does_not_exist()
    {
        var branchId = Guid.NewGuid();
        _repository.GetByIdAsync(branchId, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new SetBranchLogoCommand(branchId, "/uploads/subeler/x/logo.jpg"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sets_logo_and_persists_when_found()
    {
        var branch = Branch.Create("968847RG", "Şube", null, null);
        _repository.GetByIdAsync(branch.Id, Arg.Any<CancellationToken>()).Returns(branch);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new SetBranchLogoCommand(branch.Id, "/uploads/subeler/x/logo.jpg"), CancellationToken.None);

        result.LogoPath.Should().Be("/uploads/subeler/x/logo.jpg");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

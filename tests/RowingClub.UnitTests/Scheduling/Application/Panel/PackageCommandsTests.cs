using FluentAssertions;
using NSubstitute;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Application.Panel;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Packages;

namespace RowingClub.UnitTests.Scheduling.Application.Panel;

public sealed class CreatePackageCommandHandlerTests
{
    private readonly ILessonPackageRepository _repository = Substitute.For<ILessonPackageRepository>();
    private readonly ISchedulingUnitOfWork _unitOfWork = Substitute.For<ISchedulingUnitOfWork>();

    private CreatePackageCommandHandler CreateHandler() => new(_repository, _unitOfWork);

    [Fact]
    public async Task Throws_invalid_name_when_name_is_whitespace()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(
            new CreatePackageCommand("   ", null, 8, 100m, null), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("invalid_name");

        _repository.DidNotReceive().Add(Arg.Any<LessonPackage>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Creates_package_and_persists_when_valid()
    {
        var handler = CreateHandler();
        var result = await handler.Handle(
            new CreatePackageCommand("8 Derslik Paket", "Açıklama", 8, 100m, 30),
            CancellationToken.None);

        result.Name.Should().Be("8 Derslik Paket");
        result.SessionCount.Should().Be(8);
        result.Price.Should().Be(100m);
        _repository.Received(1).Add(Arg.Is<LessonPackage>(p => p!.Name == "8 Derslik Paket"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public sealed class UpdatePackageCommandHandlerTests
{
    private readonly ILessonPackageRepository _repository = Substitute.For<ILessonPackageRepository>();
    private readonly ISchedulingUnitOfWork _unitOfWork = Substitute.For<ISchedulingUnitOfWork>();

    private UpdatePackageCommandHandler CreateHandler() => new(_repository, _unitOfWork);

    private static LessonPackage CreatePackage() =>
        LessonPackage.Create("Eski Ad", null, 8, 100m, null);

    [Fact]
    public async Task Throws_not_found_when_package_does_not_exist()
    {
        var packageId = Guid.NewGuid();
        _repository.GetByIdAsync(packageId, Arg.Any<CancellationToken>()).Returns((LessonPackage?)null);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new UpdatePackageCommand(packageId, "Yeni Ad", null, 8, 100m, true, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Updates_package_and_persists_when_found()
    {
        var package = CreatePackage();
        _repository.GetByIdAsync(package.Id, Arg.Any<CancellationToken>()).Returns(package);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new UpdatePackageCommand(package.Id, "Yeni Ad", "Yeni Açıklama", 10, 150m, false, null),
            CancellationToken.None);

        result.Name.Should().Be("Yeni Ad");
        result.Price.Should().Be(150m);
        result.IsActive.Should().BeFalse();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public sealed class SetPackageImageCommandHandlerTests
{
    private readonly ILessonPackageRepository _repository = Substitute.For<ILessonPackageRepository>();
    private readonly ISchedulingUnitOfWork _unitOfWork = Substitute.For<ISchedulingUnitOfWork>();

    private SetPackageImageCommandHandler CreateHandler() => new(_repository, _unitOfWork);

    [Fact]
    public async Task Throws_not_found_when_package_does_not_exist()
    {
        var packageId = Guid.NewGuid();
        _repository.GetByIdAsync(packageId, Arg.Any<CancellationToken>()).Returns((LessonPackage?)null);

        var handler = CreateHandler();
        var act = () => handler.Handle(
            new SetPackageImageCommand(packageId, "/uploads/paketler/x/y.jpg"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sets_image_and_persists_when_found()
    {
        var package = LessonPackage.Create("Paket", null, 8, 100m, null);
        _repository.GetByIdAsync(package.Id, Arg.Any<CancellationToken>()).Returns(package);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new SetPackageImageCommand(package.Id, "/uploads/paketler/x/y.jpg"), CancellationToken.None);

        result.ImagePath.Should().Be("/uploads/paketler/x/y.jpg");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

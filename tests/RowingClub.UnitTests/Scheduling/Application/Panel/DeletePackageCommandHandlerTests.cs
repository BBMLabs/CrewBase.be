using FluentAssertions;
using NSubstitute;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Application.Files;
using RowingClub.Scheduling.Application.Panel;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Packages;

namespace RowingClub.UnitTests.Scheduling.Application.Panel;

public sealed class DeletePackageCommandHandlerTests
{
    private readonly ILessonPackageRepository _packageRepository = Substitute.For<ILessonPackageRepository>();
    private readonly ICustomerPackageRepository _customerPackageRepository = Substitute.For<ICustomerPackageRepository>();
    private readonly IFileStorageService _fileStorage = Substitute.For<IFileStorageService>();
    private readonly ISchedulingUnitOfWork _unitOfWork = Substitute.For<ISchedulingUnitOfWork>();

    private DeletePackageCommandHandler CreateHandler() =>
        new(_packageRepository, _customerPackageRepository, _fileStorage, _unitOfWork);

    private static LessonPackage CreatePackage() =>
        LessonPackage.Create("Test Paket", null, 8, 100m, null, null, null);

    [Fact]
    public async Task Blocks_deletion_when_package_has_been_purchased_or_assigned()
    {
        var package = CreatePackage();
        var customer = Customer.Create("Ada Lovelace", "+905551112233", "ada@example.com");
        var customerPackage = CustomerPackage.Assign(customer.Id, package, CustomerPackageSource.Assigned);

        _packageRepository.GetByIdAsync(package.Id, Arg.Any<CancellationToken>()).Returns(package);
        _customerPackageRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([customerPackage]);

        var handler = CreateHandler();
        var act = () => handler.Handle(new DeletePackageCommand(package.Id), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.ErrorCode.Should().Be("package_has_purchases");

        _packageRepository.DidNotReceive().Remove(Arg.Any<LessonPackage>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deletes_package_and_its_image_when_never_purchased()
    {
        var package = CreatePackage();
        package.SetImage("/uploads/paketler/x/y.jpg");

        _packageRepository.GetByIdAsync(package.Id, Arg.Any<CancellationToken>()).Returns(package);
        _customerPackageRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);

        var handler = CreateHandler();
        await handler.Handle(new DeletePackageCommand(package.Id), CancellationToken.None);

        await _fileStorage.Received(1).DeleteAsync("/uploads/paketler/x/y.jpg", Arg.Any<CancellationToken>());
        _packageRepository.Received(1).Remove(package);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

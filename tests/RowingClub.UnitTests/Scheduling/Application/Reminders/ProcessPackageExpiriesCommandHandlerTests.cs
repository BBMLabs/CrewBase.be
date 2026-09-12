using FluentAssertions;
using NSubstitute;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.Scheduling.Application.Reminders;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Appointments;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Logs;
using RowingClub.Scheduling.Domain.Packages;
using RowingClub.Scheduling.Domain.Settings;

namespace RowingClub.UnitTests.Scheduling.Application.Reminders;

public sealed class ProcessPackageExpiriesCommandHandlerTests
{
    private readonly ISettingsRepository _settingsRepository = Substitute.For<ISettingsRepository>();
    private readonly ICustomerPackageRepository _customerPackageRepository = Substitute.For<ICustomerPackageRepository>();
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IAppointmentRepository _appointmentRepository = Substitute.For<IAppointmentRepository>();
    private readonly IMemberLogRepository _memberLogRepository = Substitute.For<IMemberLogRepository>();
    private readonly IPackageExpiryReminderSender _reminderSender = Substitute.For<IPackageExpiryReminderSender>();
    private readonly ITenantDatabase _tenantDatabase = Substitute.For<ITenantDatabase>();
    private readonly ISchedulingUnitOfWork _unitOfWork = Substitute.For<ISchedulingUnitOfWork>();

    private ProcessPackageExpiriesCommandHandler CreateHandler() => new(
        _settingsRepository, _customerPackageRepository, _customerRepository, _appointmentRepository,
        _memberLogRepository, _reminderSender, _tenantDatabase, _unitOfWork);

    private static CustomerPackage CreateUpcomingPackage(Guid customerId, int validityDays) =>
        CustomerPackage.Assign(
            customerId,
            LessonPackage.Create("Paket", null, 8, 100m, validityDays),
            CustomerPackageSource.Assigned);

    private static CustomerPackage CreateExpiredPackage(Guid customerId)
    {
        var package = CustomerPackage.Assign(
            customerId,
            LessonPackage.Create("Paket", null, 8, 100m, 1),
            CustomerPackageSource.Assigned);

        typeof(CustomerPackage)
            .GetProperty(nameof(CustomerPackage.ExpiresAtUtc))!
            .SetValue(package, DateTimeOffset.UtcNow.AddDays(-1));

        return package;
    }

    private void SetUpCommon(CustomerPackage package, Customer customer)
    {
        _settingsRepository.GetAsync(Arg.Any<CancellationToken>()).Returns((CompanySettings?)null);
        _customerPackageRepository.GetExpiringWithinAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<CustomerPackage> { package });
        _customerRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Customer> { customer });
        _appointmentRepository.GetPackageIdsWithActiveFutureAppointmentsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<DateOnly>(), Arg.Any<TimeOnly>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<Guid>());
    }

    [Fact]
    public async Task Does_not_send_reminder_when_automatic_dues_reminders_disabled()
    {
        var customer = Customer.Create("Ali Veli", "5551234567", "ali@example.com");
        var package = CreateUpcomingPackage(customer.Id, 5);
        SetUpCommon(package, customer);
        _tenantDatabase.HasAutomaticDuesReminders.Returns(false);

        var handler = CreateHandler();
        var result = await handler.Handle(new ProcessPackageExpiriesCommand("Kulüp"), CancellationToken.None);

        result.RemindersSent.Should().Be(0);
        await _reminderSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sends_reminder_when_automatic_dues_reminders_enabled()
    {
        var customer = Customer.Create("Ali Veli", "5551234567", "ali@example.com");
        var package = CreateUpcomingPackage(customer.Id, 5);
        SetUpCommon(package, customer);
        _tenantDatabase.HasAutomaticDuesReminders.Returns(true);

        var handler = CreateHandler();
        var result = await handler.Handle(new ProcessPackageExpiriesCommand("Kulüp"), CancellationToken.None);

        result.RemindersSent.Should().Be(1);
        await _reminderSender.Received(1).SendAsync(
            customer.Email!, customer.FullName, "Kulüp", package.PackageName,
            Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deletes_expired_package_even_when_automatic_dues_reminders_disabled()
    {
        var customer = Customer.Create("Ali Veli", "5551234567", "ali@example.com");
        var package = CreateExpiredPackage(customer.Id);
        SetUpCommon(package, customer);
        _tenantDatabase.HasAutomaticDuesReminders.Returns(false);

        var handler = CreateHandler();
        var result = await handler.Handle(new ProcessPackageExpiriesCommand("Kulüp"), CancellationToken.None);

        result.PackagesDeleted.Should().Be(1);
        _customerPackageRepository.Received(1).Remove(package);
        await _reminderSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}

using FluentAssertions;
using NSubstitute;
using RowingClub.Identity.Application.Platform;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Platform;
using RowingClub.UnitTests.Identity.Application;

namespace RowingClub.UnitTests.Identity.Application.Platform;

public sealed class GetPlatformActivityLogsQueryHandlerTests
{
    private readonly IPlatformActivityLogRepository _repository = Substitute.For<IPlatformActivityLogRepository>();
    private readonly ICompanyRepository _companyRepository = Substitute.For<ICompanyRepository>();

    private GetPlatformActivityLogsQueryHandler CreateHandler() => new(_repository, _companyRepository);

    [Fact]
    public async Task Resolves_target_company_name_for_logs_that_have_one()
    {
        var company = CompanyTestFactory.Create("Kürek Kulübü");
        var log = PlatformActivityLog.Record(Guid.NewGuid(), "admin@platform.com", "SetCompanyPlan", company.Id, "1.2.3.4", "UA");
        _repository
            .GetPagedAsync(null, 1, 25, Arg.Any<CancellationToken>())
            .Returns(([log], 1));
        _companyRepository
            .GetByIdsAsync(Arg.Is<IReadOnlyCollection<Guid>>(ids => ids != null && ids.Contains(company.Id)), Arg.Any<CancellationToken>())
            .Returns([company]);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetPlatformActivityLogsQuery(), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].TargetCompanyId.Should().Be(company.Id);
        result.Items[0].TargetCompanyName.Should().Be("Kürek Kulübü");
    }

    [Fact]
    public async Task Leaves_target_company_name_null_for_logs_with_no_target_company()
    {
        var log = PlatformActivityLog.Record(Guid.NewGuid(), "admin@platform.com", "ResetCompanyAdminPassword", null, null, null);
        _repository
            .GetPagedAsync(null, 1, 25, Arg.Any<CancellationToken>())
            .Returns(([log], 1));
        _companyRepository
            .GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetPlatformActivityLogsQuery(), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].TargetCompanyId.Should().BeNull();
        result.Items[0].TargetCompanyName.Should().BeNull();
        await _companyRepository.Received(1).GetByIdsAsync(Arg.Is<IReadOnlyCollection<Guid>>(ids => ids != null && ids.Count == 0), Arg.Any<CancellationToken>());
    }
}

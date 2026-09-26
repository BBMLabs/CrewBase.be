using FluentAssertions;
using NSubstitute;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Application.Companies.SiteContent;
using RowingClub.Identity.Domain.Companies;
using RowingClub.UnitTests.Identity.Application;

namespace RowingClub.UnitTests.Identity.Domain;

public sealed class CompanySeoSettingsTests
{
    [Fact]
    public void New_company_allows_indexing_and_has_no_seo_values()
    {
        var company = CompanyTestFactory.Create();

        company.AllowIndexing.Should().BeTrue();
        company.SeoTitle.Should().BeNull();
        company.SeoDescription.Should().BeNull();
        company.SeoKeywords.Should().BeNull();
        company.GoogleSiteVerification.Should().BeNull();
        company.GoogleAnalyticsId.Should().BeNull();
    }

    [Fact]
    public void UpdateSeoSettings_trims_values_and_stores_them()
    {
        var company = CompanyTestFactory.Create();

        company.UpdateSeoSettings(
            "  Deniz Kürek Kulübü  ", " İstanbul kürek dersleri ", " kürek, deniz ", " abc123 ", " G-ABCD1234 ", false);

        company.SeoTitle.Should().Be("Deniz Kürek Kulübü");
        company.SeoDescription.Should().Be("İstanbul kürek dersleri");
        company.SeoKeywords.Should().Be("kürek, deniz");
        company.GoogleSiteVerification.Should().Be("abc123");
        company.GoogleAnalyticsId.Should().Be("G-ABCD1234");
        company.AllowIndexing.Should().BeFalse();
    }

    [Fact]
    public void UpdateSeoSettings_turns_empty_values_into_null()
    {
        var company = CompanyTestFactory.Create();
        company.UpdateSeoSettings("Başlık", "Açıklama", "a,b", "token", "G-ABCD", true);

        company.UpdateSeoSettings("", "   ", null, " ", "", true);

        company.SeoTitle.Should().BeNull();
        company.SeoDescription.Should().BeNull();
        company.SeoKeywords.Should().BeNull();
        company.GoogleSiteVerification.Should().BeNull();
        company.GoogleAnalyticsId.Should().BeNull();
    }

    [Fact]
    public void UpdateSeoSettings_accepts_values_at_max_length()
    {
        var company = CompanyTestFactory.Create();

        company.UpdateSeoSettings(
            new string('a', Company.SeoTitleMaxLength),
            new string('b', Company.SeoDescriptionMaxLength),
            new string('c', Company.SeoKeywordsMaxLength),
            new string('d', Company.GoogleSiteVerificationMaxLength),
            "G-" + new string('A', 20),
            true);

        company.SeoTitle.Should().HaveLength(Company.SeoTitleMaxLength);
        company.GoogleAnalyticsId.Should().HaveLength(22);
    }

    [Theory]
    [InlineData(71, 0, 0, 0, "invalid_seo_title")]
    [InlineData(0, 171, 0, 0, "invalid_seo_description")]
    [InlineData(0, 0, 256, 0, "invalid_seo_keywords")]
    [InlineData(0, 0, 0, 101, "invalid_google_site_verification")]
    public void UpdateSeoSettings_rejects_values_over_max_length(
        int titleLength, int descriptionLength, int keywordsLength, int verificationLength, string expectedCode)
    {
        var company = CompanyTestFactory.Create();

        var act = () => company.UpdateSeoSettings(
            new string('a', titleLength), new string('b', descriptionLength), new string('c', keywordsLength),
            new string('d', verificationLength), null, true);

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be(expectedCode);
    }

    [Theory]
    [InlineData("UA-12345-1")]
    [InlineData("G-abc123")]
    [InlineData("G-ABC")]
    [InlineData("G-ABCDEFGHIJKLMNOPQRSTU")]
    [InlineData("GT-ABCD1234")]
    [InlineData("G-ABCD 1234")]
    public void UpdateSeoSettings_rejects_invalid_google_analytics_id(string analyticsId)
    {
        var company = CompanyTestFactory.Create();

        var act = () => company.UpdateSeoSettings(null, null, null, null, analyticsId, true);

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("invalid_google_analytics_id");
    }

    [Theory]
    [InlineData("G-ABCD")]
    [InlineData("G-1A2B3C4D5E")]
    public void UpdateSeoSettings_accepts_valid_google_analytics_id(string analyticsId)
    {
        var company = CompanyTestFactory.Create();

        company.UpdateSeoSettings(null, null, null, null, analyticsId, true);

        company.GoogleAnalyticsId.Should().Be(analyticsId);
    }

    [Fact]
    public void Failed_update_leaves_previous_values_untouched()
    {
        var company = CompanyTestFactory.Create();
        company.UpdateSeoSettings("Başlık", null, null, null, "G-ABCD", true);

        var act = () => company.UpdateSeoSettings("Yeni", null, null, null, "invalid", false);

        act.Should().Throw<DomainException>();
        company.SeoTitle.Should().Be("Başlık");
        company.AllowIndexing.Should().BeTrue();
    }
}

public sealed class UpdateCompanySiteContentSeoHandlerTests
{
    private readonly ICompanyRepository _companyRepository = Substitute.For<ICompanyRepository>();
    private readonly ICompanyGalleryImageRepository _galleryRepository = Substitute.For<ICompanyGalleryImageRepository>();

    private UpdateCompanySiteContentCommandHandler CreateHandler() => new(_companyRepository, _galleryRepository);

    [Fact]
    public async Task Request_without_seo_fields_keeps_existing_seo_settings()
    {
        var company = CompanyTestFactory.Create();
        company.UpdateSeoSettings("Başlık", "Açıklama", null, null, "G-ABCD", false);
        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _galleryRepository.GetByCompanyIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateHandler().Handle(
            new UpdateCompanySiteContentCommand(
                company.Id, "Slogan", "Hakkımızda", null, null, null, null, null, null, null, null, null),
            CancellationToken.None);

        result.SeoTitle.Should().Be("Başlık");
        result.GoogleAnalyticsId.Should().Be("G-ABCD");
        result.AllowIndexing.Should().BeFalse();
    }

    [Fact]
    public async Task Request_with_seo_fields_replaces_seo_settings()
    {
        var company = CompanyTestFactory.Create();
        company.UpdateSeoSettings("Eski", "Eski açıklama", null, null, null, true);
        _companyRepository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _galleryRepository.GetByCompanyIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateHandler().Handle(
            new UpdateCompanySiteContentCommand(
                company.Id, "Slogan", "Hakkımızda", null, null, null, null, null, null, null, null, null,
                SeoTitle: "Yeni", SeoDescription: null, AllowIndexing: false),
            CancellationToken.None);

        result.SeoTitle.Should().Be("Yeni");
        result.SeoDescription.Should().BeNull();
        result.AllowIndexing.Should().BeFalse();
    }
}

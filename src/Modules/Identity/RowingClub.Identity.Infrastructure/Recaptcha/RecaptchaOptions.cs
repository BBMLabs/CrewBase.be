namespace RowingClub.Identity.Infrastructure.Recaptcha;

public sealed class RecaptchaOptions
{
    public const string SectionName = "Recaptcha";

    public string SecretKey { get; init; } = string.Empty;
    public double MinimumScore { get; init; } = 0.5;
}

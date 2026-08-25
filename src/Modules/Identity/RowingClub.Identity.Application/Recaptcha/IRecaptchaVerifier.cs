namespace RowingClub.Identity.Application.Recaptcha;

public interface IRecaptchaVerifier
{
    Task<bool> VerifyAsync(string? token, string action, CancellationToken cancellationToken = default);
}

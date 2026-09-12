using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RowingClub.Identity.Application.Recaptcha;

namespace RowingClub.Identity.Infrastructure.Recaptcha;

public sealed class GoogleRecaptchaVerifier(
    HttpClient httpClient, IOptions<RecaptchaOptions> options, ILogger<GoogleRecaptchaVerifier> logger)
    : IRecaptchaVerifier
{
    private readonly RecaptchaOptions _options = options.Value;

    public async Task<bool> VerifyAsync(string? token, string action, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var response = await httpClient.PostAsync(
                "https://www.google.com/recaptcha/api/siteverify",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["secret"] = _options.SecretKey,
                    ["response"] = token,
                }),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("reCAPTCHA siteverify HTTP {StatusCode} döndürdü.", (int)response.StatusCode);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<RecaptchaVerifyResponse>(cancellationToken: cancellationToken);
            var passed = result is { Success: true } && result.Score >= _options.MinimumScore && result.Action == action;

            if (!passed)
            {
                logger.LogWarning(
                    "reCAPTCHA doğrulaması reddedildi. Success={Success} Score={Score} Action={ActualAction} BeklenenAction={ExpectedAction} ErrorCodes={ErrorCodes}",
                    result?.Success, result?.Score, result?.Action, action, result?.ErrorCodes is null ? null : string.Join(",", result.ErrorCodes));
            }

            return passed;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            logger.LogError(ex, "reCAPTCHA doğrulama isteği başarısız oldu.");
            return false;
        }
    }

    private sealed record RecaptchaVerifyResponse(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("score")] double Score,
        [property: JsonPropertyName("action")] string? Action,
        [property: JsonPropertyName("error-codes")] string[]? ErrorCodes);
}

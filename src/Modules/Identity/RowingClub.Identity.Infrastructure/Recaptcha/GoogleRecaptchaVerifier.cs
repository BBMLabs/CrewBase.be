using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RowingClub.Identity.Application.Recaptcha;

namespace RowingClub.Identity.Infrastructure.Recaptcha;

public sealed class GoogleRecaptchaVerifier(HttpClient httpClient, IOptions<RecaptchaOptions> options) : IRecaptchaVerifier
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
            return false;
        }

        var result = await response.Content.ReadFromJsonAsync<RecaptchaVerifyResponse>(cancellationToken: cancellationToken);

        return result is { Success: true } && result.Score >= _options.MinimumScore && result.Action == action;
    }

    private sealed record RecaptchaVerifyResponse(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("score")] double Score,
        [property: JsonPropertyName("action")] string? Action);
}

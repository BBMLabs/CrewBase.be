using System.Text;

namespace RowingClub.BuildingBlocks.Infrastructure.Configuration;

/// <summary>
/// `.env.developer` / `.env.production` are only ever read by <c>docker compose --env-file</c> -
/// running the API directly with <c>dotnet run</c> (no Docker) leaves every RowingClub_* setting
/// unset and the app fails fast at startup with a validation error. This loads the matching
/// dotenv file straight into the process's real environment variables (never overwriting a value
/// that's already set - e.g. one exported by the shell or by docker-compose itself) so both ways
/// of running the app behave the same. Supports the double-quoted, multi-line values this project
/// uses for PEM keys. Lives in BuildingBlocks.Infrastructure (not RowingClub.Api) so
/// RowingClub.IntegrationTests can load real dev credentials for its Postgres fixture without a
/// project reference to the API.
/// </summary>
public static class DotEnvFileLoader
{
    /// <summary>Set by test hosts (RowingClubWebApplicationFactory) so a developer's real
    /// `.env.developer` on disk never leaks into test runs and silently overrides the throwaway
    /// secrets the tests set up themselves.</summary>
    public const string DisableEnvVarName = "ROWINGCLUB_DISABLE_DOTENV";

    public static void LoadForEnvironment(string? environmentName)
    {
        if (Environment.GetEnvironmentVariable(DisableEnvVarName) is not null)
        {
            return;
        }

        var fileName = environmentName switch
        {
            "Production" => ".env.production",
            _ => ".env.developer",
        };

        var path = FindUpwardsFromCurrentDirectory(fileName);
        if (path is null)
        {
            return;
        }

        foreach (var (key, value) in Parse(File.ReadAllText(path)))
        {
            if (Environment.GetEnvironmentVariable(key) is null)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    private static string? FindUpwardsFromCurrentDirectory(string fileName)
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

        for (var i = 0; i < 6 && directory is not null; i++, directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static IEnumerable<(string Key, string Value)> Parse(string content)
    {
        var lines = content.Replace("\r\n", "\n").Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimStart();

            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var rest = line[(separatorIndex + 1)..];

            if (rest.StartsWith('"'))
            {
                var (value, consumedLines) = ReadQuotedValue(rest, lines, i + 1);
                yield return (key, value);
                i += consumedLines;
            }
            else
            {
                yield return (key, rest.Trim());
            }
        }
    }

    private static (string Value, int ConsumedLines) ReadQuotedValue(
        string firstLineRemainder, string[] lines, int nextLineIndex)
    {
        var builder = new StringBuilder();
        var withoutOpeningQuote = firstLineRemainder[1..];

        if (withoutOpeningQuote.EndsWith('"') && withoutOpeningQuote.Length > 0)
        {
            return (withoutOpeningQuote[..^1], 0);
        }

        builder.Append(withoutOpeningQuote);

        var consumedLines = 0;
        for (var i = nextLineIndex; i < lines.Length; i++)
        {
            consumedLines++;
            var line = lines[i];

            if (line.EndsWith('"'))
            {
                builder.Append('\n').Append(line[..^1]);
                break;
            }

            builder.Append('\n').Append(line);
        }

        return (builder.ToString(), consumedLines);
    }
}

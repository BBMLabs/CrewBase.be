namespace RowingClub.Api.Configuration;

/// <summary>
/// The env var names mandated by spec section 10 (SCREAMING_SNAKE_CASE, flat) don't match the
/// nested <c>Section:Key</c> shape the options types bind against. This is the one place that
/// translates between the two, so every other file can just talk in terms of
/// <c>IConfiguration</c> sections without knowing about the raw env var names.
/// </summary>
public static class EnvironmentConfigurationExtensions
{
    public static IConfigurationBuilder AddRowingClubEnvironmentMapping(this IConfigurationBuilder builder)
    {
        var env = Environment.GetEnvironmentVariables();

        string? Get(string key) => env.Contains(key) ? env[key] as string : null;

        var currentKeyVersion = Get("FIELD_ENCRYPTION_KEY_VERSION");
        var currentKey = Get("FIELD_ENCRYPTION_KEY_CURRENT");
        var previousKey = Get("FIELD_ENCRYPTION_KEY_PREVIOUS");

        var mapped = new Dictionary<string, string?>
        {
            // The backend builds the actual mongodb:// connection string (with the mandatory
            // replicaSet=rs0 - MongoUnitOfWork uses multi-document transactions, which a
            // standalone mongod does not support, spec section 8/21) from these three pieces -
            // see MongoOptions.ConnectionString.
            ["Mongo:Host"] = Get("MONGODB_HOST"),
            ["Mongo:Port"] = Get("MONGODB_PORT"),
            ["Mongo:DatabaseName"] = Get("MONGODB_DATABASE_NAME") ?? "rowingclub",
            ["Mongo:Username"] = Get("MONGODB_USERNAME"),
            ["Mongo:Password"] = Get("MONGODB_PASSWORD"),

            ["Redis:Host"] = Get("REDIS_HOST"),
            ["Redis:Port"] = Get("REDIS_PORT"),
            ["Redis:Username"] = Get("REDIS_USERNAME"),
            ["Redis:Password"] = Get("REDIS_PASSWORD"),

            ["Jwt:Issuer"] = Get("JWT_ISSUER"),
            ["Jwt:Audience"] = Get("JWT_AUDIENCE"),
            ["Jwt:SigningPrivateKeyPem"] = Get("JWT_SIGNING_PRIVATE_KEY"),
            ["Jwt:SigningPublicKeyPem"] = Get("JWT_SIGNING_PUBLIC_KEY"),
            ["Jwt:KeyId"] = currentKeyVersion,

            ["FieldEncryption:CurrentKeyVersion"] = currentKeyVersion,
            ["FieldEncryption:BlindIndexKey"] = Get("FIELD_ENCRYPTION_BLIND_INDEX_KEY") ?? currentKey,

            ["Smtp:Host"] = Get("SMTP_HOST"),
            ["Smtp:Username"] = Get("SMTP_USERNAME"),
            ["Smtp:Password"] = Get("SMTP_PASSWORD"),

            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = Get("OTEL_EXPORTER_OTLP_ENDPOINT"),
        };

        if (currentKeyVersion is not null && currentKey is not null)
        {
            mapped[$"FieldEncryption:Keys:{currentKeyVersion}"] = currentKey;
        }

        if (currentKeyVersion is not null && previousKey is not null &&
            int.TryParse(currentKeyVersion, out var version) && version > 1)
        {
            mapped[$"FieldEncryption:Keys:{version - 1}"] = previousKey;
        }

        // An explicit null here is a real configuration VALUE as far as the configuration system
        // is concerned - it shadows/overrides whatever an earlier provider (appsettings.json, a
        // test host's UseSetting, ...) set for the same key, rather than leaving it unset. Only
        // env vars that are actually present should end up in this source.
        var presentOnly = mapped
            .Where(kvp => kvp.Value is not null)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return builder.AddInMemoryCollection(presentOnly);
    }
}

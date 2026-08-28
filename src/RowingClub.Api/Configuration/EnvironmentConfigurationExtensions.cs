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

        // A blank env var (e.g. an unfilled ".env.production" template line, `KEY=`) must be
        // treated the same as "not set" - otherwise it silently overrides a valid value from an
        // earlier config source (docker-compose's own environment block, appsettings.json) with
        // an empty string, which then fails options binding for non-string types like int.
        string? Get(string key) => env.Contains(key) && env[key] is string { Length: > 0 } value ? value : null;

        var currentKeyVersion = Get("FIELD_ENCRYPTION_KEY_VERSION");
        var currentKey = Get("FIELD_ENCRYPTION_KEY_CURRENT");
        var previousKey = Get("FIELD_ENCRYPTION_KEY_PREVIOUS");

        var mapped = new Dictionary<string, string?>
        {
            // The backend builds the actual Npgsql connection string from these pieces - see
            // PostgresOptions.ConnectionString.
            ["Postgres:Host"] = Get("POSTGRES_HOST"),
            ["Postgres:Port"] = Get("POSTGRES_PORT"),
            ["Postgres:DatabaseName"] = Get("POSTGRES_DATABASE_NAME") ?? "rowingclub",
            ["Postgres:Username"] = Get("POSTGRES_USERNAME"),
            ["Postgres:Password"] = Get("POSTGRES_PASSWORD"),

            ["Redis:Host"] = Get("REDIS_HOST"),
            ["Redis:Port"] = Get("REDIS_PORT"),
            ["Redis:Username"] = Get("REDIS_USERNAME"),
            ["Redis:Password"] = Get("REDIS_PASSWORD"),

            ["Jwt:Issuer"] = Get("JWT_ISSUER"),
            ["Jwt:Audience"] = Get("JWT_AUDIENCE"),
            ["Jwt:SigningPrivateKeyPem"] = Get("JWT_SIGNING_PRIVATE_KEY"),
            ["Jwt:SigningPublicKeyPem"] = Get("JWT_SIGNING_PUBLIC_KEY"),
            ["Jwt:KeyId"] = Get("JWT_KEY_ID") ?? "1",

            ["FieldEncryption:CurrentKeyVersion"] = currentKeyVersion,
            ["FieldEncryption:BlindIndexKey"] = Get("FIELD_ENCRYPTION_BLIND_INDEX_KEY") ?? currentKey,

            ["Smtp:Host"] = Get("SMTP_HOST"),
            ["Smtp:Username"] = Get("SMTP_USERNAME"),
            ["Smtp:Password"] = Get("SMTP_PASSWORD"),
            ["Smtp:FromAddress"] = Get("SMTP_FROM_ADDRESS"),
            ["Smtp:FromName"] = Get("SMTP_FROM_NAME"),

            ["Recaptcha:SecretKey"] = Get("RECAPTCHA_SECRET_KEY"),

            ["Iyzico:ApiKey"] = Get("IYZICO_API_KEY"),
            ["Iyzico:SecretKey"] = Get("IYZICO_SECRET_KEY"),
            ["Iyzico:MerchantId"] = Get("IYZICO_MERCHANT_ID"),
            ["Iyzico:BaseUrl"] = Get("IYZICO_BASE_URL") ?? "https://sandbox-api.iyzipay.com",
            ["Iyzico:WebhookSecret"] = Get("IYZICO_WEBHOOK_SECRET"),
            ["Iyzico:PlanReferenceCodeTayfa"] = Get("IYZICO_PLAN_TAYFA_REF"),
            ["Iyzico:PlanReferenceCodeKaptan"] = Get("IYZICO_PLAN_KAPTAN_REF"),
            ["Iyzico:PlanReferenceCodeAmiral"] = Get("IYZICO_PLAN_AMIRAL_REF"),

            // Üye ders paketi satın alma (Scheduling modülü, klasik CheckoutForm API) - firma
            // abonelik faturalamasıyla (yukarıdaki Iyzico:*) AYNI mağaza hesabı/env değişkenleri
            // paylaşılır, bkz. plan mimari karar #3.
            ["PackagePayment:ApiKey"] = Get("IYZICO_API_KEY"),
            ["PackagePayment:SecretKey"] = Get("IYZICO_SECRET_KEY"),
            ["PackagePayment:BaseUrl"] = Get("IYZICO_BASE_URL") ?? "https://sandbox-api.iyzipay.com",

            ["FileStorage:RootPath"] = Get("UPLOADS_ROOT_PATH") ?? "./uploads",

            // Cloudflare R2 (S3 uyumlu) görsel depolama - hepsi boşsa yerel diske düşülür
            // (bkz. Scheduling.Infrastructure/DependencyInjection.cs). PublicBaseUrl, bucket'a
            // bağlanmış özel alan adı (ör. https://cdn.faturebase.com) ya da r2.dev test URL'i.
            ["R2Storage:AccountId"] = Get("R2_ACCOUNT_ID"),
            ["R2Storage:AccessKeyId"] = Get("R2_ACCESS_KEY_ID"),
            ["R2Storage:SecretAccessKey"] = Get("R2_SECRET_ACCESS_KEY"),
            ["R2Storage:BucketName"] = Get("R2_BUCKET_NAME"),
            ["R2Storage:PublicBaseUrl"] = Get("R2_PUBLIC_BASE_URL"),

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

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AFH.AdviserInsights.Infrastructure.Persistence.Snowflake;

internal static class SnowflakeJwtTokenFactory
{
    public static string CreateToken(SnowflakeConnectionSettings settings, TimeProvider timeProvider)
    {
        if (string.IsNullOrWhiteSpace(settings.Account))
            throw new InvalidOperationException("AdviserInsights:Snowflake:Account is required for snowflake_jwt authentication.");
        if (string.IsNullOrWhiteSpace(settings.User))
            throw new InvalidOperationException("AdviserInsights:Snowflake:User is required for snowflake_jwt authentication.");

        using var rsa = LoadPrivateKey(settings);
        var publicKeyFingerprint = CreatePublicKeyFingerprint(rsa);
        var subject = $"{Normalize(settings.Account)}.{Normalize(settings.User)}";
        var now = timeProvider.GetUtcNow();

        var header = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new
        {
            alg = "RS256",
            typ = "JWT"
        }));

        var payload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new
        {
            iss = $"{subject}.{publicKeyFingerprint}",
            sub = subject,
            iat = now.ToUnixTimeSeconds(),
            exp = now.AddMinutes(59).ToUnixTimeSeconds()
        }));

        var unsignedToken = $"{header}.{payload}";
        var signature = rsa.SignData(
            Encoding.ASCII.GetBytes(unsignedToken),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return $"{unsignedToken}.{Base64UrlEncode(signature)}";
    }

    private static RSA LoadPrivateKey(SnowflakeConnectionSettings settings)
    {
        var privateKey = settings.PrivateKey;
        if (string.IsNullOrWhiteSpace(privateKey) && !string.IsNullOrWhiteSpace(settings.PrivateKeyFile))
            privateKey = File.ReadAllText(settings.PrivateKeyFile);

        if (string.IsNullOrWhiteSpace(privateKey))
            throw new InvalidOperationException("AdviserInsights:Snowflake:PrivateKey or AdviserInsights:Snowflake:PrivateKeyFile is required for snowflake_jwt authentication.");

        privateKey = privateKey.Replace("\\n", "\n", StringComparison.Ordinal).Trim();
        var rsa = RSA.Create();

        if (privateKey.Contains("BEGIN", StringComparison.OrdinalIgnoreCase))
        {
            rsa.ImportFromPem(privateKey);
            return rsa;
        }

        var bytes = Convert.FromBase64String(privateKey);
        rsa.ImportPkcs8PrivateKey(bytes, out _);
        return rsa;
    }

    private static string CreatePublicKeyFingerprint(RSA rsa)
    {
        var publicKey = rsa.ExportSubjectPublicKeyInfo();
        var hash = SHA256.HashData(publicKey);
        return $"SHA256:{Convert.ToBase64String(hash)}";
    }

    private static string Normalize(string value)
        => value.Trim().ToUpperInvariant();

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}

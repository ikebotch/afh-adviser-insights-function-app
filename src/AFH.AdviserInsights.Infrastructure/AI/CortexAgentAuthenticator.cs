using System.Security.Cryptography;
using AFH.AdviserInsights.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AFH.AdviserInsights.Infrastructure.AI;

public sealed class CortexAgentAuthenticator(IOptions<AdviserInsightsOptions> options) : ICortexAgentAuthenticator
{
    private const string BearerTokenMode = "BearerToken";
    private const string KeyPairJwtMode = "KeyPairJwt";
    private const int MaximumJwtLifetimeMinutes = 60;

    public void Apply(HttpRequestMessage request)
    {
        var agent = options.Value.CortexAgent;
        AddOptionalHeader(request, "X-Snowflake-Role", agent.Role);
        AddOptionalHeader(request, "X-Snowflake-Warehouse", agent.Warehouse);

        if (agent.AuthenticationMode.Equals(KeyPairJwtMode, StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {CreateKeyPairJwt(agent)}");
            request.Headers.TryAddWithoutValidation("X-Snowflake-Authorization-Token-Type", "KEYPAIR_JWT");
            return;
        }

        if (!agent.AuthenticationMode.Equals(BearerTokenMode, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Unsupported Cortex agent authentication mode '{agent.AuthenticationMode}'.");

        var token = Require(agent.BearerToken, "AdviserInsights:CortexAgent:BearerToken");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
    }

    private static string CreateKeyPairJwt(CortexAgentOptions options)
    {
        var account = NormalizeRequiredClaimPart(options.AccountIdentifier, "AdviserInsights:CortexAgent:AccountIdentifier");
        var user = NormalizeRequiredClaimPart(options.User, "AdviserInsights:CortexAgent:User");
        var privateKey = Require(options.PrivateKey, "AdviserInsights:CortexAgent:PrivateKey");

        using var rsa = RSA.Create();
        var normalizedPrivateKey = privateKey.Replace("\\n", "\n", StringComparison.Ordinal);
        if (string.IsNullOrWhiteSpace(options.PrivateKeyPassphrase))
            rsa.ImportFromPem(normalizedPrivateKey);
        else
            rsa.ImportFromEncryptedPem(normalizedPrivateKey, options.PrivateKeyPassphrase);

        var fingerprint = $"SHA256:{Convert.ToBase64String(SHA256.HashData(rsa.ExportSubjectPublicKeyInfo()))}";
        var qualifiedUser = $"{account}.{user}";
        var now = DateTimeOffset.UtcNow;
        var signingKey = new RsaSecurityKey(rsa)
        {
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
        };
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = $"{qualifiedUser}.{fingerprint}",
            Subject = new System.Security.Claims.ClaimsIdentity(
            [
                new(JwtRegisteredClaimNames.Sub, qualifiedUser)
            ]),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = now.AddMinutes(Math.Clamp(options.JwtLifetimeMinutes, 1, MaximumJwtLifetimeMinutes)).UtcDateTime,
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private static void AddOptionalHeader(HttpRequestMessage request, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            request.Headers.TryAddWithoutValidation(name, value.Trim());
    }

    private static string NormalizeRequiredClaimPart(string? value, string settingName)
        => Require(value, settingName).Replace(".", "-", StringComparison.Ordinal).ToUpperInvariant();

    private static string Require(string? value, string settingName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"{settingName} is required.")
            : value.Trim();
}

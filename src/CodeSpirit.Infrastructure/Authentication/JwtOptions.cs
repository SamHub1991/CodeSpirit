using System.Diagnostics.CodeAnalysis;

namespace CodeSpirit.Infrastructure.Authentication;

public record JwtOptions
{
    public string Secret { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int ExpirationMinutes { get; init; } = 60;
    public int RefreshTokenExpirationDays { get; init; } = 7;

    public bool IsValid(out string? error)
    {
        if (string.IsNullOrWhiteSpace(Secret) || Secret.Length < 32)
        {
            error = "JWT Secret must be at least 32 characters";
            return false;
        }
        if (string.IsNullOrWhiteSpace(Issuer))
        {
            error = "JWT Issuer is required";
            return false;
        }
        if (string.IsNullOrWhiteSpace(Audience))
        {
            error = "JWT Audience is required";
            return false;
        }
        error = null;
        return true;
    }
}

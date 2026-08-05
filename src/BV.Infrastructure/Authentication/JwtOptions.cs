namespace BV.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "BVPortal";
    public string Audience { get; init; } = "BVPortal.Client";
    public string Key { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 30;
}

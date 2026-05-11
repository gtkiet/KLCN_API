// ============================================================
// Configurations/JwtSettings.cs
// ============================================================

namespace KLCN_API.Configurations;

public class JwtSettings
{
    public string SecretKey { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public int AccessTokenExpiryMinutes { get; set; } = 60;
    public int RefreshTokenExpiryDays { get; set; } = 30;
}

// ============================================================
// Configurations/CorsSettings.cs
// ============================================================

public class CorsSettings
{
    public List<string> AllowedOrigins { get; set; } = [];
}

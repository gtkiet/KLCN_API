namespace KLCN_API.Configurations;

public class JwtSettings
{
    public string SecretKey { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public int AccessTokenExpiryMinutes { get; set; } = 60;
    public int RefreshTokenExpiryDays { get; set; } = 30;
}

public class CorsSettings
{
    public List<string> AllowedOrigins { get; set; } = [];
}

public class VNPaySettings
{
    public string TmnCode { get; set; } = null!;
    public string HashSecret { get; set; } = null!;
    public string BaseUrl { get; set; } = null!;
    public string ReturnUrl { get; set; } = null!;
    public string IpnUrl { get; set; } = null!;
}

public class MoMoSettings
{
    public string PartnerCode { get; set; } = null!;
    public string AccessKey { get; set; } = null!;
    public string SecretKey { get; set; } = null!;
    public string Endpoint { get; set; } = null!;
    public string ReturnUrl { get; set; } = null!;
    public string IpnUrl { get; set; } = null!;
    public string RequestType { get; set; } = "payWithMethod";
}
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

public class EmailSettings
{
    public string Host { get; set; } = null!;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string SenderEmail { get; set; } = null!;
    public string SenderName { get; set; } = null!;
    public string AppPassword { get; set; } = null!;
}

public class FrontendSettings
{
    public string PaymentSuccessUrl { get; set; } = null!;
    public string PaymentFailedUrl { get; set; } = null!;

    /// <summary>Tạo URL success với bookingId.</summary>
    public string BuildSuccessUrl(int bookingId)
        => string.Format(PaymentSuccessUrl, bookingId);

    /// <summary>Tạo URL failed với bookingId.</summary>
    public string BuildFailedUrl(int bookingId)
        => string.Format(PaymentFailedUrl, bookingId);
}
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
    // Web: "https://yourfrontend.com/booking/{0}/success"
    public string PaymentSuccessUrl { get; set; } = null!;
    public string PaymentFailedUrl { get; set; } = null!;

    // Mobile Flutter deep link: "sportplus://payment/result"
    // Để trống nếu không có mobile app
    public string? MobileDeepLinkUrl { get; set; }

    public bool HasMobileDeepLink
        => !string.IsNullOrWhiteSpace(MobileDeepLinkUrl);

    // ── Web ───────────────────────────────────────────────────────

    public string BuildSuccessUrl(int bookingId)
        => string.Format(PaymentSuccessUrl, bookingId);

    public string BuildFailedUrl(int bookingId)
        => string.Format(PaymentFailedUrl, bookingId);

    // ── Mobile deep link ──────────────────────────────────────────

    /// <summary>"sportplus://payment/result?status=success&bookingId=88"</summary>
    public string BuildMobileSuccessUrl(int bookingId)
        => $"{MobileDeepLinkUrl}?status=success&bookingId={bookingId}";

    /// <summary>"sportplus://payment/result?status=failed&bookingId=88"</summary>
    public string BuildMobileFailedUrl(int bookingId)
        => $"{MobileDeepLinkUrl}?status=failed&bookingId={bookingId}";
}
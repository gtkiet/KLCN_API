namespace KLCN_API.Helpers;

public static class PasswordHelper
{
    private const int WorkFactor = 12;

    /// <summary>Hash password bằng BCrypt với work factor 12.</summary>
    public static string HashPassword(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    /// <summary>Kiểm tra password plaintext so với hash đã lưu.</summary>
    public static bool VerifyPassword(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);
}
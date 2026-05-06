using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;

namespace KLCN_API.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> RegisterAsync(RegisterRequest request);
    Task<LoginResponse> CreateStaffAsync(CreateStaffRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task LogoutAsync(string refreshToken);
    Task ChangePasswordAsync(int userId, ChangePasswordRequest request);
}
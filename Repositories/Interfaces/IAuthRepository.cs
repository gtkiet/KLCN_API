using KLCN_API.Models.Entities;

namespace KLCN_API.Repositories.Interfaces;

public interface IAuthRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> PhoneExistsAsync(string phone);
    Task<User> CreateUserAsync(User user, Profile profile);

    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task AddRefreshTokenAsync(RefreshToken token);
    Task RevokeRefreshTokenAsync(string token);
    Task RevokeAllRefreshTokensAsync(int userId);
    Task<string?> GetPasswordHashAsync(int userId);
    Task UpdatePasswordAsync(int userId, string newPasswordHash);
}
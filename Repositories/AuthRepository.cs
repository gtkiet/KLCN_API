using KLCN_API.Data;
using KLCN_API.Models.Entities;
using KLCN_API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KLCN_API.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly SportPlusDbContext _ctx;

    public AuthRepository(SportPlusDbContext ctx) => _ctx = ctx;

    public async Task<User?> GetByEmailAsync(string email)
        => await _ctx.Users
            .Include(u => u.Role)
            .Include(u => u.Status)
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);

    public async Task<bool> EmailExistsAsync(string email)
        => await _ctx.Users.AnyAsync(u => u.Email == email && !u.IsDeleted);

    public async Task<bool> PhoneExistsAsync(string phone)
        => await _ctx.Users.AnyAsync(u => u.Phone == phone && !u.IsDeleted);

    public async Task<User> CreateUserAsync(User user, Profile profile)
    {
        await _ctx.Users.AddAsync(user);
        await _ctx.SaveChangesAsync();

        profile.UserId = user.UserId;
        await _ctx.Profiles.AddAsync(profile);
        await _ctx.SaveChangesAsync();

        await _ctx.Entry(user).Reference(u => u.Role).LoadAsync();
        await _ctx.Entry(user).Reference(u => u.Status).LoadAsync();
        user.Profile = profile;

        return user;
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
        => await _ctx.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(rt => rt.Token == token && !rt.IsRevoked);

    public async Task AddRefreshTokenAsync(RefreshToken token)
    {
        await _ctx.RefreshTokens.AddAsync(token);
        await _ctx.SaveChangesAsync();
    }

    public async Task RevokeRefreshTokenAsync(string token)
    {
        var rt = await _ctx.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token && !rt.IsRevoked);

        if (rt is not null)
        {
            rt.IsRevoked = true;
            await _ctx.SaveChangesAsync();
        }
    }

    public async Task RevokeAllRefreshTokensAsync(int userId)
        => await _ctx.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.IsRevoked, true));

    public async Task<string?> GetPasswordHashAsync(int userId)
        => await _ctx.Users
            .Where(u => u.UserId == userId)
            .Select(u => u.PasswordHash)
            .FirstOrDefaultAsync();

    public async Task UpdatePasswordAsync(int userId, string newPasswordHash)
        => await _ctx.Users
            .Where(u => u.UserId == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.PasswordHash, newPasswordHash)
                .SetProperty(u => u.UpdatedAt, DateTime.UtcNow));
}
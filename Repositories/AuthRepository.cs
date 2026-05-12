using KLCN_API.Data;
using KLCN_API.Models.Entities;
using KLCN_API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KLCN_API.Repositories;

// ── AuthRepository ────────────────────────────────────────────────

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
        // Dùng transaction để tạo User + Profile atomic:
        // phải SaveChanges sau khi add User để lấy IDENTITY UserId,
        // sau đó mới set profile.UserId và add Profile.
        await using var tx = await _ctx.Database.BeginTransactionAsync();

        await _ctx.Users.AddAsync(user);
        await _ctx.SaveChangesAsync(); // lấy user.UserId

        profile.UserId = user.UserId;
        await _ctx.Profiles.AddAsync(profile);
        await _ctx.SaveChangesAsync();

        await tx.CommitAsync();

        // Load navigation để GenerateAccessToken và BuildLoginResponse hoạt động đúng
        await _ctx.Entry(user).Reference(u => u.Role).LoadAsync();
        await _ctx.Entry(user).Reference(u => u.Status).LoadAsync();
        user.Profile = profile;

        return user;
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
        => await _ctx.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.Role)
            .Include(rt => rt.User)
                .ThenInclude(u => u.Status)
            .FirstOrDefaultAsync(rt => rt.Token == token && !rt.IsRevoked);

    public async Task AddRefreshTokenAsync(RefreshToken token)
    {
        await _ctx.RefreshTokens.AddAsync(token);
        await _ctx.SaveChangesAsync();
    }

    public async Task RevokeRefreshTokenAsync(string token)
    {
        await _ctx.RefreshTokens
            .Where(rt => rt.Token == token && !rt.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.IsRevoked, true));
    }

    public async Task RevokeAllRefreshTokensAsync(int userId)
        => await _ctx.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.IsRevoked, true));
}
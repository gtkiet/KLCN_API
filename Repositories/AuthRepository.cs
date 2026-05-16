using KLCN_API.Data;
using KLCN_API.Models.Entities;
using KLCN_API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace KLCN_API.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly SportPlusDbContext _ctx;
    private readonly IMemoryCache _cache;

    private static string OtpKey(int userId) => $"otp:{userId}";
    private static string ResetTokenKey(string hash) => $"rst:{hash}";

    public AuthRepository(SportPlusDbContext ctx, IMemoryCache cache)
    {
        _ctx = ctx;
        _cache = cache;
    }

    // ── User lookup ───────────────────────────────────────────────

    public async Task<User?> GetByEmailAsync(string email)
        => await _ctx.Users
            .Include(u => u.Role)
            .Include(u => u.Status)
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);

    public async Task<User?> GetByPhoneAsync(string phone)
        => await _ctx.Users
            .Include(u => u.Role)
            .Include(u => u.Status)
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Phone == phone && !u.IsDeleted);

    public async Task<bool> EmailExistsAsync(string email)
        => await _ctx.Users.AnyAsync(u => u.Email == email && !u.IsDeleted);

    public async Task<bool> PhoneExistsAsync(string phone)
        => await _ctx.Users.AnyAsync(u => u.Phone == phone && !u.IsDeleted);

    // ── Register ──────────────────────────────────────────────────

    public async Task<User> CreateUserAsync(User user, Profile profile)
    {
        await using var tx = await _ctx.Database.BeginTransactionAsync();

        await _ctx.Users.AddAsync(user);
        await _ctx.SaveChangesAsync();

        profile.UserId = user.UserId;
        await _ctx.Profiles.AddAsync(profile);
        await _ctx.SaveChangesAsync();

        await tx.CommitAsync();

        await _ctx.Entry(user).Reference(u => u.Role).LoadAsync();
        await _ctx.Entry(user).Reference(u => u.Status).LoadAsync();
        user.Profile = profile;

        return user;
    }

    // ── Refresh token ─────────────────────────────────────────────

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
        => await _ctx.RefreshTokens
            .Include(rt => rt.User).ThenInclude(u => u.Role)
            .Include(rt => rt.User).ThenInclude(u => u.Status)
            .FirstOrDefaultAsync(rt => rt.Token == token && !rt.IsRevoked);

    public async Task AddRefreshTokenAsync(RefreshToken token)
    {
        await _ctx.RefreshTokens.AddAsync(token);
        await _ctx.SaveChangesAsync();
    }

    public async Task RevokeRefreshTokenAsync(string token)
        => await _ctx.RefreshTokens
            .Where(rt => rt.Token == token && !rt.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.IsRevoked, true));

    public async Task RevokeAllRefreshTokensAsync(int userId)
        => await _ctx.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.IsRevoked, true));

    // ── Password reset — OTP ──────────────────────────────────────

    public Task SaveOtpAsync(int userId, string otpHash, DateTime expiresAt)
    {
        _cache.Set(OtpKey(userId),
            (OtpHash: otpHash, ExpiresAt: expiresAt),
            absoluteExpiration: expiresAt);
        return Task.CompletedTask;
    }

    public Task<(string OtpHash, DateTime ExpiresAt)?> GetValidOtpAsync(int userId)
    {
        if (_cache.TryGetValue(OtpKey(userId),
                out (string OtpHash, DateTime ExpiresAt) entry)
            && entry.ExpiresAt > DateTime.UtcNow)
            return Task.FromResult<(string, DateTime)?>(entry);

        return Task.FromResult<(string, DateTime)?>(null);
    }

    public Task ClearOtpAsync(int userId)
    {
        _cache.Remove(OtpKey(userId));
        return Task.CompletedTask;
    }

    // ── Password reset — Reset token ──────────────────────────────

    public Task SaveResetTokenAsync(int userId, string tokenHash, DateTime expiresAt)
    {
        _cache.Set(ResetTokenKey(tokenHash),
            (UserId: userId, ExpiresAt: expiresAt),
            absoluteExpiration: expiresAt);
        return Task.CompletedTask;
    }

    public Task<(int UserId, DateTime ExpiresAt)?> GetValidResetTokenAsync(string tokenHash)
    {
        if (_cache.TryGetValue(ResetTokenKey(tokenHash),
                out (int UserId, DateTime ExpiresAt) entry)
            && entry.ExpiresAt > DateTime.UtcNow)
            return Task.FromResult<(int, DateTime)?>(entry);

        return Task.FromResult<(int, DateTime)?>(null);
    }

    public Task ClearResetTokenAsync(string tokenHash)
    {
        _cache.Remove(ResetTokenKey(tokenHash));
        return Task.CompletedTask;
    }
}
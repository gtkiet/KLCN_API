//using KLCN_API.Data;
//using KLCN_API.Models.Entities;
//using KLCN_API.Repositories.Interfaces;
//using Microsoft.EntityFrameworkCore;

//namespace KLCN_API.Repositories;

//public class UserRepository : IUserRepository
//{
//    private readonly SportPlusDbContext _ctx;

//    public UserRepository(SportPlusDbContext ctx) => _ctx = ctx;

//    public async Task<User?> GetByIdAsync(int userId)
//        => await _ctx.Users
//            .Include(u => u.Role)
//            .Include(u => u.Status)
//            .Include(u => u.Profile)
//            .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted);

//    public async Task<(List<User> Items, int TotalCount)> GetUsersAsync(
//        string? search, int? roleId, int? statusId, int page, int pageSize)
//    {
//        var query = _ctx.Users
//            .Include(u => u.Role)
//            .Include(u => u.Status)
//            .Where(u => !u.IsDeleted)
//            .AsQueryable();

//        if (!string.IsNullOrWhiteSpace(search))
//        {
//            var s = search.Trim().ToLower();
//            query = query.Where(u =>
//                u.FullName.ToLower().Contains(s) ||
//                u.Email.ToLower().Contains(s) ||
//                u.Phone.Contains(s));
//        }

//        if (roleId.HasValue)
//            query = query.Where(u => u.RoleId == roleId.Value);

//        if (statusId.HasValue)
//            query = query.Where(u => u.StatusId == statusId.Value);

//        query = query.OrderByDescending(u => u.CreatedAt);

//        var totalCount = await query.CountAsync();
//        var items = await query
//            .Skip((page - 1) * pageSize)
//            .Take(pageSize)
//            .ToListAsync();

//        return (items, totalCount);
//    }

//    public async Task UpdateProfileAsync(int userId, string? fullName, string? phone,
//        string? avatarUrl, DateOnly? dateOfBirth, string? address)
//    {
//        var user = await _ctx.Users
//            .Include(u => u.Profile)
//            .FirstOrDefaultAsync(u => u.UserId == userId);

//        if (user is null) return;

//        if (fullName is not null) user.FullName = fullName;
//        if (phone is not null) user.Phone = phone;
//        user.UpdatedAt = DateTime.UtcNow;

//        if (user.Profile is not null)
//        {
//            if (avatarUrl is not null) user.Profile.AvatarUrl = avatarUrl;
//            if (dateOfBirth.HasValue) user.Profile.DateOfBirth = dateOfBirth;
//            if (address is not null) user.Profile.Address = address;
//        }

//        await _ctx.SaveChangesAsync();
//    }

//    public async Task UpdateStatusAsync(int userId, int statusId)
//        => await _ctx.Users
//            .Where(u => u.UserId == userId)
//            .ExecuteUpdateAsync(s => s
//                .SetProperty(u => u.StatusId, statusId)
//                .SetProperty(u => u.UpdatedAt, DateTime.UtcNow));

//    public async Task SoftDeleteAsync(int userId)
//        => await _ctx.Users
//            .Where(u => u.UserId == userId)
//            .ExecuteUpdateAsync(s => s
//                .SetProperty(u => u.IsDeleted, true)
//                .SetProperty(u => u.UpdatedAt, DateTime.UtcNow));

//    public async Task UpdatePasswordAsync(int userId, string newPasswordHash)
//        => await _ctx.Users
//            .Where(u => u.UserId == userId)
//            .ExecuteUpdateAsync(s => s
//                .SetProperty(u => u.PasswordHash, newPasswordHash)
//                .SetProperty(u => u.UpdatedAt, DateTime.UtcNow));
//}
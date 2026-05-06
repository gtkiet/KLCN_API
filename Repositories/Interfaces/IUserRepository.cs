using KLCN_API.Models.Entities;

namespace KLCN_API.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int userId);
    Task<(List<User> Items, int TotalCount)> GetUsersAsync(
        string? search, int? roleId, int? statusId, int page, int pageSize);
    Task UpdateProfileAsync(int userId, string? fullName, string? phone,
        string? avatarUrl, DateOnly? dateOfBirth, string? address);
    Task UpdateStatusAsync(int userId, int statusId);
    Task SoftDeleteAsync(int userId);
    Task UpdatePasswordAsync(int userId, string newPasswordHash);
}
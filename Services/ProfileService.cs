using KLCN_API.Helpers;
using KLCN_API.Middleware;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Entities;
using KLCN_API.Repositories.Interfaces;
using KLCN_API.Services.Interfaces;

namespace KLCN_API.Services;

public class ProfileService : IProfileService
{
    private readonly IUserRepository _userRepo;

    public ProfileService(IUserRepository userRepo) => _userRepo = userRepo;

    public async Task<UserDetailResponse> GetProfileAsync(int userId)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("Người dùng", userId);

        return MapDetail(user);
    }

    public async Task<UserDetailResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        await _userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("Người dùng", userId);

        await _userRepo.UpdateProfileAsync(
            userId,
            request.FullName,
            request.Phone,
            avatarUrl: null,    // avatar tách riêng
            request.DateOfBirth,
            request.Address);

        var updated = await _userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("Người dùng", userId);

        return MapDetail(updated);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("Người dùng", userId);

        if (!PasswordHelper.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            throw new BusinessException("Mật khẩu hiện tại không đúng.", 400);

        if (request.NewPassword == request.CurrentPassword)
            throw new BusinessException("Mật khẩu mới không được trùng mật khẩu cũ.", 400);

        await _userRepo.UpdatePasswordAsync(userId, PasswordHelper.HashPassword(request.NewPassword));
    }

    private static UserDetailResponse MapDetail(User u) => new()
    {
        UserId = u.UserId,
        FullName = u.FullName,
        Email = u.Email,
        Phone = u.Phone,
        Role = u.Role?.Name ?? string.Empty,
        RoleId = u.RoleId,
        Status = u.Status?.Name ?? string.Empty,
        StatusId = u.StatusId,
        CreatedAt = u.CreatedAt,
        Profile = u.Profile is null ? null : new ProfileResponse
        {
            AvatarUrl = u.Profile.AvatarUrl,
            DateOfBirth = u.Profile.DateOfBirth,
            Address = u.Profile.Address
        }
    };
}
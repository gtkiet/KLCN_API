using KLCN_API.Helpers;
using KLCN_API.Middleware;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Entities;
using KLCN_API.Models.Enums;
using KLCN_API.Repositories.Interfaces;
using KLCN_API.Services.Interfaces;

namespace KLCN_API.Services;


public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;

    public UserService(IUserRepository userRepo) => _userRepo = userRepo;

    public async Task<UserDetailResponse> GetByIdAsync(int userId)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("Người dùng", userId);

        return UserMapper.ToDetailResponse(user);
    }

    public async Task<PagedResponse<UserResponse>> GetUsersAsync(GetUsersRequest request)
    {
        var (items, total) = await _userRepo.GetUsersAsync(
            request.Search, request.RoleId, request.StatusId,
            request.Page, request.PageSize);

        return new PagedResponse<UserResponse>
        {
            Items = items.Select(UserMapper.ToResponse).ToList(),
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task LockUserAsync(int userId)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("Người dùng", userId);

        if (user.RoleId == (int)RoleEnum.Admin)
            throw new ForbiddenException("Không thể khóa tài khoản Admin.");

        if (user.StatusId == (int)UserStatusEnum.Locked)
            throw new BusinessException("Tài khoản đã bị khóa rồi.", 400);

        await _userRepo.UpdateStatusAsync(userId, (int)UserStatusEnum.Locked);
    }

    public async Task UnlockUserAsync(int userId)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("Người dùng", userId);

        if (user.StatusId == (int)UserStatusEnum.Active)
            throw new BusinessException("Tài khoản đang hoạt động bình thường.", 400);

        await _userRepo.UpdateStatusAsync(userId, (int)UserStatusEnum.Active);
    }

    public async Task DeleteUserAsync(int userId)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("Người dùng", userId);

        if (user.RoleId == (int)RoleEnum.Admin)
            throw new ForbiddenException("Không thể xóa tài khoản Admin.");

        await _userRepo.SoftDeleteAsync(userId);
    }
}
using BCrypt.Net;
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

    public async Task<UserDetailResponse> CreateStaffAsync(CreateStaffRequest request)
    {
        var existed = await _userRepo.GetByEmailAsync(request.Email);
        if (existed != null)
            throw new BusinessException("Email đã tồn tại trong hệ thống.", 400);

        var existedPhone = await _userRepo.GetByPhoneAsync(request.Phone);
        if (existedPhone != null)
            throw new BusinessException("Số điện thoại đã tồn tại trong hệ thống.", 400);

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email.Trim().ToLower(),
            Phone = request.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId = (int)RoleEnum.Staff,
            StatusId = request.StatusId <= 0 ? (int)UserStatusEnum.Active : request.StatusId,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _userRepo.CreateAsync(user);

        var created = await _userRepo.GetByIdAsync(user.UserId)
            ?? throw new NotFoundException("Người dùng", user.UserId);

        return UserMapper.ToDetailResponse(created);
    }

    public async Task<UserDetailResponse> CreateCustomerByAdminAsync(CreateCustomerByAdminRequest request)
    {
        var existed = await _userRepo.GetByEmailAsync(request.Email);
        if (existed != null)
            throw new BusinessException("Email đã tồn tại trong hệ thống.", 400);

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email.Trim().ToLower(),
            Phone = request.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId = (int)RoleEnum.Customer,
            StatusId = request.StatusId <= 0 ? (int)UserStatusEnum.Active : request.StatusId,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _userRepo.CreateAsync(user);

        var created = await _userRepo.GetByIdAsync(user.UserId)
            ?? throw new NotFoundException("Người dùng", user.UserId);

        return UserMapper.ToDetailResponse(created);
    }

    public async Task<UserDetailResponse> UpdateUserAsync(int userId, UpdateUserRequest request)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("Người dùng", userId);

        var duplicate = await _userRepo.GetByEmailAsync(request.Email);
        if (duplicate != null && duplicate.UserId != userId)
            throw new BusinessException("Email đã tồn tại trong hệ thống.", 400);

        var duplicatePhone = await _userRepo.GetByPhoneAsync(request.Phone);
        if (duplicatePhone != null && duplicatePhone.UserId != userId)
            throw new BusinessException("Số điện thoại đã tồn tại trong hệ thống.", 400);

        user.FullName = request.FullName;
        user.Email = request.Email.Trim().ToLower();
        user.Phone = request.Phone;
        user.StatusId = request.StatusId <= 0 ? user.StatusId : request.StatusId;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(user);

        var updated = await _userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("Người dùng", userId);

        return UserMapper.ToDetailResponse(updated);
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
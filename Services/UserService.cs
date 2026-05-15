using KLCN_API.Helpers;
using KLCN_API.Mappers;
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
        if (await _userRepo.GetByEmailAsync(request.Email) is not null)
            throw new ConflictException("Email đã tồn tại trong hệ thống.");

        if (await _userRepo.GetByPhoneAsync(request.Phone) is not null)
            throw new ConflictException("Số điện thoại đã tồn tại trong hệ thống.");

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLower(),
            Phone = request.Phone.Trim(),
            PasswordHash = PasswordHelper.HashPassword(request.Password),
            RoleId = (int)RoleEnum.Staff,
            StatusId = (int)UserStatusEnum.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _userRepo.CreateAsync(user);

        return UserMapper.ToDetailResponse(
            await _userRepo.GetByIdAsync(user.UserId)
                ?? throw new NotFoundException("Người dùng", user.UserId));
    }

    public async Task<UserDetailResponse> CreateCustomerByAdminAsync(
        CreateCustomerByAdminRequest request)
    {
        if (await _userRepo.GetByEmailAsync(request.Email) is not null)
            throw new ConflictException("Email đã tồn tại trong hệ thống.");

        if (await _userRepo.GetByPhoneAsync(request.Phone) is not null)
            throw new ConflictException("Số điện thoại đã tồn tại trong hệ thống.");

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLower(),
            Phone = request.Phone.Trim(),
            PasswordHash = PasswordHelper.HashPassword(request.Password),
            RoleId = (int)RoleEnum.Customer,
            StatusId = (int)UserStatusEnum.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _userRepo.CreateAsync(user);

        return UserMapper.ToDetailResponse(
            await _userRepo.GetByIdAsync(user.UserId)
                ?? throw new NotFoundException("Người dùng", user.UserId));
    }

    //public async Task<UserDetailResponse> UpdateUserAsync(int userId, UpdateUserRequest request)
    //{
    //    var user = await _userRepo.GetByIdAsync(userId)
    //        ?? throw new NotFoundException("Người dùng", userId);

    //    // Kiểm tra duplicate email/phone — bỏ qua nếu là chính user đó
    //    var byEmail = await _userRepo.GetByEmailAsync(request.Email);
    //    if (byEmail is not null && byEmail.UserId != userId)
    //        throw new ConflictException("Email đã tồn tại trong hệ thống.");

    //    var byPhone = await _userRepo.GetByPhoneAsync(request.Phone);
    //    if (byPhone is not null && byPhone.UserId != userId)
    //        throw new ConflictException("Số điện thoại đã tồn tại trong hệ thống.");

    //    user.FullName = request.FullName.Trim();
    //    user.Email = request.Email.Trim().ToLower();
    //    user.Phone = request.Phone.Trim();
    //    user.StatusId = request.StatusId > 0 ? request.StatusId : user.StatusId;
    //    user.UpdatedAt = DateTime.UtcNow;

    //    await _userRepo.UpdateAsync(user);

    //    return UserMapper.ToDetailResponse(
    //        await _userRepo.GetByIdAsync(userId)
    //            ?? throw new NotFoundException("Người dùng", userId));
    //}

    /// <summary>
    /// Đổi role user — chỉ Admin, không thể đổi role của chính mình,
    /// không thể gán/bỏ role Admin.
    /// requesterId: userId của Admin đang thực hiện.
    /// </summary>
    public async Task UpdateRoleAsync(int userId, int roleId, int requesterId)
    {
        if (userId == requesterId)
            throw new BusinessException("Không thể đổi role của chính mình.", 400);

        if (!Enum.IsDefined(typeof(RoleEnum), roleId))
            throw new BusinessException("Role không hợp lệ.", 400);

        if (roleId == (int)RoleEnum.Admin)
            throw new ForbiddenException("Không thể gán role Admin.");

        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("Người dùng", userId);

        if (user.RoleId == (int)RoleEnum.Admin)
            throw new ForbiddenException("Không thể thay đổi role của tài khoản Admin.");

        if (user.RoleId == roleId)
            throw new BusinessException("User đã có role này rồi.", 400);

        await _userRepo.UpdateRoleAsync(userId, roleId);
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
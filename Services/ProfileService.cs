using KLCN_API.Helpers;
using KLCN_API.Mappers;
using KLCN_API.Middleware;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Repositories.Interfaces;
using KLCN_API.Services.Interfaces;

namespace KLCN_API.Services;

public class ProfileService : IProfileService
{
    private readonly IUserRepository _userRepo;
    private readonly IWebHostEnvironment _env;

    public ProfileService(IUserRepository userRepo, IWebHostEnvironment env)
    {
        _userRepo = userRepo;
        _env = env;
    }

    public async Task<UserDetailResponse> GetProfileAsync(int userId)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("Người dùng", userId);

        return UserMapper.ToDetailResponse(user);
    }

    public async Task<UserDetailResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        // Kiểm tra tồn tại trước
        _ = await _userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("Người dùng", userId);

        await _userRepo.UpdateProfileAsync(
            userId,
            request.FullName,
            request.Phone,
            avatarUrl: null,  // avatar xử lý qua endpoint riêng
            request.DateOfBirth,
            request.Address);

        var updated = await _userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("Người dùng", userId);

        return UserMapper.ToDetailResponse(updated);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("Người dùng", userId);

        if (!PasswordHelper.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            throw new BusinessException("Mật khẩu hiện tại không đúng.", 400);

        if (request.NewPassword == request.CurrentPassword)
            throw new BusinessException("Mật khẩu mới không được trùng mật khẩu cũ.", 400);

        await _userRepo.UpdatePasswordAsync(
            userId, PasswordHelper.HashPassword(request.NewPassword));
    }

    /// <summary>
    /// Lưu file avatar mới, xóa file cũ (nếu có), cập nhật đường dẫn trong DB.
    /// Trả về URL truy cập qua /Uploads/avatars/{filename}.
    /// </summary>
    public async Task<string> UpdateAvatarAsync(int userId, IFormFile file)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("Người dùng", userId);

        // Validate file
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            throw new BusinessException("Chỉ chấp nhận file ảnh JPG, PNG hoặc WebP.", 400);

        const long maxSize = 5 * 1024 * 1024; // 5 MB
        if (file.Length > maxSize)
            throw new BusinessException("Kích thước ảnh không được vượt quá 5MB.", 400);

        // Thư mục lưu: {ContentRoot}/Uploads/avatars/
        var avatarDir = Path.Combine(_env.ContentRootPath, "Uploads", "avatars");
        if (!Directory.Exists(avatarDir))
            Directory.CreateDirectory(avatarDir);

        // Xóa ảnh cũ nếu có và file tồn tại trên disk
        var oldUrl = user.Profile?.AvatarUrl;
        if (!string.IsNullOrEmpty(oldUrl))
        {
            // URL dạng /Uploads/avatars/xxx.jpg -> lấy filename
            var oldFileName = Path.GetFileName(oldUrl);
            var oldFilePath = Path.Combine(avatarDir, oldFileName);
            if (File.Exists(oldFilePath))
                File.Delete(oldFilePath);
        }

        // Tạo tên file mới: {userId}_{guid}{ext} — tránh trùng lặp
        var ext = Path.GetExtension(file.FileName).ToLower();
        var fileName = $"{userId}_{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(avatarDir, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        var avatarUrl = $"/Uploads/avatars/{fileName}";

        await _userRepo.UpdateProfileAsync(
            userId,
            fullName: null,
            phone: null,
            avatarUrl: avatarUrl,
            dateOfBirth: null,
            address: null);

        return avatarUrl;
    }
}

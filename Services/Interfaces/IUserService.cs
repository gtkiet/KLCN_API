using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;

namespace KLCN_API.Services.Interfaces;

public interface IUserService
{
    Task<UserDetailResponse> GetByIdAsync(int userId);
    Task<PagedResponse<UserResponse>> GetUsersAsync(GetUsersRequest request);
    Task<UserDetailResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request);
    Task LockUserAsync(int userId);
    Task UnlockUserAsync(int userId);
    Task DeleteUserAsync(int userId);
}
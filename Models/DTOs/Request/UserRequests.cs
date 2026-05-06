using System.ComponentModel.DataAnnotations;

namespace KLCN_API.Models.DTOs.Request;

public class UpdateProfileRequest
{
    [MaxLength(100)]
    public string? FullName { get; set; }

    [MaxLength(20)]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string? Phone { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [MaxLength(255)]
    public string? Address { get; set; }

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }
}

public class GetUsersRequest
{
    public string? Search { get; set; }   // tìm theo tên, email, phone
    public int? RoleId { get; set; }
    public int? StatusId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
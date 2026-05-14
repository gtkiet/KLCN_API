using KLCN_API.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace KLCN_API.Models.DTOs.Request;

// ================================================================
// Auth
// ================================================================

public class RegisterRequest
{
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại không được để trống.")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự.")]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ tên không được để trống.")]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    public string Password { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Access token không được để trống.")]
    public string AccessToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "Refresh token không được để trống.")]
    public string RefreshToken { get; set; } = string.Empty;
}

// ================================================================
// Profile
// ================================================================

public class UpdateProfileRequest
{
    [MaxLength(100)]
    public string? FullName { get; set; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [MaxLength(20)]
    public string? Phone { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [MaxLength(255)]
    public string? Address { get; set; }
}

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Mật khẩu hiện tại không được để trống.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu mới không được để trống.")]
    [MinLength(6, ErrorMessage = "Mật khẩu mới tối thiểu 6 ký tự.")]
    [MaxLength(100)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Xác nhận mật khẩu không được để trống.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

// ================================================================
// Users
// ================================================================

public class GetUsersRequest
{
    public string? Search { get; set; }
    public int? RoleId { get; set; }
    public int? StatusId { get; set; }
    public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
}

public class CreateStaffRequest
{
    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Phone { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public int StatusId { get; set; } = 1;
}

public class CreateCustomerByAdminRequest
{
    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Phone { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = "123456";

    public int StatusId { get; set; } = 1;
}

public class UpdateUserRequest
{
    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Phone { get; set; } = string.Empty;

    public int StatusId { get; set; } = 1;
}

// ================================================================
// Fields
// ================================================================

public class CreateFieldRequest
{
    [Required(ErrorMessage = "Tên sân không được để trống.")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [Range(1, double.MaxValue, ErrorMessage = "Giá cơ bản phải lớn hơn 0.")]
    public decimal BasePrice { get; set; }

    [Required]
    [Range(1, double.MaxValue, ErrorMessage = "Giá cao điểm phải lớn hơn 0.")]
    public decimal PeakPrice { get; set; }

    [Required]
    public int TypeId { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }
}

public class UpdateFieldRequest
{
    [MaxLength(100)] public string? Name { get; set; }
    [MaxLength(500)] public string? Description { get; set; }
    [Range(1, double.MaxValue)] public decimal? BasePrice { get; set; }
    [Range(1, double.MaxValue)] public decimal? PeakPrice { get; set; }
    public int? TypeId { get; set; }
    public int? StatusId { get; set; }
    [MaxLength(500)] public string? ImageUrl { get; set; }
    [MaxLength(255)] public string? PriceChangeReason { get; set; }
}

public class GetFieldsRequest
{
    public string? Search { get; set; }
    public int? TypeId { get; set; }
    public int? StatusId { get; set; }
    public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
}

public class GetFieldScheduleRequest
{
    public int? FieldId { get; set; }
    [Required(ErrorMessage = "Ngày xem lịch không được để trống.")]
    public DateOnly Date { get; set; }
    public int? TypeId { get; set; }
}

// ================================================================
// Bookings
// ================================================================

public class HoldSlotsRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "Phải chọn ít nhất 1 slot.")]
    public List<int> FieldSlotIds { get; set; } = [];
}

/// <summary>
/// Tạo booking. Tất cả booking đều BẮT BUỘC đặt cọc qua MoMo.
/// Sau khi tạo thành công (StatusId=5 Chờ đặt cọc), client gọi tiếp
/// POST /api/payments/momo/create/{bookingId} để lấy URL thanh toán cọc.
/// </summary>
public class CreateBookingRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "Phải chọn ít nhất 1 slot.")]
    public List<int> FieldSlotIds { get; set; } = [];

    public List<BookingServiceItem> Services { get; set; } = [];

    [MaxLength(50)]
    public string? PromotionCode { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
    // IsFullPayment đã bỏ — luôn tạo deposit, bắt buộc thanh toán cọc qua MoMo
}

public class BookingServiceItem
{
    [Required]
    public int ServiceId { get; set; }

    [Range(1, 100, ErrorMessage = "Số lượng phải từ 1 đến 100.")]
    public int Quantity { get; set; } = 1;
}

/// <summary>
/// Ghi nhận thanh toán phần còn lại tại quầy — Staff dùng.
/// MethodId: 1=Trực tiếp, 2=MoMo, 3=VNPay.
/// </summary>
public class ConfirmPaymentRequest
{
    [Required(ErrorMessage = "Phương thức thanh toán không được để trống.")]
    [Range(1, 3, ErrorMessage = "Phương thức thanh toán không hợp lệ.")]
    public int MethodId { get; set; } = (int)PaymentMethodEnum.Direct;

    [MaxLength(100)]
    public string? TransactionCode { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}

public class CancelBookingRequest
{
    [MaxLength(500)]
    public string? Reason { get; set; }
}

public class RescheduleRequest
{
    [Required]
    public int BookingDetailId { get; set; }

    [Required]
    public int NewFieldSlotId { get; set; }
}

public class ApplyVoucherRequest
{
    [Required(ErrorMessage = "Mã voucher không được để trống.")]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;
}

public class GetBookingsRequest
{
    public int? UserId { get; set; }
    public int? StatusId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public int? FieldId { get; set; }
    public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
}

// ================================================================
// Promotions
// ================================================================
public class GetPromotionRequest
{
    public bool? isActive { get; set; }
    public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
}

public class CreatePromotionRequest
{
    [Required][MaxLength(50)] public string Code { get; set; } = string.Empty;
    [Required][MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    [Required] public int TypeId { get; set; }
    [Required][Range(0.01, double.MaxValue)] public decimal DiscountValue { get; set; }
    [Range(0, double.MaxValue)] public decimal? MaxDiscount { get; set; }
    [Range(0, double.MaxValue)] public decimal MinOrderAmount { get; set; } = 0;
    [Range(1, int.MaxValue)] public int UsageLimit { get; set; } = 1;
    [Required] public DateOnly StartDate { get; set; }
    [Required] public DateOnly EndDate { get; set; }
}

public class UpdatePromotionRequest
{
    [MaxLength(200)] public string? Name { get; set; }
    [MaxLength(500)] public string? Description { get; set; }
    public decimal? DiscountValue { get; set; }
    public decimal? MaxDiscount { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public int? UsageLimit { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool? IsActive { get; set; }
}

// ================================================================
// Services
// ================================================================

public class CreateServiceRequest
{
    [Required(ErrorMessage = "Tên dịch vụ không được để trống.")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    [Required][Range(1, double.MaxValue)] public decimal Price { get; set; }
    [MaxLength(500)] public string? ImageUrl { get; set; }
}

public class UpdateServiceRequest
{
    [MaxLength(100)] public string? Name { get; set; }
    [MaxLength(500)] public string? Description { get; set; }
    [Range(1, double.MaxValue)] public decimal? Price { get; set; }
    [MaxLength(500)] public string? ImageUrl { get; set; }
    public bool? IsAvailable { get; set; }
}

// ================================================================
// Inventory
// ================================================================

public class CreateSupplierRequest
{
    [Required(ErrorMessage = "Tên nhà cung cấp không được để trống.")]
    [MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(100)] public string? ContactName { get; set; }
    [Phone][MaxLength(20)] public string? Phone { get; set; }
    [EmailAddress][MaxLength(100)] public string? Email { get; set; }
    [MaxLength(255)] public string? Address { get; set; }
}

public class UpdateSupplierRequest
{
    [MaxLength(100)] public string? Name { get; set; }
    [MaxLength(100)] public string? ContactName { get; set; }
    [Phone][MaxLength(20)] public string? Phone { get; set; }
    [EmailAddress][MaxLength(100)] public string? Email { get; set; }
    [MaxLength(255)] public string? Address { get; set; }
}

public class CreateProductRequest
{
    [Required(ErrorMessage = "Tên sản phẩm không được để trống.")]
    [MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(50)] public string? Unit { get; set; }
    [Range(0, int.MaxValue)] public int MinQty { get; set; } = 5;
}

public class CreatePurchaseOrderRequest
{
    [Required] public int SupplierId { get; set; }
    [MaxLength(500)] public string? Note { get; set; }
    [Required][MinLength(1)] public List<PurchaseOrderDetailItem> Items { get; set; } = [];
}

public class PurchaseOrderDetailItem
{
    [Required] public int ProductId { get; set; }
    [Range(1, int.MaxValue)] public int Quantity { get; set; }
    [Range(0.01, double.MaxValue)] public decimal UnitPrice { get; set; }
}

// ================================================================
// Incidents
// ================================================================

public class CreateIncidentRequest
{
    [Required] public int FieldId { get; set; }
    [Required][MaxLength(200)] public string Title { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Description { get; set; }
    [MaxLength(500)] public string? ImageUrl { get; set; }
}

public class HandleIncidentRequest
{
    [Required][Range(2, 3)] public int StatusId { get; set; }
    [MaxLength(500)] public string? HandledNote { get; set; }
}

// ================================================================
// Reviews
// ================================================================

public class CreateReviewRequest
{
    [Required] public int BookingId { get; set; }
    [Required][Range(1, 5)] public int Rating { get; set; }
    [MaxLength(1000)] public string? Comment { get; set; }
    [MaxLength(500)] public string? ImageUrl { get; set; }
}

public class GetReviewsRequest
{
    public int? FieldId { get; set; }
    public int? Rating { get; set; }
    public bool? IsVisible { get; set; }
    public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
}

// ================================================================
// Special days
// ================================================================

public class CreateSpecialDayRequest
{
    [Required] public DateOnly SpecialDate { get; set; }
    [Required][MaxLength(100)] public string Name { get; set; } = string.Empty;
    [Range(0.01, 10)] public decimal PriceMultiplier { get; set; } = 1.0m;
    public bool IsFullDayPeak { get; set; } = false;
    [MaxLength(255)] public string? Note { get; set; }
}

public class UpdateSpecialDayRequest
{
    [MaxLength(100)] public string? Name { get; set; }
    [Range(0.01, 10)] public decimal? PriceMultiplier { get; set; }
    public bool? IsFullDayPeak { get; set; }
    [MaxLength(255)] public string? Note { get; set; }
}

// ================================================================
// System config
// ================================================================

public class UpdateSystemConfigRequest
{
    [Required][MaxLength(500)] public string ConfigValue { get; set; } = string.Empty;
}

// ================================================================
// Maintenance & Slots & Notifications
// ================================================================

public class CreateMaintenanceRequest
{
    [Required][MaxLength(500)] public string Reason { get; set; } = string.Empty;
    [Required] public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}

public class GenerateSlotsRequest
{
    [Required] public DateOnly StartDate { get; set; }
    [Required] public DateOnly EndDate { get; set; }
}

public class GetNotificationsRequest
{
    public bool? IsRead { get; set; }
    public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
}
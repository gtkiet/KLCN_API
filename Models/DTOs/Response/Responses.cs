namespace KLCN_API.Models.DTOs.Response;

// ================================================================
// Wrapper chuẩn
// ================================================================

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Thành công")
        => new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, List<string>? errors = null)
        => new() { Success = false, Message = message, Errors = errors };
}

public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Ok(string message = "Thành công")
        => new() { Success = true, Message = message };

    public static new ApiResponse Fail(string message, List<string>? errors = null)
        => new() { Success = false, Message = message, Errors = errors };
}

public class PagedResponse<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

// ================================================================
// Auth
// ================================================================

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserResponse User { get; set; } = null!;
}

public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

/// <summary>Trả về sau khi verify OTP thành công.</summary>
public class VerifyOtpResponse
{
    /// <summary>
    /// Token tạm thời dùng để đặt lại mật khẩu ở bước 3.
    /// Hết hạn sau 15 phút.
    /// </summary>
    public string ResetToken { get; set; } = string.Empty;
}

// ================================================================
// Users & profile
// ================================================================

public class UserResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserDetailResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public DateTime CreatedAt { get; set; }
    public ProfileResponse? Profile { get; set; }
}

public class ProfileResponse
{
    public string? AvatarUrl { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Address { get; set; }
}

// ================================================================
// Fields
// ================================================================

public class FieldResponse
{
    public int FieldId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public decimal PeakPrice { get; set; }
    public string? ImageUrl { get; set; }
    public string FieldType { get; set; } = string.Empty;
    public int TypeId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public decimal? AvgRating { get; set; }
    public int TotalReviews { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class FieldScheduleResponse
{
    public int FieldId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateOnly SlotDate { get; set; }
    public List<SlotResponse> Slots { get; set; } = [];
}

public class SlotResponse
{
    public int FieldSlotId { get; set; }
    public int SlotId { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public decimal Price { get; set; }
    public bool IsPeakHour { get; set; }
    public string Status { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public int? HoldRemainingSeconds { get; set; }
}

public class FieldPriceHistoryResponse
{
    public int HistoryId { get; set; }
    public decimal OldBasePrice { get; set; }
    public decimal OldPeakPrice { get; set; }
    public decimal NewBasePrice { get; set; }
    public decimal NewPeakPrice { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? Reason { get; set; }
}

public class FieldMaintenanceLogResponse
{
    public int LogId { get; set; }
    public int FieldId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// ================================================================
// Bookings
// ================================================================

public class BookingResponse
{
    public int BookingId { get; set; }
    public UserResponse Customer { get; set; } = null!;
    public string Status { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public decimal? SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal? TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public string? PromotionCode { get; set; }
    public string? Note { get; set; }
    public string? CancelReason { get; set; }
    public int RescheduleCount { get; set; }
    public List<BookingDetailResponse> Details { get; set; } = [];
    public List<BookingServiceResponse> Services { get; set; } = [];
    public DepositResponse? Deposit { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class BookingSummaryResponse
{
    public int BookingId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public decimal? TotalAmount { get; set; }
    public int SlotCount { get; set; }
    public DateOnly? EarliestSlotDate { get; set; }
    public TimeOnly? EarliestSlotTime { get; set; }
    public string? FieldName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BookingDetailResponse
{
    public int BookingDetailId { get; set; }
    public int FieldId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public DateOnly SlotDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public decimal Price { get; set; }
}

public class BookingServiceResponse
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total => Quantity * UnitPrice;
}

// ================================================================
// Payments & deposits
// ================================================================

public class PaymentResponse
{
    public int PaymentId { get; set; }
    public int BookingId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public int MethodId { get; set; }
    public string? TransactionCode { get; set; }
    public string? Note { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DepositResponse
{
    public int DepositId { get; set; }
    public int BookingId { get; set; }
    public decimal RequiredAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public DateTime DeadlineAt { get; set; }
    public int MinutesLeft { get; set; }
    public DateTime? PaidAt { get; set; }
}

// ================================================================
// Promotions
// ================================================================

public class PromotionResponse
{
    public int PromotionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public int TypeId { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscount { get; set; }
    public decimal MinOrderAmount { get; set; }
    public int UsageLimit { get; set; }
    public int UsageCount { get; set; }
    public int UsageRemaining => UsageLimit - UsageCount;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ================================================================
// Services
// ================================================================

public class ServiceResponse
{
    public int ServiceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; }
}

// ================================================================
// Inventory
// ================================================================

public class SupplierResponse
{
    public int SupplierId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
}

public class ProductResponse
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public int StockQty { get; set; }
    public int MinQty { get; set; }
    public bool IsLowStock => StockQty <= MinQty;
    public int StockBuffer => StockQty - MinQty;
}

public class PurchaseOrderResponse
{
    public int PurchaseOrderId { get; set; }
    public SupplierResponse Supplier { get; set; } = null!;
    public string CreatedBy { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? Note { get; set; }
    public List<PurchaseOrderDetailResponse> Items { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
}

public class PurchaseOrderDetailResponse
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal => Quantity * UnitPrice;
}

// ================================================================
// Incidents
// ================================================================

public class IncidentResponse
{
    public int IncidentId { get; set; }
    public int FieldId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string ReportedBy { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string? HandledBy { get; set; }
    public DateTime? HandledAt { get; set; }
    public string? HandledNote { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ================================================================
// Reviews
// ================================================================

public class ReviewResponse
{
    public int ReviewId { get; set; }
    public int BookingId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int FieldId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsVisible { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class FieldRatingResponse
{
    public int FieldId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public decimal AvgRating { get; set; }
    public int TotalReviews { get; set; }
    public int Stars5 { get; set; }
    public int Stars4 { get; set; }
    public int Stars3 { get; set; }
    public int Stars2 { get; set; }
    public int Stars1 { get; set; }
    public List<ReviewResponse> Reviews { get; set; } = [];
}

// ================================================================
// Notifications
// ================================================================

public class NotificationResponse
{
    public int NotificationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public string? Type { get; set; }
    public int? RefId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ================================================================
// Dashboard & reports
// ================================================================

public class DashboardSummaryResponse
{
    public int PendingBookings { get; set; }
    public int PendingDepositBookings { get; set; }
    public int TodayConfirmed { get; set; }
    public int ActiveFields { get; set; }
    public int MaintenanceFields { get; set; }
    public int NewIncidents { get; set; }
    public decimal TodayRevenue { get; set; }
    public int ActiveCustomers { get; set; }
    public int LowStockCount { get; set; }
    public int UrgentDepositCount { get; set; }
}

public class RevenueByMonthResponse
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int TotalBookings { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AvgBookingValue { get; set; }
}

public class FieldOccupancyResponse
{
    public int FieldId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public int TotalSlots { get; set; }
    public int BookedSlots { get; set; }
    public decimal OccupancyRate { get; set; }
}

public class RevenueByServiceResponse
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int TotalQuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalBookings { get; set; }
}

// ================================================================
// System config
// ================================================================

public class SystemConfigResponse
{
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

// ================================================================
// Special days
// ================================================================

public class SpecialDayResponse
{
    public int SpecialDayId { get; set; }
    public DateOnly SpecialDate { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PriceMultiplier { get; set; }
    public bool IsFullDayPeak { get; set; }
    public string? Note { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// ================================================================
// Backups & restores
// ================================================================

/// <summary>Thông tin một file snapshot lưu trên server.</summary>
public class BackupSnapshotInfo
{
    public string FileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string SizeLabel => SizeBytes < 1024 * 1024
        ? $"{SizeBytes / 1024.0:F1} KB"
        : $"{SizeBytes / 1024.0 / 1024.0:F2} MB";
    public DateTime CreatedAt { get; set; }
    public string CreatedAtLabel => CreatedAt.AddHours(7).ToString("dd/MM/yyyy HH:mm:ss");
}

/// <summary>Báo cáo kết quả restore.</summary>
public class RestoreReportResponse
{
    /// <summary>Tên snapshot tự động tạo trước khi restore (để rollback thủ công nếu cần).</summary>
    public string PreRestoreSnapshot { get; set; } = string.Empty;

    /// <summary>Thời gian thực hiện restore (ms).</summary>
    public long ElapsedMs { get; set; }

    /// <summary>Số dòng đã restore từng bảng.</summary>
    public Dictionary<string, int> RestoredRows { get; set; } = [];

    /// <summary>Tổng số dòng đã restore.</summary>
    public int TotalRows => RestoredRows.Values.Sum();
}
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Entities;

namespace KLCN_API.Services.Interfaces;

// ================================================================
// Auth
// ================================================================

public interface IAuthService
{
    Task<LoginResponse> RegisterAsync(RegisterRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task LogoutAsync(int userId);
    Task ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<VerifyOtpResponse> VerifyOtpAsync(VerifyOtpRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request);
}

// ================================================================
// Users
// ================================================================

public interface IUserService
{
    Task<UserDetailResponse> GetByIdAsync(int userId);
    Task<PagedResponse<UserResponse>> GetUsersAsync(GetUsersRequest request);
    Task<UserDetailResponse> CreateStaffAsync(CreateStaffRequest request);
    Task<UserDetailResponse> CreateCustomerByAdminAsync(CreateCustomerByAdminRequest request);
    Task UpdateRoleAsync(int userId, int roleId, int requesterId);
    Task LockUserAsync(int userId);
    Task UnlockUserAsync(int userId);
    Task DeleteUserAsync(int userId);
}

// ================================================================
// Profile
// ================================================================

public interface IProfileService
{
    Task<UserDetailResponse> GetProfileAsync(int userId);
    Task<UserDetailResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request);
    Task ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task<string> UpdateAvatarAsync(int userId, IFormFile file);
}

// ================================================================
// Fields
// ================================================================

public interface IFieldService
{
    Task<PagedResponse<FieldResponse>> GetFieldsAsync(GetFieldsRequest request);
    Task<FieldResponse> GetByIdAsync(int fieldId);
    Task<FieldResponse> CreateAsync(int adminId, CreateFieldRequest request);
    Task<FieldResponse> UpdateAsync(int fieldId, int adminId, UpdateFieldRequest request);
    /// <summary>
    /// Upload ảnh sân, xóa ảnh cũ nếu có, lưu URL mới vào DB.
    /// Trả về relative URL "/Uploads/fields/{guid}.ext".
    /// </summary>
    Task<string> UploadImageAsync(int fieldId, IFormFile file, IWebHostEnvironment env);
    Task DeleteAsync(int fieldId);
    Task<List<FieldScheduleResponse>> GetScheduleAsync(GetFieldScheduleRequest request);
    Task GenerateSlotsAsync(GenerateSlotsRequest request);
    Task<List<FieldPriceHistoryResponse>> GetPriceHistoryAsync(int fieldId);
    Task<List<FieldMaintenanceLogResponse>> GetMaintenanceLogsAsync(int fieldId);
    Task AddMaintenanceLogAsync(int fieldId, int createdBy, CreateMaintenanceRequest request);
}

// ================================================================
// Bookings
// ================================================================

public interface IBookingService
{
    Task HoldSlotsAsync(HoldSlotsRequest request, int userId);

    /// <summary>
    /// Customer tự tạo booking.
    /// - IsFullPayment = false (mặc định): flow cọc → gọi vnpay/create để thanh toán cọc,
    ///   sau đó gọi vnpay/create lần 2 khi muốn thanh toán phần còn lại.
    /// - IsFullPayment = true: bỏ qua cọc, tạo URL thanh toán full luôn qua vnpay/create.
    /// </summary>
    Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request, int userId);

    Task<BookingResponse> GetByIdAsync(int bookingId, int requesterId, bool isAdminOrStaff);
    Task<PagedResponse<BookingSummaryResponse>> GetBookingsAsync(GetBookingsRequest request);
    Task<PagedResponse<BookingSummaryResponse>> GetMyBookingsAsync(
        int userId, int? statusId, int page, int pageSize);
    Task CancelAsync(int bookingId, CancelBookingRequest request, int userId, bool isAdminOverride);
    Task RescheduleAsync(int bookingId, RescheduleRequest request, int userId);
    Task AdminRescheduleAsync(int bookingId, RescheduleRequest request, int adminUserId);
    Task ApplyVoucherAsync(int bookingId, ApplyVoucherRequest request, int userId);
    Task<BookingResponse> CreateAdminWalkInBookingAsync(CreateAdminWalkInBookingRequest request, int actorUserId);
    Task CompleteAsync(int bookingId, int userId);
}

// ================================================================
// Payments
// ================================================================

public interface IPaymentService
{
    /// <summary>
    /// Ghi nhận đặt cọc từ IPN callback (MoMo/VNPay).
    /// Không cần userId vì là server-to-server.
    /// </summary>
    Task RecordDepositAsync(
        int bookingId, decimal amount, int methodId, string transactionCode);

    /// <summary>
    /// Ghi nhận thanh toán phần còn lại — Staff/Admin tại quầy.
    /// MethodId trong request: 1=Trực tiếp, 2=MoMo, 3=VNPay.
    /// </summary>
    Task RecordFullPaymentAsync(
        int bookingId, ConfirmPaymentRequest request, int userId);

    /// <summary>
    /// Router cho IPN và Return fallback: tự phân loại cọc hay thanh toán còn lại
    /// dựa trên StatusId hiện tại của booking.
    /// Idempotent theo transactionCode.
    /// </summary>
    Task RecordOnlinePaymentAsync(
        int bookingId, decimal amount, int methodId, string transactionCode);

    Task<List<PaymentResponse>> GetPaymentsByBookingAsync(int bookingId);
    Task<DepositResponse?> GetDepositByBookingAsync(int bookingId);

    /// <summary>
    /// Lấy booking để tạo payment URL.
    /// Cho phép các trạng thái: PendingDeposit(5), PendingPayment(1), Confirmed(2).
    /// </summary>
    Task<BookingResponse> GetBookingForPaymentAsync(int bookingId);

    /// <summary>
    /// Tính số tiền cần charge cho lần thanh toán tiếp theo.
    ///   PendingDeposit (5) → DepositAmount
    ///   PendingPayment (1) → TotalAmount (lần đầu, chưa có payment nào)
    ///   Confirmed      (2) → TotalAmount - TổngĐãTrả (phần còn lại)
    /// Tránh overcharge khi charge TotalAmount cứng ở lần thanh toán 2.
    /// </summary>
    Task<decimal> GetAmountDueAsync(int bookingId, BookingResponse booking);
}

// ================================================================
// Invoice
// ================================================================

/// <summary>
/// Tạo file PDF hóa đơn từ InvoiceDetailResponse.
/// Inject vào InvoicesController để trả về endpoint GET /api/invoices/{id}/pdf.
/// </summary>
public interface IInvoicePdfService
{
    /// <summary>Trả về mảng byte PDF sẵn sàng để trả về qua FileResult.</summary>
    Task<byte[]> GenerateAsync(InvoiceDetailResponse invoice);
}

// ================================================================
// Promotions
// ================================================================

public interface IPromotionService
{
    Task<PagedResponse<PromotionResponse>> GetPromotionsAsync(GetPromotionRequest request);
    Task<PromotionResponse> GetByIdAsync(int promotionId);
    Task<PromotionResponse> GetByCodeAsync(string code);
    Task<PromotionResponse> CreateAsync(int adminId, CreatePromotionRequest request);
    Task<PromotionResponse> UpdateAsync(int promotionId, UpdatePromotionRequest request);
    Task ToggleActiveAsync(int promotionId);
}

// ================================================================
// Services (dịch vụ đi kèm)
// ================================================================

public interface IServiceService
{
    Task<List<ServiceResponse>> GetAllAsync(bool? isAvailable);
    Task<ServiceResponse> GetByIdAsync(int serviceId);
    Task<ServiceResponse> CreateAsync(CreateServiceRequest request);
    Task<ServiceResponse> UpdateAsync(int serviceId, UpdateServiceRequest request);
    Task<string> UploadImageAsync(int serviceId, IFormFile file, IWebHostEnvironment env);
    Task DeleteAsync(int serviceId);
}

// ================================================================
// Inventory
// ================================================================

public interface ISupplierService
{
    Task<SupplierResponse> GetByIdAsync(int supplierId);
    Task<PagedResponse<SupplierResponse>> GetAllAsync(GetSuppliersRequest request);
    Task<SupplierResponse> CreateAsync(CreateSupplierRequest request);
    Task<SupplierResponse> UpdateAsync(int supplierId, UpdateSupplierRequest request);
    Task DeleteAsync(int supplierId);
}

public interface IProductService
{
    Task<ProductResponse> GetByIdAsync(int productId);
    Task<PagedResponse<ProductResponse>> GetAllAsync(GetProductsRequest request);
    Task<ProductResponse> CreateAsync(CreateProductRequest request);
    Task<ProductResponse> UpdateAsync(int productId, UpdateProductRequest request);
    Task DeleteAsync(int productId);
}

public interface IPurchaseOrderService
{
    Task<PurchaseOrderResponse> GetByIdAsync(int purchaseOrderId);
    Task<PagedResponse<PurchaseOrderResponse>> GetAllAsync(GetPurchaseOrdersRequest request);
    Task<PurchaseOrderResponse> CreateAsync(CreatePurchaseOrderRequest request, int createdByUserId);
    Task ConfirmAsync(int purchaseOrderId, int confirmedByUserId);
    Task CancelAsync(int purchaseOrderId);
}

// ================================================================
// Incidents
// ================================================================

public interface IIncidentService
{
    Task<PagedResponse<IncidentResponse>> GetIncidentsAsync(int? fieldId, int? statusId, int page, int pageSize);
    Task<IncidentResponse> GetByIdAsync(int incidentId);
    Task<IncidentResponse> CreateAsync(CreateIncidentRequest request, int reportedBy);
    Task HandleAsync(int incidentId, HandleIncidentRequest request, int handledBy);
}

// ================================================================
// Reviews
// ================================================================

public interface IReviewService
{
    Task<PagedResponse<ReviewResponse>> GetReviewsAsync(GetReviewsRequest request);
    Task<FieldRatingResponse> GetFieldRatingAsync(int fieldId);
    Task<ReviewResponse> CreateAsync(CreateReviewRequest request, int userId);
    Task ToggleVisibilityAsync(int reviewId);
}

// ================================================================
// Notifications
// ================================================================

public interface INotificationService
{
    Task<PagedResponse<NotificationResponse>> GetByUserAsync(int userId, GetNotificationsRequest request);
    Task<int> CountUnreadAsync(int userId);
    Task MarkAsReadAsync(int userId, int notificationId);
    Task MarkAllAsReadAsync(int userId);
    Task SendAsync(int userId, string title, string body, string type, int? refId = null);
}

// ================================================================
// Dashboard
// ================================================================

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetSummaryAsync();
    Task<List<RevenueByMonthResponse>> GetRevenueByMonthAsync(int year);
    Task<List<FieldOccupancyResponse>> GetOccupancyAsync(int? year, int? month);
    Task<List<RevenueByServiceResponse>> GetRevenueByServiceAsync();
}

// ================================================================
// System config
// ================================================================

public interface ISystemConfigService
{
    Task<List<SystemConfigResponse>> GetAllAsync();
    Task<SystemConfigResponse> GetByKeyAsync(string key);
    Task UpdateAsync(string key, UpdateSystemConfigRequest request, int updatedBy);
}

// ================================================================
// Special days
// ================================================================

// FIX: Interface đã có định nghĩa nhưng chưa được đăng ký DI.
// Đã thêm vào AddApplicationServices() và AddRepositories() trong
// ServiceCollectionExtensions.cs.
public interface ISpecialDayService
{
    Task<List<SpecialDayResponse>> GetAllAsync();
    Task<SpecialDayResponse> GetByIdAsync(int specialDayId);
    Task<SpecialDayResponse> CreateAsync(CreateSpecialDayRequest request, int createdBy);
    Task<SpecialDayResponse> UpdateAsync(int specialDayId, UpdateSpecialDayRequest request);
    Task DeleteAsync(int specialDayId);
}

// ================================================================
// Invoice
// ================================================================

public interface IInvoiceService
{
    Task<PagedResponse<InvoiceListItemResponse>> GetInvoicesAsync(DateOnly? date, int page = 1, int pageSize = 20);
    Task<InvoiceDetailResponse> GetInvoiceByPaymentIdAsync(int paymentId);
}

public interface IBackupService
{
    /// <summary>Export toàn bộ dữ liệu → (zipBytes, fileName).</summary>
    Task<(byte[] ZipBytes, string FileName)> ExportAsync();

    /// <summary>Tạo snapshot lưu trên server, trả về thông tin file.</summary>
    Task<BackupSnapshotInfo> CreateSnapshotAsync();

    /// <summary>Liệt kê các snapshot đang có trên server.</summary>
    Task<List<BackupSnapshotInfo>> ListSnapshotsAsync();

    /// <summary>Đọc nội dung một snapshot để download.</summary>
    Task<byte[]> DownloadSnapshotAsync(string fileName);

    /// <summary>Xóa một snapshot trên server.</summary>
    Task DeleteSnapshotAsync(string fileName);

    /// <summary>
    /// Restore dữ liệu từ stream của file .zip backup.
    /// Tự snapshot hiện tại trước khi restore.
    /// Chạy trong transaction — rollback toàn bộ nếu lỗi.
    /// </summary>
    Task<RestoreReportResponse> RestoreAsync(Stream zipStream, int adminUserId);
}
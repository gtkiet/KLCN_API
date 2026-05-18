using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;

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
    //Task<UserDetailResponse> UpdateUserAsync(int userId, UpdateUserRequest request);
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
    Task ApplyVoucherAsync(int bookingId, ApplyVoucherRequest request, int userId);

    /// <summary>
    /// Admin/Staff đặt sân hộ khách tại quầy.
    /// - IsFullPayment = false: flow chờ cọc như cũ.
    /// - IsFullPayment = true: khách trả đủ ngay tại quầy, không cần cổng thanh toán.
    /// </summary>
    Task<BookingResponse> CreateWalkInBookingAsync(CreateWalkInBookingRequest request, int actorUserId);
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
    /// Cho phép 2 trạng thái:
    ///   - PendingDeposit (StatusId=5): customer thanh toán cọc
    ///   - Confirmed (StatusId=2)     : customer tự thanh toán phần còn lại
    /// </summary>
    Task<BookingResponse> GetBookingForPaymentAsync(int bookingId);
}

// ================================================================
// Promotions
// ================================================================

public interface IPromotionService
{
    Task<PagedResponse<PromotionResponse>> GetPromotionsAsync(GetPromotionRequest request);
    Task<PromotionResponse> GetByIdAsync(int promotionId);
    Task<PromotionResponse> GetByCodeAsync(string code);
    //Task<PromotionResponse> CreateAsync(CreatePromotionRequest request);
    Task<PromotionResponse> CreateAsync(int adminId, CreatePromotionRequest request);
    Task<PromotionResponse> UpdateAsync(int promotionId, UpdatePromotionRequest request);
    //Task<PromotionResponse> UpdateAsync(int promotionId, CreatePromotionRequest request);
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

public interface ISpecialDayService
{
    Task<List<SpecialDayResponse>> GetAllAsync();
    Task<SpecialDayResponse> GetByIdAsync(int specialDayId);
    Task<SpecialDayResponse> CreateAsync(CreateSpecialDayRequest request, int createdBy);
    Task<SpecialDayResponse> UpdateAsync(int specialDayId, UpdateSpecialDayRequest request);
    Task DeleteAsync(int specialDayId);
}
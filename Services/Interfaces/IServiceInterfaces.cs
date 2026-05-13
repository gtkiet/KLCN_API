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
}

// ================================================================
// Users
// ================================================================

public interface IUserService
{
    Task<UserDetailResponse> GetByIdAsync(int userId);
    Task<PagedResponse<UserResponse>> GetUsersAsync(GetUsersRequest request);
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
    Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request, int userId);
    Task<BookingResponse> GetByIdAsync(int bookingId, int requesterId, bool isAdminOrStaff);
    Task<PagedResponse<BookingSummaryResponse>> GetBookingsAsync(GetBookingsRequest request);
    Task<PagedResponse<BookingSummaryResponse>> GetMyBookingsAsync(int userId, int page, int pageSize);
    Task CancelAsync(int bookingId, CancelBookingRequest request, int userId, bool isAdminOverride);
    Task RescheduleAsync(int bookingId, RescheduleRequest request, int userId);
    Task ApplyVoucherAsync(int bookingId, ApplyVoucherRequest request, int userId);
}

// ================================================================
// Payments
// ================================================================

public interface IPaymentService
{
    Task RecordDepositAsync(int bookingId, RecordDepositRequest request, int userId);
    Task RecordFullPaymentAsync(int bookingId, ConfirmPaymentRequest request, int userId);
    Task<List<PaymentResponse>> GetPaymentsByBookingAsync(int bookingId);
    Task<DepositResponse?> GetDepositByBookingAsync(int bookingId);
    Task RecordOnlinePaymentAsync(
        int bookingId, decimal amount, int methodId, string transactionCode);
}

// ================================================================
// Promotions
// ================================================================

public interface IPromotionService
{
    Task<PagedResponse<PromotionResponse>> GetPromotionsAsync(bool? isActive, int page, int pageSize);
    Task<PromotionResponse> GetByIdAsync(int promotionId);
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
    Task DeleteAsync(int serviceId);
}

// ================================================================
// Inventory
// ================================================================

public interface ISupplierService
{
    Task<PagedResponse<SupplierResponse>> GetSuppliersAsync(string? search, int page, int pageSize);
    Task<SupplierResponse> GetByIdAsync(int supplierId);
    Task<SupplierResponse> CreateAsync(CreateSupplierRequest request);
    Task<SupplierResponse> UpdateAsync(int supplierId, UpdateSupplierRequest request);
    Task DeleteAsync(int supplierId);
}

public interface IProductService
{
    Task<PagedResponse<ProductResponse>> GetProductsAsync(string? search, int page, int pageSize);
    Task<List<ProductResponse>> GetLowStockAsync();
    Task<ProductResponse> GetByIdAsync(int productId);
    Task<ProductResponse> CreateAsync(CreateProductRequest request);
    // Nhận UpdateProductRequest thay vì raw entity khi implement
    Task UpdateAsync(int productId, CreateProductRequest request);
}

public interface IPurchaseOrderService
{
    Task<PagedResponse<PurchaseOrderResponse>> GetPurchaseOrdersAsync(int? statusId, int page, int pageSize);
    Task<PurchaseOrderResponse> GetByIdAsync(int purchaseOrderId);
    Task<PurchaseOrderResponse> CreateAsync(CreatePurchaseOrderRequest request, int createdBy);
    Task ConfirmAsync(int purchaseOrderId, int confirmedBy);
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
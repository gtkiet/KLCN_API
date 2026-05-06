//// ============================================================
//// Services/Interfaces/IServices.cs
//// Interface cho tất cả Service — placeholder
//// ============================================================
//// Quy ước đặt tên:
////   IAuthService, IUserService, IFieldService, IBookingService, ...
////   Mỗi method trả về Task<ApiResponse<T>> hoặc Task<T>
//// ============================================================

//using KLCN_API.Models.DTOs.Request;
//using KLCN_API.Models.DTOs.Response;

//namespace KLCN_API.Services.Interfaces;

//public interface IAuthService
//{
//    // TODO: Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
//    // TODO: Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
//    // TODO: Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request)
//    // TODO: Task<ApiResponse> LogoutAsync(int userId, string refreshToken)
//    // TODO: Task<ApiResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request)
//}

//public interface IUserService
//{
//    // TODO: Task<ApiResponse<PagedResponse<UserSummaryResponse>>> GetUsersAsync(GetUsersRequest request)
//    // TODO: Task<ApiResponse<UserSummaryResponse>> GetUserByIdAsync(int userId)
//    // TODO: Task<ApiResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request)
//    // TODO: Task<ApiResponse> UpdateUserStatusAsync(int userId, UpdateUserStatusRequest request)
//    // TODO: Task<ApiResponse> DeleteUserAsync(int userId)
//}

//public interface IFieldService
//{
//    // TODO: Task<ApiResponse<List<FieldResponse>>> GetFieldsAsync(int? typeId, int? statusId)
//    // TODO: Task<ApiResponse<FieldResponse>> GetFieldByIdAsync(int fieldId)
//    // TODO: Task<ApiResponse<FieldResponse>> CreateFieldAsync(CreateFieldRequest request)
//    // TODO: Task<ApiResponse<FieldResponse>> UpdateFieldAsync(int fieldId, UpdateFieldRequest request)
//    // TODO: Task<ApiResponse> DeleteFieldAsync(int fieldId)
//    // TODO: Task<ApiResponse<FieldScheduleResponse>> GetScheduleAsync(GetFieldScheduleRequest request)
//    // TODO: Task<ApiResponse> GenerateSlotsAsync(DateOnly startDate, DateOnly endDate)
//    // TODO: Task<ApiResponse> SetMaintenanceAsync(int fieldId, CreateMaintenanceRequest request)
//}

//public interface IBookingService
//{
//    // TODO: Task<ApiResponse> HoldSlotsAsync(HoldSlotsRequest request, int userId)
//    // TODO: Task<ApiResponse<BookingResponse>> CreateBookingAsync(CreateBookingRequest request, int userId)
//    // TODO: Task<ApiResponse<BookingResponse>> GetBookingByIdAsync(int bookingId, int requesterId)
//    // TODO: Task<ApiResponse<PagedResponse<BookingSummaryResponse>>> GetBookingsAsync(GetBookingsRequest request)
//    // TODO: Task<ApiResponse> CancelBookingAsync(int bookingId, CancelBookingRequest request, int userId, bool isAdmin)
//    // TODO: Task<ApiResponse> RescheduleAsync(int bookingId, RescheduleRequest request, int userId)
//    // TODO: Task<ApiResponse> ApplyVoucherAsync(int bookingId, ApplyVoucherRequest request)
//    // TODO: Task<ApiResponse<List<BookingSummaryResponse>>> GetMyBookingsAsync(int userId)
//}

//public interface IPaymentService
//{
//    // TODO: Task<ApiResponse> RecordDepositAsync(int bookingId, RecordDepositRequest request, int userId)
//    // TODO: Task<ApiResponse> RecordFullPaymentAsync(int bookingId, ConfirmPaymentRequest request)
//    // TODO: Task<ApiResponse<List<PaymentResponse>>> GetPaymentsByBookingAsync(int bookingId)
//    // TODO: Task<ApiResponse<DepositResponse>> GetDepositByBookingAsync(int bookingId)
//}

//public interface IPromotionService
//{
//    // TODO: Task<ApiResponse<List<PromotionResponse>>> GetPromotionsAsync(bool? isActive)
//    // TODO: Task<ApiResponse<PromotionResponse>> GetPromotionByCodeAsync(string code)
//    // TODO: Task<ApiResponse<PromotionResponse>> CreatePromotionAsync(CreatePromotionRequest request, int createdBy)
//    // TODO: Task<ApiResponse<PromotionResponse>> UpdatePromotionAsync(int id, CreatePromotionRequest request)
//    // TODO: Task<ApiResponse> TogglePromotionAsync(int id)
//}

//public interface IServiceService
//{
//    // TODO: Task<ApiResponse<List<ServiceResponse>>> GetServicesAsync(bool? isAvailable)
//    // TODO: Task<ApiResponse<ServiceResponse>> CreateServiceAsync(CreateServiceRequest request)
//    // TODO: Task<ApiResponse<ServiceResponse>> UpdateServiceAsync(int id, UpdateServiceRequest request)
//    // TODO: Task<ApiResponse> DeleteServiceAsync(int id)
//}

//public interface IInventoryService
//{
//    // TODO: Task<ApiResponse<List<SupplierResponse>>> GetSuppliersAsync()
//    // TODO: Task<ApiResponse<SupplierResponse>> CreateSupplierAsync(CreateSupplierRequest request)
//    // TODO: Task<ApiResponse<List<ProductResponse>>> GetProductsAsync()
//    // TODO: Task<ApiResponse<List<ProductResponse>>> GetLowStockProductsAsync()
//    // TODO: Task<ApiResponse<PurchaseOrderResponse>> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request, int userId)
//    // TODO: Task<ApiResponse> ConfirmPurchaseOrderAsync(int orderId, int userId)
//}

//public interface IIncidentService
//{
//    // TODO: Task<ApiResponse<List<IncidentResponse>>> GetIncidentsAsync(int? fieldId, int? statusId)
//    // TODO: Task<ApiResponse<IncidentResponse>> CreateIncidentAsync(CreateIncidentRequest request, int userId)
//    // TODO: Task<ApiResponse> HandleIncidentAsync(int id, HandleIncidentRequest request, int handlerId)
//}

//public interface IReviewService
//{
//    // TODO: Task<ApiResponse<ReviewResponse>> CreateReviewAsync(CreateReviewRequest request, int userId)
//    // TODO: Task<ApiResponse<FieldRatingResponse>> GetFieldRatingAsync(int fieldId)
//    // TODO: Task<ApiResponse<PagedResponse<ReviewResponse>>> GetReviewsByFieldAsync(int fieldId, int page, int pageSize)
//    // TODO: Task<ApiResponse> ToggleReviewVisibilityAsync(int reviewId)
//}

//public interface INotificationService
//{
//    // TODO: Task<ApiResponse<PagedResponse<NotificationResponse>>> GetNotificationsAsync(int userId, GetNotificationsRequest request)
//    // TODO: Task<ApiResponse> MarkAsReadAsync(int userId, int notificationId)
//    // TODO: Task<ApiResponse> MarkAllAsReadAsync(int userId)
//    // TODO: Task SendNotificationAsync(int userId, string title, string body, string type, int? refId)
//}

//public interface IDashboardService
//{
//    // TODO: Task<ApiResponse<DashboardSummaryResponse>> GetSummaryAsync()
//    // TODO: Task<ApiResponse<List<RevenueByMonthResponse>>> GetRevenueByMonthAsync(int year)
//    // TODO: Task<ApiResponse<List<FieldOccupancyResponse>>> GetOccupancyAsync(int year, int month)
//    // TODO: Task<ApiResponse<List<ServiceResponse>>> GetTopServicesAsync()
//}

//public interface ISystemConfigService
//{
//    // TODO: Task<ApiResponse<List<SystemConfigResponse>>> GetAllConfigsAsync()
//    // TODO: Task<ApiResponse<SystemConfigResponse>> GetConfigAsync(string key)
//    // TODO: Task<ApiResponse> UpdateConfigAsync(UpdateSystemConfigRequest request, int userId)
//}

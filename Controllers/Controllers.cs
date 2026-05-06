//// ============================================================
//// Controllers/Controllers.cs
//// Tất cả Controller — mỗi cái là một file riêng trong thực tế
//// Ở đây tập hợp placeholder để dễ overview
//// ============================================================
//// Khi làm thật: tách mỗi class ra file riêng (AuthController.cs, ...)
//// ============================================================

//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using KLCN_API.Models.DTOs.Request;
//using KLCN_API.Models.DTOs.Response;
//using KLCN_API.Services.Interfaces;

//namespace KLCN_API.Controllers;

//[ApiController]
//[Route("api/auth")]
//public class AuthController : ControllerBase
//{
//    // TODO: Inject IAuthService
//    // POST api/auth/register          → RegisterAsync
//    // POST api/auth/login             → LoginAsync
//    // POST api/auth/refresh-token     → RefreshTokenAsync
//    // POST api/auth/logout            → LogoutAsync
//    // POST api/auth/change-password   → ChangePasswordAsync [Authorize]
//}

//[ApiController]
//[Route("api/users")]
//[Authorize]
//public class UsersController : ControllerBase
//{
//    // TODO: Inject IUserService
//    // GET    api/users                → GetUsersAsync [Admin]
//    // GET    api/users/{id}           → GetUserByIdAsync [Admin | self]
//    // PUT    api/users/{id}/profile   → UpdateProfileAsync [self]
//    // PUT    api/users/{id}/status    → UpdateUserStatusAsync [Admin]
//    // DELETE api/users/{id}          → DeleteUserAsync [Admin]
//    // GET    api/users/me             → GetMyProfile [Authorize]
//}

//[ApiController]
//[Route("api/fields")]
//public class FieldsController : ControllerBase
//{
//    // TODO: Inject IFieldService, IReviewService
//    // GET    api/fields               → GetFieldsAsync [Public]
//    // GET    api/fields/{id}          → GetFieldByIdAsync [Public]
//    // POST   api/fields               → CreateFieldAsync [Admin]
//    // PUT    api/fields/{id}          → UpdateFieldAsync [Admin]
//    // DELETE api/fields/{id}         → DeleteFieldAsync [Admin]
//    // GET    api/fields/schedule      → GetScheduleAsync [Public]
//    // POST   api/fields/generate-slots → GenerateSlotsAsync [Admin]
//    // POST   api/fields/{id}/maintenance → SetMaintenanceAsync [Admin]
//    // GET    api/fields/{id}/reviews  → GetFieldRatingAsync [Public]
//}

//[ApiController]
//[Route("api/bookings")]
//[Authorize]
//public class BookingsController : ControllerBase
//{
//    // TODO: Inject IBookingService, IPaymentService
//    // POST api/bookings/hold          → HoldSlotsAsync [Customer]
//    // POST api/bookings               → CreateBookingAsync [Customer]
//    // GET  api/bookings               → GetBookingsAsync [Admin|Staff]
//    // GET  api/bookings/my            → GetMyBookingsAsync [Customer]
//    // GET  api/bookings/{id}          → GetBookingByIdAsync
//    // POST api/bookings/{id}/cancel   → CancelBookingAsync
//    // POST api/bookings/{id}/reschedule → RescheduleAsync
//    // POST api/bookings/{id}/apply-voucher → ApplyVoucherAsync
//    // POST api/bookings/{id}/deposit  → RecordDepositAsync [Customer|Staff]
//    // POST api/bookings/{id}/payment  → RecordFullPaymentAsync [Staff|Admin]
//}

//[ApiController]
//[Route("api/promotions")]
//public class PromotionsController : ControllerBase
//{
//    // TODO: Inject IPromotionService
//    // GET    api/promotions           → GetPromotionsAsync [Admin|Staff]
//    // GET    api/promotions/{code}    → GetPromotionByCodeAsync [Authorize]
//    // POST   api/promotions           → CreatePromotionAsync [Admin]
//    // PUT    api/promotions/{id}      → UpdatePromotionAsync [Admin]
//    // PATCH  api/promotions/{id}/toggle → TogglePromotionAsync [Admin]
//}

//[ApiController]
//[Route("api/services")]
//public class ServicesController : ControllerBase
//{
//    // TODO: Inject IServiceService
//    // GET    api/services             → GetServicesAsync [Public]
//    // POST   api/services             → CreateServiceAsync [Admin]
//    // PUT    api/services/{id}        → UpdateServiceAsync [Admin]
//    // DELETE api/services/{id}       → DeleteServiceAsync [Admin]
//}

//[ApiController]
//[Route("api/suppliers")]
//[Authorize(Roles = "Admin,Staff")]
//public class SuppliersController : ControllerBase
//{
//    // TODO: Inject IInventoryService
//    // GET    api/suppliers            → GetSuppliersAsync
//    // POST   api/suppliers            → CreateSupplierAsync [Admin]
//    // PUT    api/suppliers/{id}       → UpdateSupplierAsync [Admin]
//    // DELETE api/suppliers/{id}      → DeleteSupplierAsync [Admin]
//}

//[ApiController]
//[Route("api/products")]
//[Authorize(Roles = "Admin,Staff")]
//public class ProductsController : ControllerBase
//{
//    // TODO: Inject IInventoryService
//    // GET  api/products               → GetProductsAsync
//    // GET  api/products/low-stock     → GetLowStockProductsAsync
//    // POST api/products               → CreateProductAsync [Admin]
//    // PUT  api/products/{id}          → UpdateProductAsync [Admin]
//}

//[ApiController]
//[Route("api/purchase-orders")]
//[Authorize(Roles = "Admin,Staff")]
//public class PurchaseOrdersController : ControllerBase
//{
//    // TODO: Inject IInventoryService
//    // GET    api/purchase-orders          → GetPurchaseOrdersAsync
//    // GET    api/purchase-orders/{id}     → GetPurchaseOrderByIdAsync
//    // POST   api/purchase-orders          → CreatePurchaseOrderAsync
//    // POST   api/purchase-orders/{id}/confirm → ConfirmPurchaseOrderAsync [Admin]
//    // DELETE api/purchase-orders/{id}    → CancelPurchaseOrderAsync [Admin]
//}

//[ApiController]
//[Route("api/incidents")]
//[Authorize]
//public class IncidentsController : ControllerBase
//{
//    // TODO: Inject IIncidentService
//    // GET  api/incidents              → GetIncidentsAsync [Admin|Staff]
//    // GET  api/incidents/{id}         → GetIncidentByIdAsync
//    // POST api/incidents              → CreateIncidentAsync [Customer|Staff]
//    // PUT  api/incidents/{id}/handle  → HandleIncidentAsync [Admin|Staff]
//}

//[ApiController]
//[Route("api/reviews")]
//public class ReviewsController : ControllerBase
//{
//    // TODO: Inject IReviewService
//    // POST  api/reviews               → CreateReviewAsync [Customer, Authorize]
//    // GET   api/reviews/{id}          → GetReviewByIdAsync
//    // PATCH api/reviews/{id}/toggle   → ToggleVisibilityAsync [Admin]
//}

//[ApiController]
//[Route("api/notifications")]
//[Authorize]
//public class NotificationsController : ControllerBase
//{
//    // TODO: Inject INotificationService
//    // GET   api/notifications         → GetNotificationsAsync
//    // PATCH api/notifications/{id}/read → MarkAsReadAsync
//    // POST  api/notifications/read-all  → MarkAllAsReadAsync
//}

//[ApiController]
//[Route("api/dashboard")]
//[Authorize(Roles = "Admin,Staff")]
//public class DashboardController : ControllerBase
//{
//    // TODO: Inject IDashboardService
//    // GET api/dashboard/summary       → GetSummaryAsync
//    // GET api/dashboard/revenue       → GetRevenueByMonthAsync?year=2026
//    // GET api/dashboard/occupancy     → GetOccupancyAsync?year=2026&month=5
//    // GET api/dashboard/top-services  → GetTopServicesAsync
//}

//[ApiController]
//[Route("api/system-config")]
//[Authorize(Roles = "Admin")]
//public class SystemConfigController : ControllerBase
//{
//    // TODO: Inject ISystemConfigService
//    // GET api/system-config           → GetAllConfigsAsync
//    // PUT api/system-config           → UpdateConfigAsync
//}

//[ApiController]
//[Route("api/special-days")]
//[Authorize(Roles = "Admin")]
//public class SpecialDaysController : ControllerBase
//{
//    // TODO: Service chưa có — cần tạo ISpecialDayService
//    // GET    api/special-days         → GetSpecialDaysAsync
//    // POST   api/special-days         → CreateSpecialDayAsync
//    // DELETE api/special-days/{id}   → DeleteSpecialDayAsync
//}

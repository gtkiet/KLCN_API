//// ============================================================
//// Models/DTOs/Request/Requests.cs
//// Tất cả Request DTO — placeholder, điền chi tiết khi làm từng API
//// ============================================================

//namespace KLCN_API.Models.DTOs.Request;

//// ── Auth ───────────────────────────────────────────────────
///// <summary>POST /api/auth/register</summary>
//public class RegisterRequest
//{
//    // TODO: FullName, Email, Phone, Password, ConfirmPassword
//}

///// <summary>POST /api/auth/login</summary>
//public class LoginRequest
//{
//    // TODO: Email/Phone, Password
//}

///// <summary>POST /api/auth/refresh-token</summary>
//public class RefreshTokenRequest
//{
//    // TODO: RefreshToken
//}

///// <summary>POST /api/auth/change-password</summary>
//public class ChangePasswordRequest
//{
//    // TODO: OldPassword, NewPassword, ConfirmNewPassword
//}

//// ── Users ──────────────────────────────────────────────────
///// <summary>PUT /api/users/{id}/profile</summary>
//public class UpdateProfileRequest
//{
//    // TODO: FullName, Phone, DateOfBirth, Address, AvatarUrl
//}

///// <summary>PUT /api/users/{id}/status (Admin)</summary>
//public class UpdateUserStatusRequest
//{
//    // TODO: StatusId (1=Active, 2=Locked)
//}

///// <summary>GET /api/users — filter/paging</summary>
//public class GetUsersRequest : PagedRequest
//{
//    // TODO: Keyword (search name/email/phone), RoleId, StatusId
//}

//// ── Fields ─────────────────────────────────────────────────
///// <summary>POST /api/fields (Admin)</summary>
//public class CreateFieldRequest
//{
//    // TODO: Name, Description, BasePrice, PeakPrice, TypeId, ImageUrl
//}

///// <summary>PUT /api/fields/{id} (Admin)</summary>
//public class UpdateFieldRequest
//{
//    // TODO: Name, Description, BasePrice, PeakPrice, TypeId, ImageUrl, StatusId, Reason (cho price history)
//}

///// <summary>GET /api/fields/schedule?date=...&fieldId=... (Public)</summary>
//public class GetFieldScheduleRequest
//{
//    // TODO: FieldId (optional), Date (required), TypeId (optional)
//}

//// ── Slots / Booking Flow ───────────────────────────────────
///// <summary>POST /api/bookings/hold</summary>
//public class HoldSlotsRequest
//{
//    // TODO: FieldSlotIds (List<int>)
//}

///// <summary>POST /api/bookings</summary>
//public class CreateBookingRequest
//{
//    // TODO: FieldSlotIds, ServiceItems (ServiceId + Qty), PromotionCode, Note, IsFullPayment
//}

///// <summary>POST /api/bookings/{id}/confirm-payment</summary>
//public class ConfirmPaymentRequest
//{
//    // TODO: MethodId, TransactionCode, Amount, GatewayResponse
//}

///// <summary>POST /api/bookings/{id}/deposit</summary>
//public class RecordDepositRequest
//{
//    // TODO: Amount, MethodId, TransactionCode
//}

///// <summary>POST /api/bookings/{id}/cancel</summary>
//public class CancelBookingRequest
//{
//    // TODO: Reason
//}

///// <summary>POST /api/bookings/{id}/reschedule</summary>
//public class RescheduleRequest
//{
//    // TODO: BookingDetailId, NewFieldSlotId
//}

///// <summary>GET /api/bookings — filter/paging (Admin/Staff)</summary>
//public class GetBookingsRequest : PagedRequest
//{
//    // TODO: UserId, StatusId, DateFrom, DateTo, FieldId
//}

//// ── Promotions ─────────────────────────────────────────────
///// <summary>POST /api/promotions (Admin)</summary>
//public class CreatePromotionRequest
//{
//    // TODO: Code, Name, Description, TypeId, DiscountValue, MaxDiscount,
//    //       MinOrderAmount, UsageLimit, StartDate, EndDate
//}

///// <summary>POST /api/bookings/{id}/apply-voucher</summary>
//public class ApplyVoucherRequest
//{
//    // TODO: Code
//}

//// ── Services ───────────────────────────────────────────────
///// <summary>POST /api/services (Admin)</summary>
//public class CreateServiceRequest
//{
//    // TODO: Name, Description, Price, ImageUrl
//}

///// <summary>PUT /api/services/{id} (Admin)</summary>
//public class UpdateServiceRequest
//{
//    // TODO: Name, Description, Price, ImageUrl, IsAvailable
//}

//// ── Inventory ──────────────────────────────────────────────
///// <summary>POST /api/suppliers (Admin)</summary>
//public class CreateSupplierRequest
//{
//    // TODO: Name, ContactName, Phone, Email, Address
//}

///// <summary>POST /api/purchase-orders (Admin/Staff)</summary>
//public class CreatePurchaseOrderRequest
//{
//    // TODO: SupplierId, Note, Items (ProductId, Quantity, UnitPrice)
//}

//// ── Incidents ──────────────────────────────────────────────
///// <summary>POST /api/incidents</summary>
//public class CreateIncidentRequest
//{
//    // TODO: FieldId, Title, Description, ImageUrl
//}

///// <summary>PUT /api/incidents/{id}/handle (Admin/Staff)</summary>
//public class HandleIncidentRequest
//{
//    // TODO: StatusId, HandledNote
//}

//// ── Reviews ────────────────────────────────────────────────
///// <summary>POST /api/reviews</summary>
//public class CreateReviewRequest
//{
//    // TODO: BookingId, Rating (1-5), Comment, ImageUrl
//}

//// ── Special Days ───────────────────────────────────────────
///// <summary>POST /api/special-days (Admin)</summary>
//public class CreateSpecialDayRequest
//{
//    // TODO: SpecialDate, Name, PriceMultiplier, IsFullDayPeak, Note
//}

//// ── System Config ──────────────────────────────────────────
///// <summary>PUT /api/system-config (Admin)</summary>
//public class UpdateSystemConfigRequest
//{
//    // TODO: ConfigKey, ConfigValue
//}

//// ── Maintenance ────────────────────────────────────────────
///// <summary>POST /api/fields/{id}/maintenance (Admin)</summary>
//public class CreateMaintenanceRequest
//{
//    // TODO: Reason, StartDate, EndDate
//}

//// ── Notifications ──────────────────────────────────────────
///// <summary>GET /api/notifications — filter unread</summary>
//public class GetNotificationsRequest : PagedRequest
//{
//    // TODO: IsRead (optional)
//}

//// ── Base Paging ────────────────────────────────────────────
//public class PagedRequest
//{
//    public int Page { get; set; } = 1;
//    public int PageSize { get; set; } = 20;
//    // TODO: SortBy, SortDesc
//}

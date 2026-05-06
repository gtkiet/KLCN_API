// ============================================================
// Models/DTOs/Response/Responses.cs
// Tất cả Response DTO — placeholder, điền chi tiết khi làm từng API
// ============================================================

namespace KLCN_API.Models.DTOs.Response;

// ── Auth ───────────────────────────────────────────────────
public class AuthResponse
{
    // TODO: AccessToken, RefreshToken, ExpiresAt, User (UserSummaryResponse)
}

public class UserSummaryResponse
{
    // TODO: UserId, FullName, Email, Phone, Role, AvatarUrl
}

// ── Field ──────────────────────────────────────────────────
public class FieldResponse
{
    // TODO: FieldId, Name, Description, BasePrice, PeakPrice, ImageUrl,
    //       FieldType, Status, AvgRating, TotalReviews
}

public class FieldScheduleResponse
{
    // TODO: FieldId, FieldName, SlotDate, Slots (List<SlotResponse>)
}

public class SlotResponse
{
    // TODO: FieldSlotId, StartTime, EndTime, Price, Status, IsPeakHour,
    //       HoldRemainingSeconds
}

// ── Booking ────────────────────────────────────────────────
public class BookingResponse
{
    // TODO: BookingId, Customer (UserSummaryResponse), Status, SubTotal,
    //       DiscountAmount, TaxAmount, TotalAmount, DepositAmount,
    //       PromotionCode, Note, CancelReason, RescheduleCount,
    //       Details (List<BookingDetailResponse>), Services, CreatedAt
}

public class BookingDetailResponse
{
    // TODO: BookingDetailId, FieldId, FieldName, SlotDate, StartTime, EndTime, Price
}

public class BookingSummaryResponse
{
    // TODO: Compact version dùng cho list/history — ít field hơn BookingResponse
}

// ── Payment ────────────────────────────────────────────────
public class PaymentResponse
{
    // TODO: PaymentId, BookingId, Amount, Status, Method, TransactionCode, PaidAt
}

public class DepositResponse
{
    // TODO: DepositId, BookingId, RequiredAmount, PaidAmount, Status,
    //       DeadlineAt, MinutesLeft, PaidAt
}

// ── Promotion ──────────────────────────────────────────────
public class PromotionResponse
{
    // TODO: PromotionId, Code, Name, Type, DiscountValue, MaxDiscount,
    //       MinOrderAmount, UsageLimit, UsageCount, StartDate, EndDate, IsActive
}

// ── Service ────────────────────────────────────────────────
public class ServiceResponse
{
    // TODO: ServiceId, Name, Description, Price, ImageUrl, IsAvailable
}

// ── Inventory ──────────────────────────────────────────────
public class SupplierResponse
{
    // TODO: SupplierId, Name, ContactName, Phone, Email, Address
}

public class ProductResponse
{
    // TODO: ProductId, Name, Unit, StockQty, MinQty, IsLowStock
}

public class PurchaseOrderResponse
{
    // TODO: PurchaseOrderId, Supplier, Status, TotalAmount, Note,
    //       Items (List<PurchaseOrderDetailResponse>), CreatedAt, ConfirmedAt
}

// ── Incident ───────────────────────────────────────────────
public class IncidentResponse
{
    // TODO: IncidentId, Field, ReportedBy, Title, Description, ImageUrl,
    //       Status, HandledBy, HandledAt, HandledNote, CreatedAt
}

// ── Review ─────────────────────────────────────────────────
public class ReviewResponse
{
    // TODO: ReviewId, BookingId, User, Rating, Comment, ImageUrl, CreatedAt
}

public class FieldRatingResponse
{
    // TODO: FieldId, FieldName, AvgRating, TotalReviews,
    //       Stars5..Stars1, Reviews (List<ReviewResponse>)
}

// ── Notification ───────────────────────────────────────────
public class NotificationResponse
{
    // TODO: NotificationId, Title, Body, Type, RefId, IsRead, CreatedAt
}

// ── Dashboard / Reports ────────────────────────────────────
public class DashboardSummaryResponse
{
    // TODO: PendingBookings, PendingDepositBookings, TodayConfirmed,
    //       ActiveFields, MaintenanceFields, NewIncidents,
    //       TodayRevenue, ActiveCustomers, LowStockCount, UrgentDepositCount
}

public class RevenueByMonthResponse
{
    // TODO: Year, Month, TotalBookings, TotalRevenue, AvgBookingValue
}

public class FieldOccupancyResponse
{
    // TODO: FieldId, FieldName, FieldType, Year, Month,
    //       TotalSlots, BookedSlots, OccupancyRate
}

// ── System Config ──────────────────────────────────────────
public class SystemConfigResponse
{
    // TODO: ConfigKey, ConfigValue, DataType, Description, UpdatedAt
}

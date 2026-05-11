namespace KLCN_API.Models.Enums;

// ── Lookup enums ───────────────────────────────────────────
// Giá trị int phải khớp với thứ tự INSERT trong 02_seed.sql.
// Không thay đổi giá trị đã có — thêm mới thì gán số tường minh.

public enum RoleEnum
{
    Admin = 1,
    Staff = 2,
    Customer = 3
}

public enum UserStatusEnum
{
    Active = 1,  // Hoạt động
    Locked = 2   // Bị khóa
}

public enum FieldTypeEnum
{
    Field5 = 1,  // Sân 5
    Field7 = 2   // Sân 7
}

public enum FieldStatusEnum
{
    Active = 1,  // Hoạt động
    Maintenance = 2   // Bảo trì
}

public enum FieldSlotStatusEnum
{
    Available = 1,  // Trống
    Holding = 2,  // Đang giữ
    Booked = 3   // Đã đặt
}

public enum BookingStatusEnum
{
    PendingPayment = 1,  // Chờ thanh toán
    Confirmed = 2,  // Đã xác nhận
    Cancelled = 3,  // Đã hủy
    Completed = 4,  // Đã hoàn thành
    PendingDeposit = 5   // Chờ đặt cọc
}

public enum PaymentStatusEnum
{
    Unpaid = 1,  // Chưa thanh toán
    Paid = 2,  // Đã thanh toán
    Failed = 3,  // Thất bại
    Refunded = 4   // Đã hoàn tiền
}

public enum DepositStatusEnum
{
    Pending = 1,  // Chờ nộp
    Paid = 2,  // Đã nộp
    Refunded = 3,  // Đã hoàn
    Forfeited = 4   // Đã tịch thu
}

public enum IncidentStatusEnum
{
    New = 1,  // Mới
    Processing = 2,  // Đang xử lý
    Resolved = 3   // Đã xử lý
}

public enum PurchaseOrderStatusEnum
{
    Pending = 1,  // Chờ xác nhận
    Confirmed = 2,  // Đã nhập
    Cancelled = 3   // Đã hủy
}

public enum PromotionTypeEnum
{
    Percentage = 1,  // Phần trăm
    FixedAmount = 2   // Số tiền cố định
}

public enum PaymentMethodEnum
{
    Cash = 1,  // Tiền mặt
    BankTransfer = 2,  // Chuyển khoản
    VNPay = 3,
    MoMo = 4
}

// ── Notification type ──────────────────────────────────────
// Không lưu dưới dạng int trong DB — cột Type là NVARCHAR(50).
// Dùng NotificationTypeExtensions.ToDbString() khi ghi vào DB,
// và NotificationTypeExtensions.FromDbString() khi đọc ra.

public enum NotificationType
{
    BookingConfirm = 1,
    BookingCancel = 2,
    Payment = 3,
    Deposit = 4,
    Incident = 5,
    Review = 6,
    System = 7
}

public static class NotificationTypeExtensions
{
    // Chuỗi này khớp với giá trị cột Type trong bảng Notifications
    private static readonly Dictionary<NotificationType, string> DbStrings = new()
    {
        [NotificationType.BookingConfirm] = "BOOKING_CONFIRM",
        [NotificationType.BookingCancel] = "BOOKING_CANCEL",
        [NotificationType.Payment] = "PAYMENT",
        [NotificationType.Deposit] = "DEPOSIT",
        [NotificationType.Incident] = "INCIDENT",
        [NotificationType.Review] = "REVIEW",
        [NotificationType.System] = "SYSTEM"
    };

    private static readonly Dictionary<string, NotificationType> FromStrings =
        DbStrings.ToDictionary(kv => kv.Value, kv => kv.Key);

    /// <summary>Chuyển enum → chuỗi để lưu vào cột Notifications.Type.</summary>
    public static string ToDbString(this NotificationType type) =>
        DbStrings.TryGetValue(type, out var s) ? s : type.ToString().ToUpperInvariant();

    /// <summary>Chuyển chuỗi từ DB → enum. Trả null nếu không nhận ra.</summary>
    public static NotificationType? FromDbString(string? value) =>
        value is not null && FromStrings.TryGetValue(value, out var t) ? t : null;
}
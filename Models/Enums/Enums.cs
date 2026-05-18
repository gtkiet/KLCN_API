namespace KLCN_API.Models.Enums;

public enum RoleEnum
{
    Admin = 1,
    Staff = 2,
    Customer = 3
}

public enum UserStatusEnum
{
    Active = 1,
    Locked = 2
}

public enum FieldTypeEnum
{
    Field5 = 1,
    Field7 = 2
}

public enum FieldStatusEnum
{
    Active = 1,
    Maintenance = 2
}

public enum FieldSlotStatusEnum
{
    Available = 1,
    Holding = 2,
    Booked = 3
}

public enum BookingStatusEnum
{
    PendingPayment = 1,  // Slot đang giữ, chưa tạo booking chính thức
    Confirmed = 2,  // Đã nộp cọc thành công
    Cancelled = 3,
    Completed = 4,
    PendingDeposit = 5   // Đã tạo booking, đang chờ khách nộp cọc
}

public enum PaymentStatusEnum
{
    Unpaid = 1,
    Paid = 2,
    Failed = 3,
    Refunded = 4
}

public enum DepositStatusEnum
{
    Pending = 1,
    Paid = 2,
    Refunded = 3,
    Forfeited = 4
}

public enum IncidentStatusEnum
{
    New = 1,
    Processing = 2,
    Resolved = 3
}

public enum PurchaseOrderStatusEnum
{
    Pending = 1,
    Confirmed = 2,
    Cancelled = 3
}

public enum PromotionTypeEnum
{
    Percentage = 1,
    FixedAmount = 2
}

/// <summary>
/// Phương thức thanh toán — chỉ có 2 loại:
///   Direct (1): Trực tiếp tại quầy — Staff dùng để ghi nhận phần còn lại sau cọc.
///   MoMo   (2): Bắt buộc dùng để đặt cọc online trước khi booking được xác nhận.
/// </summary>
public enum PaymentMethodEnum
{
    Direct = 1,
    MoMo = 2
}

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

    public static string ToDbString(this NotificationType type) =>
        DbStrings.TryGetValue(type, out var s) ? s : type.ToString().ToUpperInvariant();

    public static NotificationType? FromDbString(string? value) =>
        value is not null && FromStrings.TryGetValue(value, out var t) ? t : null;
}
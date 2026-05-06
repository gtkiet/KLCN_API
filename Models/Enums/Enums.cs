// ============================================================
// Models/Enums/Enums.cs
// Các enum ánh xạ tới lookup tables trong DB
// ============================================================

namespace KLCN_API.Models.Enums;

public enum RoleEnum
{
    Admin = 1,
    Staff = 2,
    Customer = 3
}

public enum UserStatusEnum
{
    Active = 1,     // Hoạt động
    Locked = 2      // Bị khóa
}

public enum FieldStatusEnum
{
    Active = 1,      // Hoạt động
    Maintenance = 2  // Bảo trì
}

public enum FieldSlotStatusEnum
{
    Available = 1,  // Trống
    Holding = 2,    // Đang giữ
    Booked = 3      // Đã đặt
}

public enum BookingStatusEnum
{
    PendingPayment = 1,   // Chờ thanh toán
    Confirmed = 2,        // Đã xác nhận
    Cancelled = 3,        // Đã hủy
    Completed = 4,        // Đã hoàn thành
    PendingDeposit = 5    // Chờ đặt cọc
}

public enum PaymentStatusEnum
{
    Unpaid = 1,     // Chưa thanh toán
    Paid = 2,       // Đã thanh toán
    Failed = 3,     // Thất bại
    Refunded = 4    // Đã hoàn tiền
}

public enum DepositStatusEnum
{
    Pending = 1,    // Chờ nộp
    Paid = 2,       // Đã nộp
    Refunded = 3,   // Đã hoàn
    Forfeited = 4   // Đã tịch thu
}

public enum IncidentStatusEnum
{
    New = 1,        // Mới
    Processing = 2, // Đang xử lý
    Resolved = 3    // Đã xử lý
}

public enum PurchaseOrderStatusEnum
{
    Pending = 1,    // Chờ xác nhận
    Confirmed = 2,  // Đã nhập
    Cancelled = 3   // Đã hủy
}

public enum PromotionTypeEnum
{
    Percentage = 1,  // Phần trăm
    FixedAmount = 2  // Số tiền cố định
}

public enum PaymentMethodEnum
{
    Cash = 1,           // Tiền mặt
    BankTransfer = 2,   // Chuyển khoản
    VNPay = 3,
    MoMo = 4
}

public enum NotificationType
{
    BookingConfirm,
    BookingCancel,
    Payment,
    Deposit,
    Incident,
    Review,
    System
}

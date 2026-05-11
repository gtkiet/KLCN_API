using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KLCN_API.Models.Entities;

// ── Lookup tables ──────────────────────────────────────────

public class Role
{
    [Key] public int RoleId { get; set; }
    [MaxLength(50)] public string Name { get; set; } = null!;

    public ICollection<User> Users { get; set; } = [];
}

public class UserStatus
{
    [Key] public int StatusId { get; set; }
    [MaxLength(50)] public string Name { get; set; } = null!;
}

public class FieldType
{
    [Key] public int TypeId { get; set; }
    [MaxLength(50)] public string Name { get; set; } = null!;

    public ICollection<Field> Fields { get; set; } = [];
}

public class FieldStatus
{
    [Key] public int StatusId { get; set; }
    [MaxLength(50)] public string Name { get; set; } = null!;
}

public class FieldSlotStatus
{
    [Key] public int StatusId { get; set; }
    [MaxLength(50)] public string Name { get; set; } = null!;
}

public class BookingStatus
{
    [Key] public int StatusId { get; set; }
    [MaxLength(50)] public string Name { get; set; } = null!;
}

public class PaymentStatus
{
    [Key] public int StatusId { get; set; }
    [MaxLength(50)] public string Name { get; set; } = null!;
}

public class PaymentMethod
{
    [Key] public int MethodId { get; set; }
    [MaxLength(50)] public string Name { get; set; } = null!;
}

public class DepositStatus
{
    [Key] public int StatusId { get; set; }
    [MaxLength(50)] public string Name { get; set; } = null!;
}

public class IncidentStatus
{
    [Key] public int StatusId { get; set; }
    [MaxLength(50)] public string Name { get; set; } = null!;
}

public class PurchaseOrderStatus
{
    [Key] public int StatusId { get; set; }
    [MaxLength(50)] public string Name { get; set; } = null!;
}

public class PromotionType
{
    [Key] public int TypeId { get; set; }
    [MaxLength(50)] public string Name { get; set; } = null!;
}

// ── System config ──────────────────────────────────────────

public class SystemConfig
{
    [Key][MaxLength(100)] public string ConfigKey { get; set; } = null!;
    [MaxLength(500)] public string ConfigValue { get; set; } = null!;
    [MaxLength(20)] public string DataType { get; set; } = "STRING";
    [MaxLength(500)] public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    [ForeignKey(nameof(UpdatedBy))] public User? UpdatedByUser { get; set; }
}

// ── Users ──────────────────────────────────────────────────

public class User
{
    [Key] public int UserId { get; set; }
    [MaxLength(100)] public string Email { get; set; } = null!;
    [MaxLength(20)] public string Phone { get; set; } = null!;
    [MaxLength(255)] public string PasswordHash { get; set; } = null!;
    [MaxLength(100)] public string FullName { get; set; } = null!;
    public int RoleId { get; set; }
    public int StatusId { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    [ForeignKey(nameof(RoleId))] public Role Role { get; set; } = null!;
    [ForeignKey(nameof(StatusId))] public UserStatus Status { get; set; } = null!;
    public Profile? Profile { get; set; }
    public ICollection<Booking> Bookings { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}

public class Profile
{
    [Key] public int ProfileId { get; set; }
    public int UserId { get; set; }
    [MaxLength(500)] public string? AvatarUrl { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    [MaxLength(255)] public string? Address { get; set; }

    [ForeignKey(nameof(UserId))] public User User { get; set; } = null!;
}

public class RefreshToken
{
    [Key] public int TokenId { get; set; }
    public int UserId { get; set; }
    [MaxLength(500)] public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(UserId))] public User User { get; set; } = null!;
}

// ── Fields ─────────────────────────────────────────────────

public class Field
{
    [Key] public int FieldId { get; set; }
    [MaxLength(100)] public string Name { get; set; } = null!;
    [MaxLength(500)] public string? Description { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal BasePrice { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal PeakPrice { get; set; }
    [MaxLength(500)] public string? ImageUrl { get; set; }
    public int TypeId { get; set; }
    public int StatusId { get; set; } = 1;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [ForeignKey(nameof(TypeId))] public FieldType Type { get; set; } = null!;
    [ForeignKey(nameof(StatusId))] public FieldStatus Status { get; set; } = null!;
    public ICollection<FieldSlot> FieldSlots { get; set; } = [];
    public ICollection<FieldPriceHistory> PriceHistories { get; set; } = [];
    public ICollection<FieldMaintenanceLog> MaintenanceLogs { get; set; } = [];
    // Review.FieldId là FK trực tiếp về Fields nên cần navigation này
    public ICollection<Review> Reviews { get; set; } = [];
}

public class FieldPriceHistory
{
    [Key] public int HistoryId { get; set; }
    public int FieldId { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal OldBasePrice { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal OldPeakPrice { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal NewBasePrice { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal NewPeakPrice { get; set; }
    public int ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
    [MaxLength(255)] public string? Reason { get; set; }

    [ForeignKey(nameof(FieldId))] public Field Field { get; set; } = null!;
    [ForeignKey(nameof(ChangedBy))] public User ChangedByUser { get; set; } = null!;
}

public class TimeSlot
{
    [Key] public int SlotId { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsPeakHour { get; set; }

    public ICollection<FieldSlot> FieldSlots { get; set; } = [];
    // PeakSchedules tham chiếu TimeSlot để cấu hình cao điểm theo thứ
    public ICollection<PeakSchedule> PeakSchedules { get; set; } = [];
}

public class FieldSlot
{
    [Key] public int FieldSlotId { get; set; }
    public int FieldId { get; set; }
    public int SlotId { get; set; }
    public DateOnly SlotDate { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal Price { get; set; }
    public int StatusId { get; set; } = 1;
    public DateTime? HoldExpireAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [ForeignKey(nameof(FieldId))] public Field Field { get; set; } = null!;
    [ForeignKey(nameof(SlotId))] public TimeSlot TimeSlot { get; set; } = null!;
    [ForeignKey(nameof(StatusId))] public FieldSlotStatus Status { get; set; } = null!;
    // Unique constraint UQ_BookingDetail_Slot đảm bảo 1 FieldSlot chỉ thuộc 1 BookingDetail
    public BookingDetail? BookingDetail { get; set; }
}

public class FieldMaintenanceLog
{
    [Key] public int LogId { get; set; }
    public int FieldId { get; set; }
    [MaxLength(500)] public string Reason { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(FieldId))] public Field Field { get; set; } = null!;
    [ForeignKey(nameof(CreatedBy))] public User CreatedByUser { get; set; } = null!;
}

// ── Special days & peak schedules ─────────────────────────

public class SpecialDay
{
    [Key] public int SpecialDayId { get; set; }
    public DateOnly SpecialDate { get; set; }
    [MaxLength(100)] public string Name { get; set; } = null!;
    [Column(TypeName = "decimal(5,2)")] public decimal PriceMultiplier { get; set; } = 1.0m;
    public bool IsFullDayPeak { get; set; }
    [MaxLength(255)] public string? Note { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(CreatedBy))] public User CreatedByUser { get; set; } = null!;
}

public class PeakSchedule
{
    [Key] public int PeakScheduleId { get; set; }
    public byte DayOfWeek { get; set; }
    public int SlotId { get; set; }
    public bool IsPeak { get; set; } = true;

    [ForeignKey(nameof(SlotId))] public TimeSlot TimeSlot { get; set; } = null!;
}

// ── Bookings ───────────────────────────────────────────────

public class Booking
{
    [Key] public int BookingId { get; set; }
    public int UserId { get; set; }
    public int StatusId { get; set; } = 1;
    [Column(TypeName = "decimal(12,2)")] public decimal? SubTotal { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal DiscountAmount { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal TaxAmount { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal? TotalAmount { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal DepositAmount { get; set; }
    public int? PromotionId { get; set; }
    [MaxLength(500)] public string? Note { get; set; }
    [MaxLength(500)] public string? CancelReason { get; set; }
    public int RescheduleCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [ForeignKey(nameof(UserId))] public User User { get; set; } = null!;
    [ForeignKey(nameof(StatusId))] public BookingStatus Status { get; set; } = null!;
    [ForeignKey(nameof(PromotionId))] public Promotion? Promotion { get; set; }
    public ICollection<BookingDetail> BookingDetails { get; set; } = [];
    public ICollection<BookingService> BookingServices { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
    public ICollection<BookingLog> BookingLogs { get; set; } = [];
    public Deposit? Deposit { get; set; }
    public Review? Review { get; set; }
}

public class BookingDetail
{
    [Key] public int BookingDetailId { get; set; }
    public int BookingId { get; set; }
    public int FieldSlotId { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal Price { get; set; }

    [ForeignKey(nameof(BookingId))] public Booking Booking { get; set; } = null!;
    [ForeignKey(nameof(FieldSlotId))] public FieldSlot FieldSlot { get; set; } = null!;
}

public class BookingLog
{
    [Key] public int LogId { get; set; }
    public int BookingId { get; set; }
    public int? OldStatusId { get; set; }
    public int NewStatusId { get; set; }
    public int? ChangedByUserId { get; set; }
    [MaxLength(500)] public string? Note { get; set; }
    public DateTime ChangedAt { get; set; }

    [ForeignKey(nameof(BookingId))] public Booking Booking { get; set; } = null!;
    [ForeignKey(nameof(ChangedByUserId))] public User? ChangedByUser { get; set; }
}

public class Payment
{
    [Key] public int PaymentId { get; set; }
    public int BookingId { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal Amount { get; set; }
    public int StatusId { get; set; } = 1;
    public int MethodId { get; set; }
    [MaxLength(100)] public string? TransactionCode { get; set; }
    public string? GatewayResponse { get; set; }
    [MaxLength(255)] public string? Note { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(BookingId))] public Booking Booking { get; set; } = null!;
    [ForeignKey(nameof(StatusId))] public PaymentStatus Status { get; set; } = null!;
    [ForeignKey(nameof(MethodId))] public PaymentMethod Method { get; set; } = null!;
}

public class Deposit
{
    [Key] public int DepositId { get; set; }
    public int BookingId { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal RequiredAmount { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal PaidAmount { get; set; }
    public int StatusId { get; set; } = 1;
    public DateTime DeadlineAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public DateTime? ForfeitedAt { get; set; }
    public int? PaymentId { get; set; }
    [MaxLength(255)] public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [ForeignKey(nameof(BookingId))] public Booking Booking { get; set; } = null!;
    [ForeignKey(nameof(StatusId))] public DepositStatus Status { get; set; } = null!;
    [ForeignKey(nameof(PaymentId))] public Payment? Payment { get; set; }
}

// ── Services ───────────────────────────────────────────────

public class Service
{
    [Key] public int ServiceId { get; set; }
    [MaxLength(100)] public string Name { get; set; } = null!;
    [MaxLength(500)] public string? Description { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal Price { get; set; }
    [MaxLength(500)] public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class BookingService
{
    [Key] public int BookingServiceId { get; set; }
    public int BookingId { get; set; }
    public int ServiceId { get; set; }
    public int Quantity { get; set; } = 1;
    [Column(TypeName = "decimal(12,2)")] public decimal UnitPrice { get; set; }

    [ForeignKey(nameof(BookingId))] public Booking Booking { get; set; } = null!;
    [ForeignKey(nameof(ServiceId))] public Service Service { get; set; } = null!;
}

// ── Promotions ─────────────────────────────────────────────

public class Promotion
{
    [Key] public int PromotionId { get; set; }
    [MaxLength(50)] public string Code { get; set; } = null!;
    [MaxLength(200)] public string Name { get; set; } = null!;
    [MaxLength(500)] public string? Description { get; set; }
    public int TypeId { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal DiscountValue { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal? MaxDiscount { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal MinOrderAmount { get; set; }
    public int UsageLimit { get; set; } = 1;
    public int UsageCount { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(TypeId))] public PromotionType Type { get; set; } = null!;
    [ForeignKey(nameof(CreatedBy))] public User CreatedByUser { get; set; } = null!;
}

// ── Inventory ──────────────────────────────────────────────

public class Supplier
{
    [Key] public int SupplierId { get; set; }
    [MaxLength(100)] public string Name { get; set; } = null!;
    [MaxLength(100)] public string? ContactName { get; set; }
    [MaxLength(20)] public string? Phone { get; set; }
    [MaxLength(100)] public string? Email { get; set; }
    [MaxLength(255)] public string? Address { get; set; }
    public bool IsDeleted { get; set; }

    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = [];
}

public class Product
{
    [Key] public int ProductId { get; set; }
    [MaxLength(100)] public string Name { get; set; } = null!;
    [MaxLength(50)] public string? Unit { get; set; }
    public int StockQty { get; set; }
    public int MinQty { get; set; } = 5;
    public bool IsDeleted { get; set; }
}

public class PurchaseOrder
{
    [Key] public int PurchaseOrderId { get; set; }
    public int SupplierId { get; set; }
    public int CreatedByUserId { get; set; }
    public int StatusId { get; set; } = 1;
    [Column(TypeName = "decimal(12,2)")] public decimal? TotalAmount { get; set; }
    [MaxLength(500)] public string? Note { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(SupplierId))] public Supplier Supplier { get; set; } = null!;
    [ForeignKey(nameof(CreatedByUserId))] public User CreatedByUser { get; set; } = null!;
    [ForeignKey(nameof(StatusId))] public PurchaseOrderStatus Status { get; set; } = null!;
    public ICollection<PurchaseOrderDetail> Details { get; set; } = [];
}

public class PurchaseOrderDetail
{
    [Key] public int PurchaseOrderDetailId { get; set; }
    public int PurchaseOrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal UnitPrice { get; set; }

    [ForeignKey(nameof(PurchaseOrderId))] public PurchaseOrder PurchaseOrder { get; set; } = null!;
    [ForeignKey(nameof(ProductId))] public Product Product { get; set; } = null!;
}

// ── Incidents ──────────────────────────────────────────────

public class Incident
{
    [Key] public int IncidentId { get; set; }
    public int FieldId { get; set; }
    public int ReportedByUserId { get; set; }
    [MaxLength(200)] public string Title { get; set; } = null!;
    [MaxLength(1000)] public string? Description { get; set; }
    [MaxLength(500)] public string? ImageUrl { get; set; }
    public int StatusId { get; set; } = 1;
    public int? HandledByUserId { get; set; }
    public DateTime? HandledAt { get; set; }
    [MaxLength(500)] public string? HandledNote { get; set; }
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(FieldId))] public Field Field { get; set; } = null!;
    [ForeignKey(nameof(ReportedByUserId))] public User ReportedByUser { get; set; } = null!;
    [ForeignKey(nameof(HandledByUserId))] public User? HandledByUser { get; set; }
    [ForeignKey(nameof(StatusId))] public IncidentStatus Status { get; set; } = null!;
}

// ── Reviews ────────────────────────────────────────────────

public class Review
{
    [Key] public int ReviewId { get; set; }
    public int BookingId { get; set; }
    public int UserId { get; set; }
    public int FieldId { get; set; }
    public byte Rating { get; set; }
    [MaxLength(1000)] public string? Comment { get; set; }
    [MaxLength(500)] public string? ImageUrl { get; set; }
    public bool IsVisible { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [ForeignKey(nameof(BookingId))] public Booking Booking { get; set; } = null!;
    [ForeignKey(nameof(UserId))] public User User { get; set; } = null!;
    [ForeignKey(nameof(FieldId))] public Field Field { get; set; } = null!;
}

// ── Notifications ──────────────────────────────────────────

public class Notification
{
    [Key] public int NotificationId { get; set; }
    public int UserId { get; set; }
    [MaxLength(200)] public string Title { get; set; } = null!;
    [MaxLength(1000)] public string? Body { get; set; }
    [MaxLength(50)] public string? Type { get; set; }
    public int? RefId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(UserId))] public User User { get; set; } = null!;
}
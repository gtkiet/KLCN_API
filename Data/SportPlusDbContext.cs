using Microsoft.EntityFrameworkCore;
using KLCN_API.Models.Entities;

namespace KLCN_API.Data;

public class SportPlusDbContext : DbContext
{
    public SportPlusDbContext(DbContextOptions<SportPlusDbContext> options)
        : base(options) { }

    // ── Lookup Tables ────────────────────────────────────────────
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserStatus> UserStatuses { get; set; }
    public DbSet<FieldType> FieldTypes { get; set; }
    public DbSet<FieldStatus> FieldStatuses { get; set; }
    public DbSet<FieldSlotStatus> FieldSlotStatuses { get; set; }
    public DbSet<BookingStatus> BookingStatuses { get; set; }
    public DbSet<PaymentStatus> PaymentStatuses { get; set; }
    public DbSet<PaymentMethod> PaymentMethods { get; set; }
    public DbSet<DepositStatus> DepositStatuses { get; set; }
    public DbSet<IncidentStatus> IncidentStatuses { get; set; }
    public DbSet<PurchaseOrderStatus> PurchaseOrderStatuses { get; set; }
    public DbSet<PromotionType> PromotionTypes { get; set; }

    // ── System ───────────────────────────────────────────────────
    public DbSet<SystemConfig> SystemConfigs { get; set; }

    // ── Users ────────────────────────────────────────────────────
    public DbSet<User> Users { get; set; }
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    // ── Fields ───────────────────────────────────────────────────
    public DbSet<Field> Fields { get; set; }
    public DbSet<FieldPriceHistory> FieldPriceHistories { get; set; }
    public DbSet<TimeSlot> TimeSlots { get; set; }
    public DbSet<FieldSlot> FieldSlots { get; set; }
    public DbSet<FieldMaintenanceLog> FieldMaintenanceLogs { get; set; }

    // ── Special Days & Peak ──────────────────────────────────────
    public DbSet<SpecialDay> SpecialDays { get; set; }
    public DbSet<PeakSchedule> PeakSchedules { get; set; }

    // ── Bookings ─────────────────────────────────────────────────
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<BookingDetail> BookingDetails { get; set; }
    public DbSet<BookingLog> BookingLogs { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Deposit> Deposits { get; set; }

    // ── Services ─────────────────────────────────────────────────
    public DbSet<Service> Services { get; set; }
    public DbSet<BookingService> BookingServices { get; set; }

    // ── Promotions ───────────────────────────────────────────────
    public DbSet<Promotion> Promotions { get; set; }

    // ── Inventory ────────────────────────────────────────────────
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<PurchaseOrderDetail> PurchaseOrderDetails { get; set; }

    // ── Incidents & Reviews ──────────────────────────────────────
    public DbSet<Incident> Incidents { get; set; }
    public DbSet<Review> Reviews { get; set; }

    // ── Notifications ────────────────────────────────────────────
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Soft delete: KHÔNG dùng global query filter ──────────
        // Lý do: các entity User/Field/Service/Supplier/Product là
        // "required end" của nhiều relationships. Global filter gây
        // warning 10622 và có thể trả kết quả sai khi join.
        // → Filter IsDeleted = false thủ công tại từng query cần thiết.
        // Ví dụ: _ctx.Users.Where(u => !u.IsDeleted)
        //         _ctx.Fields.Where(f => !f.IsDeleted)

        // ── Unique constraints ───────────────────────────────────
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Phone).IsUnique();

        modelBuilder.Entity<TimeSlot>()
            .HasIndex(t => new { t.StartTime, t.EndTime }).IsUnique();

        modelBuilder.Entity<FieldSlot>()
            .HasIndex(fs => new { fs.FieldId, fs.SlotId, fs.SlotDate }).IsUnique();

        modelBuilder.Entity<Promotion>()
            .HasIndex(p => p.Code).IsUnique();

        modelBuilder.Entity<SpecialDay>()
            .HasIndex(s => s.SpecialDate).IsUnique();

        modelBuilder.Entity<PeakSchedule>()
            .HasIndex(ps => new { ps.DayOfWeek, ps.SlotId }).IsUnique();

        // BookingDetail: 1 FieldSlot chỉ thuộc 1 BookingDetail
        modelBuilder.Entity<BookingDetail>()
            .HasIndex(bd => bd.FieldSlotId).IsUnique();

        // Deposit: 1 booking chỉ có 1 deposit
        modelBuilder.Entity<Deposit>()
            .HasIndex(d => d.BookingId).IsUnique();

        // Review: 1 booking chỉ có 1 review
        modelBuilder.Entity<Review>()
            .HasIndex(r => r.BookingId).IsUnique();

        // BookingService: unique per booking+service
        modelBuilder.Entity<BookingService>()
            .HasIndex(bs => new { bs.BookingId, bs.ServiceId }).IsUnique();

        modelBuilder.Entity<PurchaseOrderDetail>()
            .HasIndex(pod => new { pod.PurchaseOrderId, pod.ProductId }).IsUnique();

        // ── Self-referencing & multi-FK relationships ────────────

        // BookingLog: OldStatusId / NewStatusId đều FK về BookingStatuses
        // → tắt cascade để tránh multiple cascade paths
        // BookingStatus không có inverse navigation → WithMany() không tham số
        modelBuilder.Entity<BookingLog>()
            .HasOne<BookingStatus>()
            .WithMany()
            .HasForeignKey(bl => bl.OldStatusId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<BookingLog>()
            .HasOne<BookingStatus>()
            .WithMany()
            .HasForeignKey(bl => bl.NewStatusId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<BookingLog>()
            .HasOne(bl => bl.ChangedByUser)
            .WithMany()
            .HasForeignKey(bl => bl.ChangedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<BookingLog>()
            .HasOne(bl => bl.Booking)
            .WithMany(b => b.BookingLogs)
            .HasForeignKey(bl => bl.BookingId)
            .OnDelete(DeleteBehavior.NoAction);

        // Incident: 2 FK về Users → tắt cascade
        modelBuilder.Entity<Incident>()
            .HasOne(i => i.ReportedByUser)
            .WithMany()
            .HasForeignKey(i => i.ReportedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Incident>()
            .HasOne(i => i.HandledByUser)
            .WithMany()
            .HasForeignKey(i => i.HandledByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        // FieldPriceHistory: ChangedBy → tắt cascade
        modelBuilder.Entity<FieldPriceHistory>()
            .HasOne(fph => fph.ChangedByUser)
            .WithMany()
            .HasForeignKey(fph => fph.ChangedBy)
            .OnDelete(DeleteBehavior.NoAction);

        // FieldMaintenanceLog: CreatedBy → tắt cascade
        modelBuilder.Entity<FieldMaintenanceLog>()
            .HasOne(fml => fml.CreatedByUser)
            .WithMany()
            .HasForeignKey(fml => fml.CreatedBy)
            .OnDelete(DeleteBehavior.NoAction);

        // SystemConfig: UpdatedBy → tắt cascade
        modelBuilder.Entity<SystemConfig>()
            .HasOne(sc => sc.UpdatedByUser)
            .WithMany()
            .HasForeignKey(sc => sc.UpdatedBy)
            .OnDelete(DeleteBehavior.NoAction);

        // Booking → Promotion: tắt cascade
        // Promotion không có inverse navigation Bookings → WithMany() không tham số
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Promotion)
            .WithMany()
            .HasForeignKey(b => b.PromotionId)
            .OnDelete(DeleteBehavior.NoAction);

        // Booking → User: tắt cascade (User có nhiều booking)
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.User)
            .WithMany(u => u.Bookings)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // Deposit → Payment (PaymentId): tắt cascade
        modelBuilder.Entity<Deposit>()
            .HasOne(d => d.Payment)
            .WithMany()
            .HasForeignKey(d => d.PaymentId)
            .OnDelete(DeleteBehavior.NoAction);

        // Review → Booking, User, Field: tắt cascade
        modelBuilder.Entity<Review>()
            .HasOne(r => r.Booking)
            .WithOne(b => b.Review)
            .HasForeignKey<Review>(r => r.BookingId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // SpecialDay → User
        modelBuilder.Entity<SpecialDay>()
            .HasOne(sd => sd.CreatedByUser)
            .WithMany()
            .HasForeignKey(sd => sd.CreatedBy)
            .OnDelete(DeleteBehavior.NoAction);

        // Promotion → User
        modelBuilder.Entity<Promotion>()
            .HasOne(p => p.CreatedByUser)
            .WithMany()
            .HasForeignKey(p => p.CreatedBy)
            .OnDelete(DeleteBehavior.NoAction);

        // ── Performance indexes ──────────────────────────────────
        modelBuilder.Entity<FieldSlot>()
            .HasIndex(fs => new { fs.FieldId, fs.SlotDate, fs.StatusId });

        modelBuilder.Entity<FieldSlot>()
            .HasIndex(fs => fs.HoldExpireAt)
            .HasFilter("[StatusId] = 2"); // Đang giữ

        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.UserId);

        modelBuilder.Entity<Booking>()
            .HasIndex(b => new { b.StatusId, b.CreatedAt });

        modelBuilder.Entity<Deposit>()
            .HasIndex(d => new { d.StatusId, d.DeadlineAt });

        modelBuilder.Entity<Deposit>()
            .HasIndex(d => d.DeadlineAt)
            .HasFilter("[StatusId] = 1"); // Chờ nộp

        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });

        modelBuilder.Entity<Incident>()
            .HasIndex(i => new { i.FieldId, i.StatusId });

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.Token);

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => new { rt.UserId, rt.IsRevoked });

        modelBuilder.Entity<Promotion>()
            .HasIndex(p => p.Code)
            .HasFilter("[IsActive] = 1");
    }
}
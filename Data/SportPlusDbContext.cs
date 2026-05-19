using Microsoft.EntityFrameworkCore;
using KLCN_API.Models.Entities;

namespace KLCN_API.Data;

public class SportPlusDbContext : DbContext
{
    public SportPlusDbContext(DbContextOptions<SportPlusDbContext> options)
        : base(options) { }

    // ── Lookup tables ────────────────────────────────────────────
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
    // DbSet ten "SystemConfigs" nhung bang SQL la "SystemConfig" -> can ToTable()
    public DbSet<SystemConfig> SystemConfigs { get; set; }

    // ── Users ────────────────────────────────────────────────────
    public DbSet<User> Users { get; set; }
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    // ── Fields ───────────────────────────────────────────────────
    public DbSet<Field> Fields { get; set; }
    // DbSet ten "FieldPriceHistories" nhung bang SQL la "FieldPriceHistory" -> can ToTable()
    public DbSet<FieldPriceHistory> FieldPriceHistories { get; set; }
    public DbSet<TimeSlot> TimeSlots { get; set; }
    public DbSet<FieldSlot> FieldSlots { get; set; }
    public DbSet<FieldMaintenanceLog> FieldMaintenanceLogs { get; set; }

    // ── Special days & peak schedules ────────────────────────────
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

    // ── Incidents & reviews ──────────────────────────────────────
    public DbSet<Incident> Incidents { get; set; }
    public DbSet<Review> Reviews { get; set; }

    // ── Notifications ────────────────────────────────────────────
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Table name overrides ──────────────────────────────────
        // EF Core pluralize ten DbSet thanh ten bang theo convention,
        // nhung hai bang nay trong SQL duoc dat ten so it / khac quy tac.
        modelBuilder.Entity<SystemConfig>()
            .ToTable("SystemConfig");

        // FieldPriceHistory: ten bang khac convention + co trigger ghi vao bang nay
        // -> UseSqlOutputClause(false) de EF khong dung OUTPUT clause khi INSERT/UPDATE.
        modelBuilder.Entity<FieldPriceHistory>()
            .ToTable("FieldPriceHistory", tb => tb.UseSqlOutputClause(false));

        // ── Trigger-safe tables ───────────────────────────────────
        // Cac bang co DATABASE TRIGGER se gap loi DbUpdateException khi EF Core
        // dung lenh INSERT/UPDATE ... OUTPUT INSERTED.* (khong co INTO clause).
        // Fix: bao EF dung SELECT rieng sau DML thay vi OUTPUT clause.
        //
        // Triggers trong schema:
        //   trg_Fields_PriceHistory  -> ON Fields   AFTER UPDATE
        //   trg_Bookings_StatusLog   -> ON Bookings  AFTER UPDATE
        modelBuilder.Entity<Field>()
            .ToTable("Fields", tb => tb.UseSqlOutputClause(false));

        modelBuilder.Entity<Booking>()
            .ToTable("Bookings", tb => tb.UseSqlOutputClause(false));

        // ── Soft delete ───────────────────────────────────────────
        // Khong dung global query filter de tranh EF warning 10622
        // (required-end relationship bi filter gay ket qua sai khi join).
        // Loc thu cong tai tung query:
        //   _ctx.Users.Where(u => !u.IsDeleted)
        //   _ctx.Fields.Where(f => !f.IsDeleted)

        // ── Unique constraints ────────────────────────────────────
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

        // 1 FieldSlot chi thuoc 1 BookingDetail (unique FK)
        modelBuilder.Entity<BookingDetail>()
            .HasIndex(bd => bd.FieldSlotId).IsUnique();

        // 1 Booking chi co 1 Deposit
        modelBuilder.Entity<Deposit>()
            .HasIndex(d => d.BookingId).IsUnique();

        // 1 Booking chi co 1 Review
        modelBuilder.Entity<Review>()
            .HasIndex(r => r.BookingId).IsUnique();

        // 1 Booking chi dat moi Service 1 lan
        modelBuilder.Entity<BookingService>()
            .HasIndex(bs => new { bs.BookingId, bs.ServiceId }).IsUnique();

        modelBuilder.Entity<PurchaseOrderDetail>()
            .HasIndex(pod => new { pod.PurchaseOrderId, pod.ProductId }).IsUnique();

        // ── Relationships ─────────────────────────────────────────

        // FieldSlot <-> BookingDetail: 1-1 optional.
        // Khai bao tuong minh de EF khong tu sinh FK nguoc hoac nham
        // voi unique index da co tren BookingDetail.FieldSlotId.
        modelBuilder.Entity<FieldSlot>()
            .HasOne(fs => fs.BookingDetail)
            .WithOne(bd => bd.FieldSlot)
            .HasForeignKey<BookingDetail>(bd => bd.FieldSlotId)
            .OnDelete(DeleteBehavior.NoAction);

        // BookingLog: OldStatusId va NewStatusId cung tro ve BookingStatuses
        // -> tat cascade de tranh multiple cascade paths.
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

        // Incident: ReportedByUserId va HandledByUserId cung tro ve Users.
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

        // FieldPriceHistory.ChangedBy -> Users
        modelBuilder.Entity<FieldPriceHistory>()
            .HasOne(fph => fph.ChangedByUser)
            .WithMany()
            .HasForeignKey(fph => fph.ChangedBy)
            .OnDelete(DeleteBehavior.NoAction);

        // FieldMaintenanceLog.CreatedBy -> Users
        modelBuilder.Entity<FieldMaintenanceLog>()
            .HasOne(fml => fml.CreatedByUser)
            .WithMany()
            .HasForeignKey(fml => fml.CreatedBy)
            .OnDelete(DeleteBehavior.NoAction);

        // SystemConfig.UpdatedBy -> Users
        modelBuilder.Entity<SystemConfig>()
            .HasOne(sc => sc.UpdatedByUser)
            .WithMany()
            .HasForeignKey(sc => sc.UpdatedBy)
            .OnDelete(DeleteBehavior.NoAction);

        // Booking.PromotionId -> Promotions (Promotion khong co navigation nguoc)
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Promotion)
            .WithMany()
            .HasForeignKey(b => b.PromotionId)
            .OnDelete(DeleteBehavior.NoAction);

        // Booking.UserId -> Users
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.User)
            .WithMany(u => u.Bookings)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // Deposit.PaymentId -> Payments (Payment khong quan ly Deposit nguoc lai)
        modelBuilder.Entity<Deposit>()
            .HasOne(d => d.Payment)
            .WithMany()
            .HasForeignKey(d => d.PaymentId)
            .OnDelete(DeleteBehavior.NoAction);

        // Review <-> Booking: 1-1
        modelBuilder.Entity<Review>()
            .HasOne(r => r.Booking)
            .WithOne(b => b.Review)
            .HasForeignKey<Review>(r => r.BookingId)
            .OnDelete(DeleteBehavior.NoAction);

        // Review.UserId -> Users
        modelBuilder.Entity<Review>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // Review.FieldId -> Fields: tat cascade vi Field da co nhieu FK khac,
        // de mac dinh se gay multiple cascade paths.
        modelBuilder.Entity<Review>()
            .HasOne(r => r.Field)
            .WithMany(f => f.Reviews)
            .HasForeignKey(r => r.FieldId)
            .OnDelete(DeleteBehavior.NoAction);

        // SpecialDay.CreatedBy -> Users
        modelBuilder.Entity<SpecialDay>()
            .HasOne(sd => sd.CreatedByUser)
            .WithMany()
            .HasForeignKey(sd => sd.CreatedBy)
            .OnDelete(DeleteBehavior.NoAction);

        // Promotion.CreatedBy -> Users
        modelBuilder.Entity<Promotion>()
            .HasOne(p => p.CreatedByUser)
            .WithMany()
            .HasForeignKey(p => p.CreatedBy)
            .OnDelete(DeleteBehavior.NoAction);

        // ── Performance indexes ───────────────────────────────────

        // FieldSlots: tim kiem theo san + ngay + trang thai
        modelBuilder.Entity<FieldSlot>()
            .HasIndex(fs => new { fs.FieldId, fs.SlotDate, fs.StatusId });

        // FieldSlots: giai phong slot het han hold (StatusId=2: Dang giu)
        modelBuilder.Entity<FieldSlot>()
            .HasIndex(fs => fs.HoldExpireAt)
            .HasFilter("[StatusId] = 2");

        // Bookings: tra cuu theo user, loc theo trang thai + thoi gian tao
        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.UserId);
        modelBuilder.Entity<Booking>()
            .HasIndex(b => new { b.StatusId, b.CreatedAt });

        // Deposits: xu ly het han va loc theo trang thai + deadline
        modelBuilder.Entity<Deposit>()
            .HasIndex(d => new { d.StatusId, d.DeadlineAt });

        // Deposits: tim nhanh deposit cho nop (StatusId=1: Cho nop)
        modelBuilder.Entity<Deposit>()
            .HasIndex(d => d.DeadlineAt)
            .HasFilter("[StatusId] = 1");

        // Notifications: doc thong bao chua doc cua user, sap xep moi nhat truoc
        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });

        // Incidents: loc su co theo san va trang thai
        modelBuilder.Entity<Incident>()
            .HasIndex(i => new { i.FieldId, i.StatusId });

        // RefreshTokens: xac thuc token va loc token con hieu luc cua user
        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.Token);
        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => new { rt.UserId, rt.IsRevoked });

        // Promotions: tra cuu ma voucher, chi index voucher dang active
        modelBuilder.Entity<Promotion>()
            .HasIndex(p => p.Code)
            .HasFilter("[IsActive] = 1");
    }
}
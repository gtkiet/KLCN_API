using KLCN_API.Models.Entities;

namespace KLCN_API.Repositories.Interfaces;

// ================================================================
// Auth
// ================================================================

public interface IAuthRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> PhoneExistsAsync(string phone);
    Task<User> CreateUserAsync(User user, Profile profile);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task AddRefreshTokenAsync(RefreshToken token);
    Task RevokeRefreshTokenAsync(string token);
    Task RevokeAllRefreshTokensAsync(int userId);
}

// ================================================================
// Users
// ================================================================

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int userId);
    Task<User?> GetByEmailAsync(string email);

    Task<User?> GetByPhoneAsync(string phone);
    Task<(List<User> Items, int TotalCount)> GetUsersAsync(
        string? search, int? roleId, int? statusId, int page, int pageSize);
    Task UpdateRoleAsync(int userId, int roleId);
    Task CreateAsync(User user);
    Task UpdateAsync(User user);

    Task UpdateProfileAsync(int userId, string? fullName, string? phone,
        string? avatarUrl, DateOnly? dateOfBirth, string? address);

    Task UpdateStatusAsync(int userId, int statusId);
    Task SoftDeleteAsync(int userId);
    Task UpdatePasswordAsync(int userId, string newPasswordHash);
}

// ================================================================
// Fields
// ================================================================

public interface IFieldRepository
{
    Task<Field?> GetByIdAsync(int fieldId);
    Task<(List<Field> Items, int TotalCount)> GetFieldsAsync(
        string? search, int? typeId, int? statusId, int page, int pageSize);
    Task<Field> CreateAsync(Field field);
    Task UpdateAsync(Field field);
    Task SoftDeleteAsync(int fieldId);

    Task<List<FieldSlot>> GetScheduleAsync(int? fieldId, DateOnly date);
    Task<FieldSlot?> GetSlotByIdAsync(int fieldSlotId);
    Task<List<FieldSlot>> GetSlotsByIdsAsync(List<int> fieldSlotIds);

    Task<List<FieldPriceHistory>> GetPriceHistoryAsync(int fieldId);

    Task<List<FieldMaintenanceLog>> GetMaintenanceLogsAsync(int fieldId);
    Task<FieldMaintenanceLog> AddMaintenanceLogAsync(FieldMaintenanceLog log);
}

// ================================================================
// Bookings
// ================================================================

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(int bookingId);
    Task<Booking?> GetWithDetailsAsync(int bookingId);
    Task<(List<Booking> Items, int TotalCount)> GetBookingsAsync(
        int? userId, int? statusId, DateOnly? dateFrom, DateOnly? dateTo,
        int? fieldId, int page, int pageSize);
    Task<List<Booking>> GetActiveByUserAsync(int userId);
    Task<Booking> CreateAsync(Booking booking);
    Task UpdateAsync(Booking booking);

    Task AddBookingDetailAsync(BookingDetail detail);
    Task AddBookingServiceAsync(BookingService service);
    Task<List<BookingService>> GetBookingServicesAsync(int bookingId);
}

// ================================================================
// Payments
// ================================================================

public interface IPaymentRepository
{
    Task<List<Payment>> GetByBookingAsync(int bookingId);
    Task<decimal> GetTotalPaidAsync(int bookingId);
    Task<Payment> AddAsync(Payment payment);
}

// ================================================================
// Deposits
// ================================================================

public interface IDepositRepository
{
    Task<Deposit?> GetByBookingAsync(int bookingId);
    Task<Deposit> AddAsync(Deposit deposit);
    Task UpdateAsync(Deposit deposit);
}

// ================================================================
// Promotions
// ================================================================

public interface IPromotionRepository
{
    Task<Promotion?> GetByIdAsync(int promotionId);
    Task<Promotion?> GetActiveByCodeAsync(string code);
    Task<(List<Promotion> Items, int TotalCount)> GetPromotionsAsync(
        bool? isActive, int page, int pageSize);
    Task<Promotion> CreateAsync(Promotion promotion);
    Task UpdateAsync(Promotion promotion);
    Task IncrementUsageAsync(int promotionId);
}

// ================================================================
// Services (dịch vụ đi kèm)
// ================================================================

public interface IServiceRepository
{
    Task<Service?> GetByIdAsync(int serviceId);
    Task<List<Service>> GetAllAsync(bool? isAvailable = null);
    Task<Service> CreateAsync(Service service);
    Task UpdateAsync(Service service);
    Task SoftDeleteAsync(int serviceId);
}

// ================================================================
// Inventory
// ================================================================

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(int supplierId);
    Task<(List<Supplier> Items, int TotalCount)> GetAllAsync(string? search, int page, int pageSize);
    Task<bool> NameExistsAsync(string name, int? excludeId = null);
    Task<Supplier> CreateAsync(Supplier supplier);
    Task UpdateAsync(Supplier supplier);
    Task SoftDeleteAsync(int supplierId);
}

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int productId);
    Task<(List<Product> Items, int TotalCount)> GetAllAsync(string? search, bool? lowStockOnly, int page, int pageSize);
    Task<bool> NameExistsAsync(string name, int? excludeId = null);
    Task<Product> CreateAsync(Product product);
    Task UpdateAsync(Product product);
    Task SoftDeleteAsync(int productId);
}

public interface IPurchaseOrderRepository
{
    Task<PurchaseOrder?> GetByIdAsync(int purchaseOrderId);
    Task<(List<PurchaseOrder> Items, int TotalCount)> GetAllAsync(int? supplierId, int? statusId, int page, int pageSize);
    Task<PurchaseOrder> CreateAsync(PurchaseOrder order, List<PurchaseOrderDetail> details);
    Task CancelAsync(int purchaseOrderId);
}

// ================================================================
// Incidents
// ================================================================

public interface IIncidentRepository
{
    Task<Incident?> GetByIdAsync(int incidentId);
    Task<(List<Incident> Items, int TotalCount)> GetIncidentsAsync(
        int? fieldId, int? statusId, int page, int pageSize);
    Task<Incident> CreateAsync(Incident incident);
    Task UpdateAsync(Incident incident);
}

// ================================================================
// Reviews
// ================================================================

//public interface IReviewRepository
//{
//    Task<Review?> GetByIdAsync(int reviewId);
//    Task<Review?> GetByBookingAsync(int bookingId);
//    Task<(List<Review> Items, int TotalCount)> GetReviewsAsync(
//        int? fieldId, int? rating, bool? isVisible, int page, int pageSize);
//    Task<Review> CreateAsync(Review review);
//    Task UpdateVisibilityAsync(int reviewId, bool isVisible);
//}

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(int reviewId);
    Task<Review?> GetByBookingAsync(int bookingId);
    Task<(List<Review> Items, int TotalCount)> GetReviewsAsync(
        int? fieldId, int? rating, bool? isVisible, int page, int pageSize);
    Task<Review> CreateAsync(Review review);
    Task UpdateVisibilityAsync(int reviewId, bool isVisible);
    Task<FieldRatingRaw?> GetFieldRatingRawAsync(int fieldId);
}

public class FieldRatingRaw
{
    public int FieldId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public int TotalReviews { get; set; }
    public decimal AvgRating { get; set; }
    public int Stars5 { get; set; }
    public int Stars4 { get; set; }
    public int Stars3 { get; set; }
    public int Stars2 { get; set; }
    public int Stars1 { get; set; }
}


// ================================================================
// Notifications
// ================================================================

public interface INotificationRepository
{
    Task<(List<Notification> Items, int TotalCount)> GetByUserAsync(
        int userId, bool? isRead, int page, int pageSize);
    Task<int> CountUnreadAsync(int userId);
    Task MarkAsReadAsync(int notificationId, int userId);
    Task MarkAllAsReadAsync(int userId);
    Task<Notification> AddAsync(Notification notification);
}

// ================================================================
// System config
// ================================================================

public interface ISystemConfigRepository
{
    Task<List<SystemConfig>> GetAllAsync();
    Task<SystemConfig?> GetByKeyAsync(string key);
    Task UpdateAsync(string key, string value, int updatedBy);
}

// ================================================================
// Special days
// ================================================================

public interface ISpecialDayRepository
{
    Task<List<SpecialDay>> GetAllAsync();
    Task<SpecialDay?> GetByIdAsync(int specialDayId);
    Task<SpecialDay?> GetByDateAsync(DateOnly date);
    Task<SpecialDay> CreateAsync(SpecialDay specialDay);
    Task UpdateAsync(SpecialDay specialDay);
    Task DeleteAsync(int specialDayId);
}

// ================================================================
// Dashboard
// ================================================================

public interface IDashboardRepository
{
    Task<DashboardRaw> GetSummaryAsync();
    Task<List<RevenueByMonthRaw>> GetRevenueByMonthAsync(int year);
    Task<List<FieldOccupancyRaw>> GetOccupancyAsync(int? year, int? month);
    Task<List<RevenueByServiceRaw>> GetRevenueByServiceAsync();
}

// ── Raw models dùng nội bộ — map từ View/SP, không expose ra ngoài ──

public class DashboardRaw
{
    public int PendingBookings { get; set; }
    public int PendingDepositBookings { get; set; }
    public int TodayConfirmed { get; set; }
    public int ActiveFields { get; set; }
    public int MaintenanceFields { get; set; }
    public int NewIncidents { get; set; }
    public decimal TodayRevenue { get; set; }
    public int ActiveCustomers { get; set; }
    public int LowStockCount { get; set; }
    public int UrgentDepositCount { get; set; }
}

public class RevenueByMonthRaw
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int TotalBookings { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AvgBookingValue { get; set; }
}

public class FieldOccupancyRaw
{
    public int FieldId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public int TotalSlots { get; set; }
    public int BookedSlots { get; set; }
    public decimal OccupancyRate { get; set; }
}
public class RevenueByServiceRaw
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int TotalQuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalBookings { get; set; }
}

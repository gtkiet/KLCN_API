//// ============================================================
//// Repositories/Interfaces/IRepositories.cs
//// Interface cho data access layer — placeholder
//// ============================================================
//// Quy ước:
////   - Dùng IGenericRepository<T> cho CRUD cơ bản
////   - Mỗi entity phức tạp có ISpecificRepository riêng
////   - Gọi Stored Procedure qua ExecuteStoredProcedureAsync hoặc FromSqlRaw
//// ============================================================

//using KLCN_API.Models.Entities;

//namespace KLCN_API.Repositories.Interfaces;

///// <summary>CRUD cơ bản dùng chung</summary>
//public interface IGenericRepository<T> where T : class
//{
//    // TODO: Task<T?> GetByIdAsync(int id)
//    // TODO: Task<List<T>> GetAllAsync()
//    // TODO: Task<T> AddAsync(T entity)
//    // TODO: Task UpdateAsync(T entity)
//    // TODO: Task DeleteAsync(int id)
//    // TODO: Task<bool> ExistsAsync(int id)
//}

//public interface IUserRepository : IGenericRepository<User>
//{
//    // TODO: Task<User?> GetByEmailAsync(string email)
//    // TODO: Task<User?> GetByPhoneAsync(string phone)
//    // TODO: Task<User?> GetWithProfileAsync(int userId)
//    // TODO: Task<(List<User> users, int total)> GetPagedAsync(string? keyword, int? roleId, int? statusId, int page, int pageSize)
//}

//public interface IRefreshTokenRepository
//{
//    // TODO: Task<RefreshToken?> GetActiveTokenAsync(int userId, string token)
//    // TODO: Task RevokeTokenAsync(string token)
//    // TODO: Task RevokeAllUserTokensAsync(int userId)
//    // TODO: Task AddAsync(RefreshToken token)
//}

//public interface IFieldRepository : IGenericRepository<Field>
//{
//    // TODO: Task<List<Field>> GetActiveFieldsAsync(int? typeId)
//    // TODO: Task<Field?> GetWithDetailsAsync(int fieldId)
//    // TODO: Task<List<FieldSlot>> GetSlotsByDateAsync(int? fieldId, DateOnly date)
//}

//public interface IFieldSlotRepository
//{
//    // TODO: Task<FieldSlot?> GetByIdAsync(int fieldSlotId)
//    // TODO: Task<List<FieldSlot>> GetByIdsAsync(List<int> ids)
//    // TODO: Task<List<FieldSlot>> GetExpiredHoldsAsync()
//    // TODO: Task ReleaseExpiredHoldsAsync()
//}

//public interface IBookingRepository : IGenericRepository<Booking>
//{
//    // TODO: Task<Booking?> GetWithDetailsAsync(int bookingId)
//    // TODO: Task<(List<Booking> items, int total)> GetPagedAsync(int? userId, int? statusId, DateOnly? from, DateOnly? to, int page, int pageSize)
//    // TODO: Task<List<Booking>> GetActiveByUserAsync(int userId)
//    // TODO: Task<int> CountActiveByUserAsync(int userId)
//}

//public interface IPaymentRepository
//{
//    // TODO: Task<List<Payment>> GetByBookingAsync(int bookingId)
//    // TODO: Task<Payment?> GetLatestPaidAsync(int bookingId)
//    // TODO: Task<decimal> GetTotalPaidAsync(int bookingId)
//    // TODO: Task<Payment> AddAsync(Payment payment)
//}

//public interface IDepositRepository
//{
//    // TODO: Task<Deposit?> GetByBookingAsync(int bookingId)
//    // TODO: Task<List<Deposit>> GetPendingOverdueAsync()
//    // TODO: Task UpdateAsync(Deposit deposit)
//}

//public interface IPromotionRepository : IGenericRepository<Promotion>
//{
//    // TODO: Task<Promotion?> GetActiveByCodeAsync(string code)
//    // TODO: Task IncrementUsageAsync(int promotionId)
//}

//public interface IProductRepository : IGenericRepository<Product>
//{
//    // TODO: Task<List<Product>> GetLowStockAsync()
//    // TODO: Task UpdateStockAsync(int productId, int quantityDelta)
//}

//public interface IIncidentRepository : IGenericRepository<Incident>
//{
//    // TODO: Task<(List<Incident> items, int total)> GetPagedAsync(int? fieldId, int? statusId, int page, int pageSize)
//}

//public interface IReviewRepository : IGenericRepository<Review>
//{
//    // TODO: Task<Review?> GetByBookingAsync(int bookingId)
//    // TODO: Task<(List<Review> items, int total)> GetByFieldAsync(int fieldId, int page, int pageSize)
//}

//public interface INotificationRepository
//{
//    // TODO: Task<(List<Notification> items, int total)> GetByUserAsync(int userId, bool? isRead, int page, int pageSize)
//    // TODO: Task MarkAsReadAsync(int notificationId)
//    // TODO: Task MarkAllAsReadAsync(int userId)
//    // TODO: Task AddAsync(Notification notification)
//}

//public interface IDashboardRepository
//{
//    // TODO: Dùng Views từ DB (vw_DashboardSummary, vw_RevenueByMonth, ...)
//    //       hoặc raw SQL / Stored Procedure để lấy dữ liệu báo cáo nhanh
//    // TODO: Task<DashboardRawData> GetSummaryAsync()
//    // TODO: Task<List<RevenueRaw>> GetRevenueByMonthAsync(int year)
//    // TODO: Task<List<OccupancyRaw>> GetOccupancyAsync(int year, int month)
//}

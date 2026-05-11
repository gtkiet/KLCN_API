//using KLCN_API.Middleware;
//using KLCN_API.Models.DTOs.Request;
//using KLCN_API.Models.DTOs.Response;
//using KLCN_API.Models.Entities;
//using KLCN_API.Repositories.Interfaces;
//using KLCN_API.Services.Interfaces;

//namespace KLCN_API.Services;

//// ================================================================
//// NotificationService
//// ================================================================

//public class NotificationService : INotificationService
//{
//    private readonly INotificationRepository _notifRepo;

//    public NotificationService(INotificationRepository notifRepo) => _notifRepo = notifRepo;

//    public async Task<PagedResponse<NotificationResponse>> GetByUserAsync(
//        int userId, GetNotificationsRequest request)
//    {
//        var (items, total) = await _notifRepo.GetByUserAsync(
//            userId, request.IsRead, request.Page, request.PageSize);

//        return new PagedResponse<NotificationResponse>
//        {
//            Items = items.Select(Map).ToList(),
//            TotalCount = total,
//            Page = request.Page,
//            PageSize = request.PageSize
//        };
//    }

//    public async Task<int> CountUnreadAsync(int userId)
//        => await _notifRepo.CountUnreadAsync(userId);

//    public async Task MarkAsReadAsync(int userId, int notificationId)
//        => await _notifRepo.MarkAsReadAsync(notificationId, userId);

//    public async Task MarkAllAsReadAsync(int userId)
//        => await _notifRepo.MarkAllAsReadAsync(userId);

//    public async Task SendAsync(int userId, string title, string body, string type, int? refId = null)
//    {
//        var notification = new Notification
//        {
//            UserId = userId,
//            Title = title,
//            Body = body,
//            Type = type,
//            RefId = refId,
//            IsRead = false,
//            CreatedAt = DateTime.UtcNow
//        };

//        await _notifRepo.AddAsync(notification);
//    }

//    private static NotificationResponse Map(Notification n) => new()
//    {
//        NotificationId = n.NotificationId,
//        Title = n.Title,
//        Body = n.Body,
//        Type = n.Type,
//        RefId = n.RefId,
//        IsRead = n.IsRead,
//        CreatedAt = n.CreatedAt
//    };
//}

//// ================================================================
//// DashboardService
//// ================================================================

//public class DashboardService : IDashboardService
//{
//    private readonly IDashboardRepository _dashRepo;

//    public DashboardService(IDashboardRepository dashRepo) => _dashRepo = dashRepo;

//    public async Task<DashboardSummaryResponse> GetSummaryAsync()
//    {
//        var raw = await _dashRepo.GetSummaryAsync();
//        return new DashboardSummaryResponse
//        {
//            PendingBookings = raw.PendingBookings,
//            PendingDepositBookings = raw.PendingDepositBookings,
//            TodayConfirmed = raw.TodayConfirmed,
//            ActiveFields = raw.ActiveFields,
//            MaintenanceFields = raw.MaintenanceFields,
//            NewIncidents = raw.NewIncidents,
//            TodayRevenue = raw.TodayRevenue,
//            ActiveCustomers = raw.ActiveCustomers,
//            LowStockCount = raw.LowStockCount,
//            UrgentDepositCount = raw.UrgentDepositCount
//        };
//    }

//    public async Task<List<RevenueByMonthResponse>> GetRevenueByMonthAsync(int year)
//    {
//        var rows = await _dashRepo.GetRevenueByMonthAsync(year);
//        return rows.Select(r => new RevenueByMonthResponse
//        {
//            Year = r.Year,
//            Month = r.Month,
//            TotalBookings = r.TotalBookings,
//            TotalRevenue = r.TotalRevenue,
//            AvgBookingValue = r.AvgBookingValue
//        }).ToList();
//    }

//    public async Task<List<FieldOccupancyResponse>> GetOccupancyAsync(int? year, int? month)
//    {
//        var rows = await _dashRepo.GetOccupancyAsync(year, month);
//        return rows.Select(r => new FieldOccupancyResponse
//        {
//            FieldId = r.FieldId,
//            FieldName = r.FieldName,
//            FieldType = r.FieldType,
//            Year = r.Year,
//            Month = r.Month,
//            TotalSlots = r.TotalSlots,
//            BookedSlots = r.BookedSlots,
//            OccupancyRate = r.OccupancyRate
//        }).ToList();
//    }

//    public async Task<List<RevenueByServiceResponse>> GetRevenueByServiceAsync()
//    {
//        var rows = await _dashRepo.GetRevenueByServiceAsync();
//        return rows.Select(r => new RevenueByServiceResponse
//        {
//            ServiceId = r.ServiceId,
//            ServiceName = r.ServiceName,
//            TotalQuantitySold = r.TotalQuantitySold,
//            TotalRevenue = r.TotalRevenue,
//            TotalBookings = r.TotalBookings
//        }).ToList();
//    }
//}

//// ================================================================
//// SystemConfigService
//// ================================================================

//public class SystemConfigService : ISystemConfigService
//{
//    private readonly ISystemConfigRepository _configRepo;

//    public SystemConfigService(ISystemConfigRepository configRepo) => _configRepo = configRepo;

//    public async Task<List<SystemConfigResponse>> GetAllAsync()
//    {
//        var items = await _configRepo.GetAllAsync();
//        return items.Select(Map).ToList();
//    }

//    public async Task<SystemConfigResponse> GetByKeyAsync(string key)
//    {
//        var config = await _configRepo.GetByKeyAsync(key)
//            ?? throw new NotFoundException($"Cấu hình '{key}' không tồn tại.");
//        return Map(config);
//    }

//    public async Task UpdateAsync(string key, UpdateSystemConfigRequest request, int updatedBy)
//    {
//        await _configRepo.GetByKeyAsync(key)
//            ?? throw new NotFoundException($"Cấu hình '{key}' không tồn tại.");

//        await _configRepo.UpdateAsync(key, request.ConfigValue.Trim(), updatedBy);
//    }

//    private static SystemConfigResponse Map(SystemConfig c) => new()
//    {
//        ConfigKey = c.ConfigKey,
//        ConfigValue = c.ConfigValue,
//        DataType = c.DataType,
//        Description = c.Description,
//        UpdatedAt = c.UpdatedAt
//    };
//}

//// ================================================================
//// SpecialDayService
//// ================================================================

//public class SpecialDayService : ISpecialDayService
//{
//    private readonly ISpecialDayRepository _specialDayRepo;

//    public SpecialDayService(ISpecialDayRepository specialDayRepo)
//        => _specialDayRepo = specialDayRepo;

//    public async Task<List<SpecialDayResponse>> GetAllAsync()
//    {
//        var items = await _specialDayRepo.GetAllAsync();
//        return items.Select(Map).ToList();
//    }

//    public async Task<SpecialDayResponse> GetByIdAsync(int specialDayId)
//    {
//        var sd = await _specialDayRepo.GetByIdAsync(specialDayId)
//            ?? throw new NotFoundException("Ngày đặc biệt", specialDayId);
//        return Map(sd);
//    }

//    public async Task<SpecialDayResponse> CreateAsync(CreateSpecialDayRequest request, int createdBy)
//    {
//        if (await _specialDayRepo.GetByDateAsync(request.SpecialDate) is not null)
//            throw new BusinessException($"Ngày {request.SpecialDate:dd/MM/yyyy} đã được cấu hình.", 400);

//        var sd = new SpecialDay
//        {
//            SpecialDate = request.SpecialDate,
//            Name = request.Name.Trim(),
//            PriceMultiplier = request.PriceMultiplier,
//            IsFullDayPeak = request.IsFullDayPeak,
//            Note = request.Note?.Trim(),
//            CreatedBy = createdBy,
//            CreatedAt = DateTime.UtcNow
//        };

//        var created = await _specialDayRepo.CreateAsync(sd);
//        return Map(created);
//    }

//    public async Task<SpecialDayResponse> UpdateAsync(int specialDayId, UpdateSpecialDayRequest request)
//    {
//        var sd = await _specialDayRepo.GetByIdAsync(specialDayId)
//            ?? throw new NotFoundException("Ngày đặc biệt", specialDayId);

//        if (request.Name is not null) sd.Name = request.Name.Trim();
//        if (request.PriceMultiplier.HasValue) sd.PriceMultiplier = request.PriceMultiplier.Value;
//        if (request.IsFullDayPeak.HasValue) sd.IsFullDayPeak = request.IsFullDayPeak.Value;
//        if (request.Note is not null) sd.Note = request.Note.Trim();

//        await _specialDayRepo.UpdateAsync(sd);
//        return Map(sd);
//    }

//    public async Task DeleteAsync(int specialDayId)
//    {
//        await _specialDayRepo.GetByIdAsync(specialDayId)
//            ?? throw new NotFoundException("Ngày đặc biệt", specialDayId);

//        await _specialDayRepo.DeleteAsync(specialDayId);
//    }

//    private static SpecialDayResponse Map(SpecialDay sd) => new()
//    {
//        SpecialDayId = sd.SpecialDayId,
//        SpecialDate = sd.SpecialDate,
//        Name = sd.Name,
//        PriceMultiplier = sd.PriceMultiplier,
//        IsFullDayPeak = sd.IsFullDayPeak,
//        Note = sd.Note,
//        CreatedBy = sd.CreatedByUser?.FullName ?? string.Empty,
//        CreatedAt = sd.CreatedAt
//    };
//}
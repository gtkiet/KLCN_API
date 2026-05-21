using KLCN_API.Models.DTOs.Response;
using KLCN_API.Repositories.Interfaces;
using KLCN_API.Services.Interfaces;

namespace KLCN_API.Services;

public class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dashRepo;

    public DashboardService(IDashboardRepository dashRepo) => _dashRepo = dashRepo;

    public async Task<DashboardSummaryResponse> GetSummaryAsync()
    {
        var raw = await _dashRepo.GetSummaryAsync();

        // Raw và Response có cùng shape — map thủ công để tường minh
        return new DashboardSummaryResponse
        {
            PendingBookings = raw.PendingBookings,
            PendingDepositBookings = raw.PendingDepositBookings,
            TodayConfirmed = raw.TodayConfirmed,
            ActiveFields = raw.ActiveFields,
            MaintenanceFields = raw.MaintenanceFields,
            NewIncidents = raw.NewIncidents,
            TodayRevenue = raw.TodayRevenue,
            ActiveCustomers = raw.ActiveCustomers,
            LowStockCount = raw.LowStockCount,
            UrgentDepositCount = raw.UrgentDepositCount
        };
    }

    public async Task<List<RevenueByMonthResponse>> GetRevenueByMonthAsync(int year)
    {
        var raws = await _dashRepo.GetRevenueByMonthAsync(year);

        return raws.Select(r => new RevenueByMonthResponse
        {
            Year = r.Year,
            Month = r.Month,
            TotalBookings = r.TotalBookings,
            TotalRevenue = r.TotalRevenue,
            AvgBookingValue = r.AvgBookingValue
        }).ToList();
    }

    public async Task<List<FieldOccupancyResponse>> GetOccupancyAsync(int? year, int? month)
    {
        var raws = await _dashRepo.GetOccupancyAsync(year, month);

        return raws.Select(r => new FieldOccupancyResponse
        {
            FieldId = r.FieldId,
            FieldName = r.FieldName,
            FieldType = r.FieldType,
            Year = r.Year,
            Month = r.Month,
            TotalSlots = r.TotalSlots,
            BookedSlots = r.BookedSlots,
            OccupancyRate = r.OccupancyRate
        }).ToList();
    }

    public async Task<List<RevenueByServiceResponse>> GetRevenueByServiceAsync()
    {
        var raws = await _dashRepo.GetRevenueByServiceAsync();

        return raws.Select(r => new RevenueByServiceResponse
        {
            ServiceId = r.ServiceId,
            ServiceName = r.ServiceName,
            TotalQuantitySold = r.TotalQuantitySold,
            TotalRevenue = r.TotalRevenue,
            TotalBookings = r.TotalBookings
        }).ToList();
    }
}

using KLCN_API.Data;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Repositories.Interfaces;
using KLCN_API.Services.Interfaces;
using KLCN_API.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace KLCN_API.Services;

public class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dashRepo;
    private readonly SportPlusDbContext _ctx;

    public DashboardService(IDashboardRepository dashRepo, SportPlusDbContext ctx)
    {
        _dashRepo = dashRepo;
        _ctx = ctx;
    }

    public async Task<DashboardSummaryResponse> GetSummaryAsync()
    {
        var raw = await _dashRepo.GetSummaryAsync();

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

    public async Task<MonthlyReportResponse> GetMonthlyReportAsync(int year, int month)
    {
        var from = new DateTime(year, month, 1);
        var to = from.AddMonths(1);

        var paidPayments = await _ctx.Payments
            .AsNoTracking()
            .Include(x => x.Method)
            .Where(x => x.PaidAt.HasValue
                        && x.PaidAt.Value >= from
                        && x.PaidAt.Value < to
                        && x.StatusId == 2)
            .ToListAsync();

        var bookings = await _ctx.Bookings
            .AsNoTracking()
            .Where(x => x.CreatedAt >= from && x.CreatedAt < to)
            .ToListAsync();

        var totalRevenue = paidPayments.Sum(x => x.Amount);

        var cashRevenue = paidPayments
            .Where(x => x.Method != null &&
                        (x.Method.Name.Contains("tiền mặt") || x.Method.Name.Contains("trực tiếp")))
            .Sum(x => x.Amount);

        var vnPayRevenue = totalRevenue - cashRevenue;

        var completedBookings = bookings.Count(x => x.StatusId == (int)BookingStatusEnum.Completed);
        var cancelledBookings = bookings.Count(x => x.StatusId == (int)BookingStatusEnum.Cancelled);

        return new MonthlyReportResponse
        {
            Year = year,
            Month = month,
            TotalRevenue = totalRevenue,
            CashRevenue = cashRevenue,
            VnPayRevenue = vnPayRevenue,
            TotalBookings = completedBookings + cancelledBookings,
            CompletedBookings = completedBookings,
            CancelledBookings = cancelledBookings
        };
    }
}
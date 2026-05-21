using KLCN_API.Data;
using KLCN_API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KLCN_API.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly SportPlusDbContext _ctx;

    public DashboardRepository(SportPlusDbContext ctx) => _ctx = ctx;

    public async Task<DashboardRaw> GetSummaryAsync()
    {
        // vw_DashboardSummary trả về đúng 1 dòng — scalar aggregates không cần WHERE
        var row = await _ctx.Database
            .SqlQueryRaw<DashboardRaw>("SELECT * FROM vw_DashboardSummary")
            .FirstOrDefaultAsync();

        // View luôn trả về 1 dòng (scalar subqueries không NULL)
        return row ?? new DashboardRaw();
    }

    public async Task<List<RevenueByMonthRaw>> GetRevenueByMonthAsync(int year)
        => await _ctx.Database
            .SqlQueryRaw<RevenueByMonthRaw>(
                "SELECT * FROM vw_RevenueByMonth WHERE [Year] = {0}", year)
            .OrderBy(r => r.Month)
            .ToListAsync();

    public async Task<List<FieldOccupancyRaw>> GetOccupancyAsync(int? year, int? month)
    {
        var sql = """
        SELECT *
        FROM vw_FieldOccupancyByMonth
        WHERE 1=1
        """;

        var parameters = new List<object>();

        if (year.HasValue)
        {
            sql += " AND [Year] = @p0";
            parameters.Add(year.Value);
        }

        if (month.HasValue)
        {
            sql += year.HasValue
                ? " AND [Month] = @p1"
                : " AND [Month] = @p0";

            parameters.Add(month.Value);
        }

        return await _ctx.Database
            .SqlQueryRaw<FieldOccupancyRaw>(sql, parameters.ToArray())
            .OrderBy(r => r.FieldId)
            .ThenBy(r => r.Year)
            .ThenBy(r => r.Month)
            .ToListAsync();
    }

    public async Task<List<RevenueByServiceRaw>> GetRevenueByServiceAsync()
        => await _ctx.Database
            .SqlQueryRaw<RevenueByServiceRaw>("SELECT * FROM vw_RevenueByService")
            .OrderByDescending(r => r.TotalRevenue)
            .ToListAsync();
}

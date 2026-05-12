using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using KLCN_API.Data;

namespace KLCN_API.Jobs;

// ── Job 1: Giải phóng slot hết hạn (mỗi 1 phút) ────────────────

/// <summary>
/// Chạy sp_ReleaseExpiredSlots mỗi 1 phút.
/// Giải phóng slot hold hết hạn, tự hủy deposit quá deadline,
/// tự hoàn thành booking đã qua ngày thi đấu.
/// </summary>
public class ReleaseExpiredSlotsJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReleaseExpiredSlotsJob> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    public ReleaseExpiredSlotsJob(
        IServiceScopeFactory scopeFactory,
        ILogger<ReleaseExpiredSlotsJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReleaseExpiredSlotsJob started.");

        // Chờ 10 giây sau khi app khởi động trước khi chạy lần đầu
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<SportPlusDbContext>();

                // ExecuteSqlRawAsync không có overload nhận CancellationToken —
                // cancellation được xử lý bởi vòng lặp và Task.Delay phía dưới.
                await ctx.Database.ExecuteSqlRawAsync("EXEC sp_ReleaseExpiredSlots");

                _logger.LogDebug("sp_ReleaseExpiredSlots executed at {Time}", DateTime.UtcNow);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error in ReleaseExpiredSlotsJob");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in ReleaseExpiredSlotsJob");
            }

            await Task.Delay(Interval, stoppingToken);
        }

        _logger.LogInformation("ReleaseExpiredSlotsJob stopped.");
    }
}

// ── Job 2: Sinh slot hàng ngày lúc 00:01 ────────────────────────

/// <summary>
/// Chạy lúc 00:01 mỗi ngày (giờ VN), gọi sp_GenerateSlots cho ngày thứ 30 tới.
/// Đảm bảo luôn có đủ slot trong rolling window 30 ngày.
/// </summary>
public class GenerateDailySlotsJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GenerateDailySlotsJob> _logger;
    private const int DaysAhead = 29;

    public GenerateDailySlotsJob(
        IServiceScopeFactory scopeFactory,
        ILogger<GenerateDailySlotsJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GenerateDailySlotsJob started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRun();
            _logger.LogDebug(
                "GenerateDailySlotsJob next run in {Minutes:F1} minutes",
                delay.TotalMinutes);

            await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<SportPlusDbContext>();

                var targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(DaysAhead));

                // sp_GenerateSlots nhận DATE — truyền DateTime với time = 00:00:00,
                // SQL Server tự cast về DATE.
                await ctx.Database.ExecuteSqlRawAsync(
                    "EXEC sp_GenerateSlots @StartDate, @EndDate",
                    new SqlParameter("@StartDate", targetDate.ToDateTime(TimeOnly.MinValue)),
                    new SqlParameter("@EndDate", targetDate.ToDateTime(TimeOnly.MinValue)));

                _logger.LogInformation(
                    "GenerateDailySlotsJob: sinh slot cho ngay {Date}", targetDate);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error in GenerateDailySlotsJob");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GenerateDailySlotsJob");
            }
        }

        _logger.LogInformation("GenerateDailySlotsJob stopped.");
    }

    /// <summary>
    /// Tính thời gian chờ đến 00:01 ngày hôm sau theo giờ VN (UTC+7).
    /// Fallback về TimeSpan cố định nếu không tìm được timezone.
    /// </summary>
    private TimeSpan GetDelayUntilNextRun()
    {
        try
        {
            var tzId = OperatingSystem.IsWindows()
                ? "SE Asia Standard Time"
                : "Asia/Ho_Chi_Minh";

            var vnZone = TimeZoneInfo.FindSystemTimeZoneById(tzId);
            var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnZone);
            var nextRun = nowVn.Date.AddDays(1).AddMinutes(1); // 00:01 ngày mai
            var delay = nextRun - nowVn;

            return delay > TimeSpan.Zero ? delay : TimeSpan.FromHours(24);
        }
        catch (Exception ex)
        {
            // Không tìm được timezone (môi trường Docker stripped) —
            // fallback: tính theo UTC+7 offset thủ công.
            _logger.LogWarning(ex,
                "Khong tim duoc timezone VN, fallback ve UTC+7 offset thu cong.");

            var nowVn = DateTime.UtcNow.AddHours(7);
            var nextRun = nowVn.Date.AddDays(1).AddMinutes(1);
            var delay = nextRun - nowVn;

            return delay > TimeSpan.Zero ? delay : TimeSpan.FromHours(24);
        }
    }
}
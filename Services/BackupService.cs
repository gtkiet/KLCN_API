using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using KLCN_API.Data;
using KLCN_API.Middleware;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Entities;
using KLCN_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KLCN_API.Services;

/// <summary>
/// Backup / Restore dữ liệu bằng JSON export/import.
///
/// Hoạt động trên shared hosting — không cần quyền BACKUP DATABASE.
///
/// Cấu trúc file .zip:
///   SportPlus_Backup_20260521_143000.zip
///   ├── manifest.json          (version, timestamp, danh sách bảng)
///   ├── Users.json
///   ├── Profiles.json
///   ├── ... (xem BackupService.TableOrder)
///   └── Notifications.json
///
/// Bảng KHÔNG backup (lookup cố định):
///   Roles, UserStatuses, FieldTypes, FieldStatuses, FieldSlotStatuses,
///   BookingStatuses, PaymentStatuses, PaymentMethods, DepositStatuses,
///   IncidentStatuses, PurchaseOrderStatuses, PromotionTypes, SystemConfig
/// </summary>
public class BackupService : IBackupService
{
    private const string BackupVersion = "1.0";
    private const string SnapshotFolder = "Backups";
    private const int MaxSnapshots = 30;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new DateOnlyJsonConverter(), new TimeOnlyJsonConverter() }
    };

    // Thứ tự restore theo FK (parent trước, child sau)
    private static readonly string[] TableOrder =
    [
        "Users", "Profiles", "RefreshTokens",
        "Fields", "FieldPriceHistory", "TimeSlots", "FieldSlots", "FieldMaintenanceLogs",
        "SpecialDays", "PeakSchedules",
        "Services", "Promotions",
        "Suppliers", "Products", "PurchaseOrders", "PurchaseOrderDetails",
        "Bookings", "BookingDetails", "BookingServices", "BookingLogs",
        "Payments", "Deposits",
        "Incidents", "Reviews", "Notifications"
    ];

    private readonly SportPlusDbContext _ctx;
    private readonly IWebHostEnvironment _env;

    public BackupService(SportPlusDbContext ctx, IWebHostEnvironment env)
    {
        _ctx = ctx;
        _env = env;
    }

    // ── Export ────────────────────────────────────────────────────

    public async Task<(byte[] ZipBytes, string FileName)> ExportAsync()
    {
        var fileName = $"SportPlus_Backup_{DateTime.UtcNow.AddHours(7):yyyyMMdd_HHmmss}.zip";
        var zipBytes = await BuildZipAsync();
        return (zipBytes, fileName);
    }

    // ── Snapshot ──────────────────────────────────────────────────

    public async Task<BackupSnapshotInfo> CreateSnapshotAsync()
    {
        var dir = GetSnapshotDir();
        Directory.CreateDirectory(dir);

        // Xóa snapshot cũ nhất nếu vượt giới hạn
        var existing = Directory.GetFiles(dir, "*.zip")
                                .OrderBy(f => f).ToList();
        while (existing.Count >= MaxSnapshots)
        {
            File.Delete(existing[0]);
            existing.RemoveAt(0);
        }

        var fileName = $"SportPlus_Backup_{DateTime.UtcNow.AddHours(7):yyyyMMdd_HHmmss}.zip";
        var fullPath = Path.Combine(dir, fileName);

        var zipBytes = await BuildZipAsync();
        await File.WriteAllBytesAsync(fullPath, zipBytes);

        var fi = new FileInfo(fullPath);
        return new BackupSnapshotInfo
        {
            FileName = fi.Name,
            SizeBytes = fi.Length,
            CreatedAt = fi.CreationTimeUtc
        };
    }

    public Task<List<BackupSnapshotInfo>> ListSnapshotsAsync()
    {
        var dir = GetSnapshotDir();
        if (!Directory.Exists(dir))
            return Task.FromResult(new List<BackupSnapshotInfo>());

        var list = Directory.GetFiles(dir, "*.zip")
            .Select(f => new FileInfo(f))
            .OrderByDescending(fi => fi.CreationTimeUtc)
            .Select(fi => new BackupSnapshotInfo
            {
                FileName = fi.Name,
                SizeBytes = fi.Length,
                CreatedAt = fi.CreationTimeUtc
            })
            .ToList();

        return Task.FromResult(list);
    }

    public async Task<byte[]> DownloadSnapshotAsync(string fileName)
    {
        var path = GetSnapshotPath(fileName);
        if (!File.Exists(path))
            throw new NotFoundException($"Snapshot '{fileName}' không tồn tại.");

        return await File.ReadAllBytesAsync(path);
    }

    public Task DeleteSnapshotAsync(string fileName)
    {
        var path = GetSnapshotPath(fileName);
        if (!File.Exists(path))
            throw new NotFoundException($"Snapshot '{fileName}' không tồn tại.");

        File.Delete(path);
        return Task.CompletedTask;
    }

    // ── Restore ───────────────────────────────────────────────────

    public async Task<RestoreReportResponse> RestoreAsync(Stream zipStream, int adminUserId)
    {
        var sw = Stopwatch.StartNew();

        // 1. Tự động snapshot trước khi restore
        var preSnapshot = await CreateSnapshotAsync();

        // 2. Đọc toàn bộ zip vào memory
        using var ms = new MemoryStream();
        await zipStream.CopyToAsync(ms);
        ms.Position = 0;

        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

        // 3. Kiểm tra manifest
        var manifestEntry = archive.GetEntry("manifest.json")
            ?? throw new BusinessException("File backup không hợp lệ (thiếu manifest.json).", 400);

        await using var manifestStream = manifestEntry.Open();
        var manifest = await JsonSerializer.DeserializeAsync<BackupManifest>(manifestStream, JsonOpts)
            ?? throw new BusinessException("Không đọc được manifest.json.", 400);

        if (manifest.Version != BackupVersion)
            throw new BusinessException(
                $"Phiên bản backup không tương thích " +
                $"(file: {manifest.Version}, hệ thống: {BackupVersion}).", 400);

        // 4. Đọc dữ liệu từ zip trước khi mở transaction
        var data = await ReadAllEntriesAsync(archive);

        // 5. Restore trong transaction
        var report = new RestoreReportResponse
        {
            PreRestoreSnapshot = preSnapshot.FileName
        };

        await using var tx = await _ctx.Database.BeginTransactionAsync();
        try
        {
            await DeleteAllTablesAsync();
            await _ctx.SaveChangesAsync();

            // Tắt IDENTITY_INSERT theo từng bảng, insert, bật lại
            report.RestoredRows["Users"] = await InsertWithIdentityAsync("Users", data, "Users.json", _ctx.Users);
            report.RestoredRows["Profiles"] = await InsertWithIdentityAsync("Profiles", data, "Profiles.json", _ctx.Profiles);
            report.RestoredRows["RefreshTokens"] = await InsertWithIdentityAsync("RefreshTokens", data, "RefreshTokens.json", _ctx.RefreshTokens);
            report.RestoredRows["Fields"] = await InsertWithIdentityAsync("Fields", data, "Fields.json", _ctx.Fields);
            report.RestoredRows["FieldPriceHistory"] = await InsertWithIdentityAsync("FieldPriceHistory", data, "FieldPriceHistory.json", _ctx.FieldPriceHistories);
            report.RestoredRows["TimeSlots"] = await InsertWithIdentityAsync("TimeSlots", data, "TimeSlots.json", _ctx.TimeSlots);
            report.RestoredRows["FieldSlots"] = await InsertWithIdentityAsync("FieldSlots", data, "FieldSlots.json", _ctx.FieldSlots);
            report.RestoredRows["FieldMaintenanceLogs"] = await InsertWithIdentityAsync("FieldMaintenanceLogs", data, "FieldMaintenanceLogs.json", _ctx.FieldMaintenanceLogs);
            report.RestoredRows["SpecialDays"] = await InsertWithIdentityAsync("SpecialDays", data, "SpecialDays.json", _ctx.SpecialDays);
            report.RestoredRows["PeakSchedules"] = await InsertWithIdentityAsync("PeakSchedules", data, "PeakSchedules.json", _ctx.PeakSchedules);
            report.RestoredRows["Services"] = await InsertWithIdentityAsync("Services", data, "Services.json", _ctx.Services);
            report.RestoredRows["Promotions"] = await InsertWithIdentityAsync("Promotions", data, "Promotions.json", _ctx.Promotions);
            report.RestoredRows["Suppliers"] = await InsertWithIdentityAsync("Suppliers", data, "Suppliers.json", _ctx.Suppliers);
            report.RestoredRows["Products"] = await InsertWithIdentityAsync("Products", data, "Products.json", _ctx.Products);
            report.RestoredRows["PurchaseOrders"] = await InsertWithIdentityAsync("PurchaseOrders", data, "PurchaseOrders.json", _ctx.PurchaseOrders);
            report.RestoredRows["PurchaseOrderDetails"] = await InsertWithIdentityAsync("PurchaseOrderDetails", data, "PurchaseOrderDetails.json", _ctx.PurchaseOrderDetails);
            report.RestoredRows["Bookings"] = await InsertWithIdentityAsync("Bookings", data, "Bookings.json", _ctx.Bookings);
            report.RestoredRows["BookingDetails"] = await InsertWithIdentityAsync("BookingDetails", data, "BookingDetails.json", _ctx.BookingDetails);
            report.RestoredRows["BookingServices"] = await InsertWithIdentityAsync("BookingServices", data, "BookingServices.json", _ctx.BookingServices);
            report.RestoredRows["BookingLogs"] = await InsertWithIdentityAsync("BookingLogs", data, "BookingLogs.json", _ctx.BookingLogs);
            report.RestoredRows["Payments"] = await InsertWithIdentityAsync("Payments", data, "Payments.json", _ctx.Payments);
            report.RestoredRows["Deposits"] = await InsertWithIdentityAsync("Deposits", data, "Deposits.json", _ctx.Deposits);
            report.RestoredRows["Incidents"] = await InsertWithIdentityAsync("Incidents", data, "Incidents.json", _ctx.Incidents);
            report.RestoredRows["Reviews"] = await InsertWithIdentityAsync("Reviews", data, "Reviews.json", _ctx.Reviews);
            report.RestoredRows["Notifications"] = await InsertWithIdentityAsync("Notifications", data, "Notifications.json", _ctx.Notifications);

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        sw.Stop();
        report.ElapsedMs = sw.ElapsedMilliseconds;
        return report;
    }

    // ── Build ZIP ─────────────────────────────────────────────────

    private async Task<byte[]> BuildZipAsync()
    {
        using var ms = new MemoryStream();
        using var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true);

        var manifest = new BackupManifest
        {
            Version = BackupVersion,
            CreatedAt = DateTime.UtcNow,
            Tables = TableOrder.ToList()
        };
        await WriteEntryAsync(archive, "manifest.json",
            JsonSerializer.Serialize(manifest, JsonOpts));

        // Serialize từng bảng — AsNoTracking để không load navigation
        await WriteEntryAsync(archive, "Users.json",
            Serialize(await _ctx.Users.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "Profiles.json",
            Serialize(await _ctx.Profiles.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "RefreshTokens.json",
            Serialize(await _ctx.RefreshTokens.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "Fields.json",
            Serialize(await _ctx.Fields.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "FieldPriceHistory.json",
            Serialize(await _ctx.FieldPriceHistories.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "TimeSlots.json",
            Serialize(await _ctx.TimeSlots.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "FieldSlots.json",
            Serialize(await _ctx.FieldSlots.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "FieldMaintenanceLogs.json",
            Serialize(await _ctx.FieldMaintenanceLogs.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "SpecialDays.json",
            Serialize(await _ctx.SpecialDays.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "PeakSchedules.json",
            Serialize(await _ctx.PeakSchedules.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "Services.json",
            Serialize(await _ctx.Services.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "Promotions.json",
            Serialize(await _ctx.Promotions.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "Suppliers.json",
            Serialize(await _ctx.Suppliers.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "Products.json",
            Serialize(await _ctx.Products.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "PurchaseOrders.json",
            Serialize(await _ctx.PurchaseOrders.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "PurchaseOrderDetails.json",
            Serialize(await _ctx.PurchaseOrderDetails.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "Bookings.json",
            Serialize(await _ctx.Bookings.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "BookingDetails.json",
            Serialize(await _ctx.BookingDetails.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "BookingServices.json",
            Serialize(await _ctx.BookingServices.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "BookingLogs.json",
            Serialize(await _ctx.BookingLogs.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "Payments.json",
            Serialize(await _ctx.Payments.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "Deposits.json",
            Serialize(await _ctx.Deposits.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "Incidents.json",
            Serialize(await _ctx.Incidents.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "Reviews.json",
            Serialize(await _ctx.Reviews.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "Notifications.json",
            Serialize(await _ctx.Notifications.AsNoTracking().ToListAsync()));

        archive.Dispose();
        return ms.ToArray();
    }

    // ── Restore helpers ───────────────────────────────────────────

    /// <summary>
    /// Xóa dữ liệu theo thứ tự ngược FK để tránh constraint violation.
    /// Dùng DELETE thay vì TRUNCATE vì các bảng có FK reference lẫn nhau.
    /// </summary>
    private async Task DeleteAllTablesAsync()
    {
        // Thứ tự: child trước, parent sau
        string[] deleteOrder =
        [
            "Notifications", "Reviews", "Incidents",
            "Deposits", "Payments",
            "BookingLogs", "BookingServices", "BookingDetails", "Bookings",
            "PurchaseOrderDetails", "PurchaseOrders",
            "Products", "Suppliers",
            "Promotions", "Services",
            "PeakSchedules", "SpecialDays",
            "FieldMaintenanceLogs", "FieldSlots", "TimeSlots",
            "FieldPriceHistory", "Fields",
            "RefreshTokens", "Profiles", "Users"
        ];

        foreach (var table in deleteOrder)
            await _ctx.Database.ExecuteSqlAsync($"DELETE FROM [{table}]");
    }

    /// <summary>
    /// Insert với IDENTITY_INSERT ON để giữ nguyên PK từ backup.
    /// Bắt buộc phải giữ PK gốc vì các bảng con dùng FK trỏ về PK đó.
    /// </summary>
    private async Task<int> InsertWithIdentityAsync<T>(
        string tableName,
        Dictionary<string, string> data,
        string entryKey,
        DbSet<T> dbSet) where T : class
    {
        if (!data.TryGetValue(entryKey, out var json) || string.IsNullOrWhiteSpace(json))
            return 0;

        var items = JsonSerializer.Deserialize<List<T>>(json, JsonOpts);
        if (items is null || items.Count == 0) return 0;

        // SET IDENTITY_INSERT ON — giữ PK gốc
        await _ctx.Database.ExecuteSqlAsync(
            $"SET IDENTITY_INSERT [{tableName}] ON");
        try
        {
            await dbSet.AddRangeAsync(items);
            await _ctx.SaveChangesAsync();
        }
        finally
        {
            // Luôn tắt lại dù có lỗi
            await _ctx.Database.ExecuteSqlAsync(
                $"SET IDENTITY_INSERT [{tableName}] OFF");
        }

        // Detach để EF không track lại khi xử lý bảng khác
        foreach (var item in items)
            _ctx.Entry(item).State = EntityState.Detached;

        return items.Count;
    }

    /// <summary>Đọc toàn bộ entry trong zip vào Dictionary trước khi xử lý.</summary>
    private static async Task<Dictionary<string, string>> ReadAllEntriesAsync(ZipArchive archive)
    {
        var data = new Dictionary<string, string>();
        foreach (var entry in archive.Entries)
        {
            await using var stream = entry.Open();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            data[entry.Name] = await reader.ReadToEndAsync();
        }
        return data;
    }

    // ── Static helpers ────────────────────────────────────────────

    private static string Serialize<T>(List<T> items)
        => JsonSerializer.Serialize(items, JsonOpts);

    private static async Task WriteEntryAsync(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        await using var stream = entry.Open();
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        await writer.WriteAsync(content);
    }

    private string GetSnapshotDir()
        => Path.Combine(_env.ContentRootPath, SnapshotFolder);

    private string GetSnapshotPath(string fileName)
    {
        // Path traversal protection — chỉ lấy tên file, bỏ directory
        var safe = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safe) || safe != fileName)
            throw new BusinessException("Tên file không hợp lệ.", 400);

        return Path.Combine(GetSnapshotDir(), safe);
    }
}

// ── Manifest ──────────────────────────────────────────────────────

internal class BackupManifest
{
    public string Version { get; set; } = "1.0";
    public DateTime CreatedAt { get; set; }
    public List<string> Tables { get; set; } = [];
}

// ── Custom JSON Converters ────────────────────────────────────────

internal class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    public override DateOnly Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
        => DateOnly.Parse(r.GetString()!);

    public override void Write(Utf8JsonWriter w, DateOnly v, JsonSerializerOptions o)
        => w.WriteStringValue(v.ToString("yyyy-MM-dd"));
}

internal class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
{
    public override TimeOnly Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
        => TimeOnly.Parse(r.GetString()!);

    public override void Write(Utf8JsonWriter w, TimeOnly v, JsonSerializerOptions o)
        => w.WriteStringValue(v.ToString("HH:mm:ss"));
}
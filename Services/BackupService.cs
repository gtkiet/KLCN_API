using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using KLCN_API.Data;
using KLCN_API.Middleware;
using KLCN_API.Models.DTOs.Response;
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
///   ├── manifest.json
///   ├── _lookup_Roles.json              (upsert khi restore, không xóa trước)
///   ├── _lookup_UserStatuses.json
///   ├── ... (tất cả lookup tables)
///   ├── _system_SystemConfig.json       (upsert khi restore)
///   ├── Users.json
///   └── Notifications.json
/// </summary>
public class BackupService : IBackupService
{
    private const string BackupVersion = "1.1";
    private const string SnapshotFolder = "Backups";
    private const int MaxSnapshots = 30;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new DateOnlyJsonConverter(), new TimeOnlyJsonConverter() }
    };

    // ── Lookup tables: có IDENTITY, upsert khi restore (không xóa trước) ──────
    // Thứ tự đủ để FK trong bảng data trỏ vào đúng ID.
    private static readonly string[] LookupTableOrder =
    [
        "Roles", "UserStatuses", "FieldTypes", "FieldStatuses", "FieldSlotStatuses",
        "BookingStatuses", "PaymentStatuses", "PaymentMethods", "DepositStatuses",
        "IncidentStatuses", "PurchaseOrderStatuses", "PromotionTypes"
    ];

    // ── Bảng data: xóa sạch rồi insert lại khi restore ───────────────────────
    // Thứ tự: parent trước, child sau.
    private static readonly string[] DataTableOrder =
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

    // Tất cả bảng được backup (lookup + data) — dùng để whitelist tên bảng.
    private static readonly HashSet<string> AllowedTables =
        new(LookupTableOrder.Concat(DataTableOrder).Append("SystemConfig"),
            StringComparer.Ordinal);

    // Whitelist cột theo từng bảng — khớp chính xác với schema SQL.
    private static readonly Dictionary<string, HashSet<string>> AllowedColumns =
        new(StringComparer.Ordinal)
        {
            // ── Lookup tables (chỉ có ID + Name) ───────────────────────────
            ["Roles"] = ["RoleId", "Name"],
            ["UserStatuses"] = ["StatusId", "Name"],
            ["FieldTypes"] = ["TypeId", "Name"],
            ["FieldStatuses"] = ["StatusId", "Name"],
            ["FieldSlotStatuses"] = ["StatusId", "Name"],
            ["BookingStatuses"] = ["StatusId", "Name"],
            ["PaymentStatuses"] = ["StatusId", "Name"],
            ["PaymentMethods"] = ["MethodId", "Name"],
            ["DepositStatuses"] = ["StatusId", "Name"],
            ["IncidentStatuses"] = ["StatusId", "Name"],
            ["PurchaseOrderStatuses"] = ["StatusId", "Name"],
            ["PromotionTypes"] = ["TypeId", "Name"],

            // ── SystemConfig (PK là string, không có IDENTITY) ──────────────
            ["SystemConfig"] = ["ConfigKey", "ConfigValue", "DataType", "Description", "UpdatedAt", "UpdatedBy"],

            // ── Data tables ─────────────────────────────────────────────────
            ["Users"] = ["UserId", "Email", "Phone", "PasswordHash", "FullName", "RoleId", "StatusId", "CreatedAt", "UpdatedAt", "IsDeleted"],
            ["Profiles"] = ["ProfileId", "UserId", "AvatarUrl", "DateOfBirth", "Address"],
            ["RefreshTokens"] = ["TokenId", "UserId", "Token", "ExpiresAt", "IsRevoked", "CreatedAt"],

            ["Fields"] = ["FieldId", "Name", "Description", "BasePrice", "PeakPrice", "ImageUrl", "TypeId", "StatusId", "IsDeleted", "CreatedAt", "UpdatedAt"],
            ["FieldPriceHistory"] = ["HistoryId", "FieldId", "OldBasePrice", "OldPeakPrice", "NewBasePrice", "NewPeakPrice", "ChangedBy", "ChangedAt", "Reason"],
            ["TimeSlots"] = ["SlotId", "StartTime", "EndTime", "IsPeakHour"],
            ["FieldSlots"] = ["FieldSlotId", "FieldId", "SlotId", "SlotDate", "Price", "StatusId", "HoldExpireAt", "UpdatedAt"],
            ["FieldMaintenanceLogs"] = ["LogId", "FieldId", "Reason", "StartDate", "EndDate", "CreatedBy", "CreatedAt"],

            ["SpecialDays"] = ["SpecialDayId", "SpecialDate", "Name", "PriceMultiplier", "IsFullDayPeak", "Note", "CreatedBy", "CreatedAt"],
            ["PeakSchedules"] = ["PeakScheduleId", "DayOfWeek", "SlotId", "IsPeak"],

            ["Services"] = ["ServiceId", "Name", "Description", "Price", "ImageUrl", "IsAvailable", "IsDeleted", "UpdatedAt"],
            ["Promotions"] = ["PromotionId", "Code", "Name", "Description", "TypeId", "DiscountValue", "MaxDiscount", "MinOrderAmount", "UsageLimit", "UsageCount", "StartDate", "EndDate", "IsActive", "CreatedBy", "CreatedAt"],

            ["Suppliers"] = ["SupplierId", "Name", "ContactName", "Phone", "Email", "Address", "IsDeleted"],
            ["Products"] = ["ProductId", "Name", "Unit", "StockQty", "MinQty", "IsDeleted"],
            ["PurchaseOrders"] = ["PurchaseOrderId", "SupplierId", "CreatedByUserId", "StatusId", "TotalAmount", "Note", "ConfirmedAt", "CreatedAt"],
            ["PurchaseOrderDetails"] = ["PurchaseOrderDetailId", "PurchaseOrderId", "ProductId", "Quantity", "UnitPrice"],

            ["Bookings"] = ["BookingId", "UserId", "PromotionId", "StatusId", "SubTotal", "DiscountAmount", "TaxAmount", "TotalAmount", "DepositAmount", "Note", "CancelReason", "RescheduleCount", "CreatedAt", "UpdatedAt"],
            ["BookingDetails"] = ["BookingDetailId", "BookingId", "FieldSlotId", "Price"],
            ["BookingServices"] = ["BookingServiceId", "BookingId", "ServiceId", "Quantity", "UnitPrice"],
            ["BookingLogs"] = ["LogId", "BookingId", "OldStatusId", "NewStatusId", "ChangedByUserId", "Note", "ChangedAt"],

            ["Payments"] = ["PaymentId", "BookingId", "Amount", "MethodId", "StatusId", "TransactionCode", "GatewayResponse", "Note", "PaidAt", "CreatedAt"],
            ["Deposits"] = ["DepositId", "BookingId", "RequiredAmount", "PaidAmount", "StatusId", "DeadlineAt", "PaidAt", "RefundedAt", "ForfeitedAt", "PaymentId", "Note", "CreatedAt", "UpdatedAt"],

            ["Incidents"] = ["IncidentId", "FieldId", "ReportedByUserId", "Title", "Description", "ImageUrl", "StatusId", "HandledByUserId", "HandledAt", "HandledNote", "CreatedAt"],
            ["Reviews"] = ["ReviewId", "BookingId", "FieldId", "UserId", "Rating", "Comment", "ImageUrl", "IsVisible", "CreatedAt", "UpdatedAt"],
            ["Notifications"] = ["NotificationId", "UserId", "Title", "Body", "Type", "RefId", "IsRead", "CreatedAt"],
        };

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

        var existing = Directory.GetFiles(dir, "*.zip").OrderBy(f => f).ToList();
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

    public async Task<RestoreReportResponse> RestoreFromSnapshotAsync(string fileName, int adminUserId)
    {
        var path = GetSnapshotPath(fileName);
        if (!File.Exists(path))
            throw new NotFoundException($"Snapshot '{fileName}' không tồn tại.");

        await using var stream = File.OpenRead(path);
        return await RestoreAsync(stream, adminUserId);
    }

    public async Task<RestoreReportResponse> RestoreAsync(Stream zipStream, int adminUserId)
    {
        var sw = Stopwatch.StartNew();

        var preSnapshot = await CreateSnapshotAsync();

        using var ms = new MemoryStream();
        await zipStream.CopyToAsync(ms);
        ms.Position = 0;

        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

        var manifestEntry = archive.GetEntry("manifest.json")
            ?? throw new BusinessException("File backup không hợp lệ (thiếu manifest.json).", 400);

        await using var manifestStream = manifestEntry.Open();
        var manifest = await JsonSerializer.DeserializeAsync<BackupManifest>(manifestStream, JsonOpts)
            ?? throw new BusinessException("Không đọc được manifest.json.", 400);

        // Chấp nhận cả version 1.0 (cũ, không có lookup) và 1.1 (mới, có lookup)
        if (manifest.Version != BackupVersion && manifest.Version != "1.0")
            throw new BusinessException(
                $"Phiên bản backup không tương thích (file: {manifest.Version}, hệ thống: {BackupVersion}).", 400);

        var data = await ReadAllEntriesAsync(archive);

        var report = new RestoreReportResponse { PreRestoreSnapshot = preSnapshot.FileName };

        await using var tx = await _ctx.Database.BeginTransactionAsync();
        try
        {
            // Bước 1: Xóa toàn bộ data tables theo thứ tự FK ngược
            await DeleteDataTablesAsync();

            // Bước 2: Upsert lookup tables + SystemConfig
            // Dùng MERGE để không bị lỗi duplicate nếu lookup vẫn còn trong DB.
            // Với backup version 1.0 (cũ) thì bỏ qua vì không có file lookup.
            if (manifest.Version == "1.1")
            {
                foreach (var table in LookupTableOrder)
                {
                    var entryKey = $"_lookup_{table}.json";
                    var count = await UpsertLookupAsync(table, data, entryKey);
                    report.RestoredRows[$"[lookup] {table}"] = count;
                }

                var sysCount = await UpsertSystemConfigAsync(data, "_system_SystemConfig.json");
                report.RestoredRows["[lookup] SystemConfig"] = sysCount;
            }

            // Bước 3: Insert data tables theo thứ tự FK
            report.RestoredRows["Users"] = await BulkInsertJsonAsync("Users", data, "Users.json");
            report.RestoredRows["Profiles"] = await BulkInsertJsonAsync("Profiles", data, "Profiles.json");
            report.RestoredRows["RefreshTokens"] = await BulkInsertJsonAsync("RefreshTokens", data, "RefreshTokens.json");
            report.RestoredRows["Fields"] = await BulkInsertJsonAsync("Fields", data, "Fields.json");
            report.RestoredRows["FieldPriceHistory"] = await BulkInsertJsonAsync("FieldPriceHistory", data, "FieldPriceHistory.json");
            report.RestoredRows["TimeSlots"] = await BulkInsertJsonAsync("TimeSlots", data, "TimeSlots.json");
            report.RestoredRows["FieldSlots"] = await BulkInsertJsonAsync("FieldSlots", data, "FieldSlots.json");
            report.RestoredRows["FieldMaintenanceLogs"] = await BulkInsertJsonAsync("FieldMaintenanceLogs", data, "FieldMaintenanceLogs.json");
            report.RestoredRows["SpecialDays"] = await BulkInsertJsonAsync("SpecialDays", data, "SpecialDays.json");
            report.RestoredRows["PeakSchedules"] = await BulkInsertJsonAsync("PeakSchedules", data, "PeakSchedules.json");
            report.RestoredRows["Services"] = await BulkInsertJsonAsync("Services", data, "Services.json");
            report.RestoredRows["Promotions"] = await BulkInsertJsonAsync("Promotions", data, "Promotions.json");
            report.RestoredRows["Suppliers"] = await BulkInsertJsonAsync("Suppliers", data, "Suppliers.json");
            report.RestoredRows["Products"] = await BulkInsertJsonAsync("Products", data, "Products.json");
            report.RestoredRows["PurchaseOrders"] = await BulkInsertJsonAsync("PurchaseOrders", data, "PurchaseOrders.json");
            report.RestoredRows["PurchaseOrderDetails"] = await BulkInsertJsonAsync("PurchaseOrderDetails", data, "PurchaseOrderDetails.json");
            report.RestoredRows["Bookings"] = await BulkInsertJsonAsync("Bookings", data, "Bookings.json");
            report.RestoredRows["BookingDetails"] = await BulkInsertJsonAsync("BookingDetails", data, "BookingDetails.json");
            report.RestoredRows["BookingServices"] = await BulkInsertJsonAsync("BookingServices", data, "BookingServices.json");
            report.RestoredRows["BookingLogs"] = await BulkInsertJsonAsync("BookingLogs", data, "BookingLogs.json");
            report.RestoredRows["Payments"] = await BulkInsertJsonAsync("Payments", data, "Payments.json");
            report.RestoredRows["Deposits"] = await BulkInsertJsonAsync("Deposits", data, "Deposits.json");
            report.RestoredRows["Incidents"] = await BulkInsertJsonAsync("Incidents", data, "Incidents.json");
            report.RestoredRows["Reviews"] = await BulkInsertJsonAsync("Reviews", data, "Reviews.json");
            report.RestoredRows["Notifications"] = await BulkInsertJsonAsync("Notifications", data, "Notifications.json");

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

        var allTables = LookupTableOrder
            .Select(t => $"_lookup_{t}")
            .Append("_system_SystemConfig")
            .Concat(DataTableOrder)
            .ToList();

        var manifest = new BackupManifest
        {
            Version = BackupVersion,
            CreatedAt = DateTime.UtcNow,
            Tables = allTables
        };
        await WriteEntryAsync(archive, "manifest.json", JsonSerializer.Serialize(manifest, JsonOpts));

        // Lookup tables — dùng IDENTITY_INSERT khi restore, nên cần export cả ID
        await WriteEntryAsync(archive, "_lookup_Roles.json",
            Serialize(await _ctx.Roles.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "_lookup_UserStatuses.json",
            Serialize(await _ctx.UserStatuses.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "_lookup_FieldTypes.json",
            Serialize(await _ctx.FieldTypes.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "_lookup_FieldStatuses.json",
            Serialize(await _ctx.FieldStatuses.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "_lookup_FieldSlotStatuses.json",
            Serialize(await _ctx.FieldSlotStatuses.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "_lookup_BookingStatuses.json",
            Serialize(await _ctx.BookingStatuses.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "_lookup_PaymentStatuses.json",
            Serialize(await _ctx.PaymentStatuses.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "_lookup_PaymentMethods.json",
            Serialize(await _ctx.PaymentMethods.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "_lookup_DepositStatuses.json",
            Serialize(await _ctx.DepositStatuses.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "_lookup_IncidentStatuses.json",
            Serialize(await _ctx.IncidentStatuses.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "_lookup_PurchaseOrderStatuses.json",
            Serialize(await _ctx.PurchaseOrderStatuses.AsNoTracking().ToListAsync()));
        await WriteEntryAsync(archive, "_lookup_PromotionTypes.json",
            Serialize(await _ctx.PromotionTypes.AsNoTracking().ToListAsync()));

        // SystemConfig — không có IDENTITY, dùng MERGE khi restore
        await WriteEntryAsync(archive, "_system_SystemConfig.json",
            Serialize(await _ctx.SystemConfigs.AsNoTracking().ToListAsync()));

        // Data tables
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
    /// Xóa toàn bộ data tables theo thứ tự FK ngược.
    /// Không xóa lookup tables và SystemConfig — chúng được upsert riêng.
    /// </summary>
    private async Task DeleteDataTablesAsync()
    {
        // SystemConfig có FK UpdatedBy → Users, cần null trước khi xóa Users
        await _ctx.Database.ExecuteSqlAsync(
            $"UPDATE SystemConfig SET UpdatedBy = NULL WHERE UpdatedBy IS NOT NULL");

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
        {
            if (!AllowedTables.Contains(table))
                throw new InvalidOperationException($"Table '{table}' không nằm trong whitelist.");

            // table đã validate qua AllowedTables — an toàn để nhúng vào SQL.
            // ReSharper disable once EntityFramework.MightBeUnsafeInterpolation
#pragma warning disable EF1002
            await _ctx.Database.ExecuteSqlRawAsync($"DELETE FROM [{table}]");
#pragma warning restore EF1002
        }
    }

    /// <summary>
    /// Upsert lookup table dùng MERGE: nếu ID đã tồn tại thì UPDATE Name,
    /// nếu chưa có thì INSERT với IDENTITY_INSERT ON.
    /// Đảm bảo ID không thay đổi → tất cả FK trong data tables vẫn hợp lệ.
    /// </summary>
    private async Task<int> UpsertLookupAsync(
        string tableName,
        Dictionary<string, string> data,
        string entryKey)
    {
        if (!data.TryGetValue(entryKey, out var json) || string.IsNullOrWhiteSpace(json))
            return 0;

        if (!AllowedColumns.TryGetValue(tableName, out var allowedCols))
            throw new InvalidOperationException($"Không tìm thấy whitelist cột cho table '{tableName}'.");

        using var doc = JsonDocument.Parse(json);
        var rowCount = doc.RootElement.GetArrayLength();
        if (rowCount == 0) return 0;

        // Xác định tên PK của lookup table từ JSON (cột đầu tiên không phải Name)
        var firstRow = doc.RootElement[0];
        var pkCol = firstRow.EnumerateObject()
            .Select(p => p.Name)
            .First(n => allowedCols.Contains(n) && n != "Name");

        // MERGE: match theo PK, update Name nếu khác, insert nếu chưa có
        // IDENTITY_INSERT ON để giữ nguyên ID gốc khi insert row mới
        var sql = $"""
            SET IDENTITY_INSERT [{tableName}] ON;
            MERGE [{tableName}] AS target
            USING (
                SELECT [{pkCol}], [Name]
                FROM OPENJSON(@json)
                WITH (
                    [{pkCol}] nvarchar(max) '$.{pkCol}',
                    [Name]    nvarchar(max) '$.Name'
                )
            ) AS src ON target.[{pkCol}] = src.[{pkCol}]
            WHEN MATCHED AND target.[Name] <> src.[Name]
                THEN UPDATE SET target.[Name] = src.[Name]
            WHEN NOT MATCHED BY TARGET
                THEN INSERT ([{pkCol}], [Name]) VALUES (src.[{pkCol}], src.[Name]);
            SET IDENTITY_INSERT [{tableName}] OFF;
            """;

        await _ctx.Database.ExecuteSqlRawAsync(sql,
            new Microsoft.Data.SqlClient.SqlParameter("@json", json)
            {
                SqlDbType = System.Data.SqlDbType.NVarChar,
                Size = -1
            });

        return rowCount;
    }

    /// <summary>
    /// Upsert SystemConfig dùng MERGE theo ConfigKey (PK string, không có IDENTITY).
    /// Chỉ update ConfigValue và DataType — giữ nguyên Description.
    /// </summary>
    private async Task<int> UpsertSystemConfigAsync(
        Dictionary<string, string> data,
        string entryKey)
    {
        if (!data.TryGetValue(entryKey, out var json) || string.IsNullOrWhiteSpace(json))
            return 0;

        using var doc = JsonDocument.Parse(json);
        var rowCount = doc.RootElement.GetArrayLength();
        if (rowCount == 0) return 0;

        const string sql = """
            MERGE [SystemConfig] AS target
            USING (
                SELECT [ConfigKey], [ConfigValue], [DataType], [Description]
                FROM OPENJSON(@json)
                WITH (
                    [ConfigKey]   nvarchar(max) '$.ConfigKey',
                    [ConfigValue] nvarchar(max) '$.ConfigValue',
                    [DataType]    nvarchar(max) '$.DataType',
                    [Description] nvarchar(max) '$.Description'
                )
            ) AS src ON target.[ConfigKey] = src.[ConfigKey]
            WHEN MATCHED
                THEN UPDATE SET
                    target.[ConfigValue] = src.[ConfigValue],
                    target.[DataType]    = src.[DataType],
                    target.[Description] = src.[Description]
            WHEN NOT MATCHED BY TARGET
                THEN INSERT ([ConfigKey], [ConfigValue], [DataType], [Description])
                     VALUES (src.[ConfigKey], src.[ConfigValue], src.[DataType], src.[Description]);
            """;

        await _ctx.Database.ExecuteSqlRawAsync(sql,
            new Microsoft.Data.SqlClient.SqlParameter("@json", json)
            {
                SqlDbType = System.Data.SqlDbType.NVarChar,
                Size = -1
            });

        return rowCount;
    }

    /// <summary>
    /// BulkInsert dùng raw SQL thông qua SQL Server OPENJSON.
    ///
    /// SQL injection protection:
    ///   - tableName    → validate qua AllowedTables (whitelist cứng trong code)
    ///   - column names → validate qua AllowedColumns[tableName] (whitelist cứng trong code)
    ///   - json data    → SqlParameter @json, KHÔNG nhúng vào SQL string
    /// </summary>
    private async Task<int> BulkInsertJsonAsync(
        string tableName,
        Dictionary<string, string> data,
        string entryKey)
    {
        if (!data.TryGetValue(entryKey, out var json) || string.IsNullOrWhiteSpace(json))
            return 0;

        if (!AllowedTables.Contains(tableName))
            throw new InvalidOperationException($"Table '{tableName}' không nằm trong whitelist.");

        if (!AllowedColumns.TryGetValue(tableName, out var allowedCols))
            throw new InvalidOperationException($"Không tìm thấy whitelist cột cho table '{tableName}'.");

        using var doc = JsonDocument.Parse(json);
        var rowCount = doc.RootElement.GetArrayLength();
        if (rowCount == 0) return 0;

        var firstRow = doc.RootElement[0];
        var columns = firstRow.EnumerateObject()
            .Select(p => p.Name)
            .Where(name => allowedCols.Contains(name))
            .ToList();

        if (columns.Count == 0)
            throw new InvalidOperationException(
                $"Không có cột hợp lệ nào trong backup entry '{entryKey}'.");

        var colList = string.Join(", ", columns.Select(c => $"[{c}]"));
        var withClause = string.Join(",\n    ", columns.Select(c =>
            $"[{c}] nvarchar(max) '$.{c}'"));

        var sql = $"""
            SET IDENTITY_INSERT [{tableName}] ON;
            INSERT INTO [{tableName}] ({colList})
            SELECT {colList}
            FROM OPENJSON(@json)
            WITH (
                {withClause}
            );
            SET IDENTITY_INSERT [{tableName}] OFF;
            """;

        await _ctx.Database.ExecuteSqlRawAsync(sql,
            new Microsoft.Data.SqlClient.SqlParameter("@json", json)
            {
                SqlDbType = System.Data.SqlDbType.NVarChar,
                Size = -1
            });

        return rowCount;
    }

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
        var safe = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safe) || safe != fileName)
            throw new BusinessException("Tên file không hợp lệ.", 400);

        return Path.Combine(GetSnapshotDir(), safe);
    }
}

// ── Manifest ──────────────────────────────────────────────────────

internal class BackupManifest
{
    public string Version { get; set; } = "1.1";
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
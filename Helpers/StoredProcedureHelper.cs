using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KLCN_API.Helpers;

public static class StoredProcedureHelper
{
    /// <summary>
    /// Thực thi Stored Procedure không trả về kết quả (INSERT/UPDATE).
    /// Ném SqlException với message từ SP nếu SP THROW lỗi.
    /// </summary>
    public static async Task ExecuteSpAsync(
        DbContext ctx,
        string spName,
        params SqlParameter[] parameters)
    {
        var paramList = BuildParamString(parameters);
        var sql = $"EXEC {spName} {paramList}";
        await ctx.Database.ExecuteSqlRawAsync(sql, parameters.Cast<object>().ToArray());
    }

    /// <summary>
    /// Thực thi SP và đọc kết quả vào List&lt;T&gt; qua DataReader.
    /// </summary>
    public static async Task<List<T>> QuerySpAsync<T>(
        DbContext ctx,
        string spName,
        Func<IDataReader, T> mapper,
        params SqlParameter[] parameters)
    {
        var results = new List<T>();
        var conn = ctx.Database.GetDbConnection();

        try
        {
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = spName;
            cmd.CommandType = CommandType.StoredProcedure;

            foreach (var p in parameters)
                cmd.Parameters.Add(p);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                results.Add(mapper(reader));
        }
        finally
        {
            // EF quản lý connection — chỉ đóng nếu chúng ta tự mở
            if (conn.State == ConnectionState.Open)
                await conn.CloseAsync();
        }

        return results;
    }

    // ── Factory methods cho các SP thường dùng ──────────────────

    /// <summary>sp_HoldSlots — giữ slot, kiểm tra ràng buộc đặt trước.</summary>
    public static Task HoldSlotsAsync(DbContext ctx, string fieldSlotIds, int? userId = null)
        => ExecuteSpAsync(ctx, "sp_HoldSlots",
            new SqlParameter("@FieldSlotIds", fieldSlotIds),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value));

    /// <summary>sp_ConfirmBooking — xác nhận booking + tính tiền + tạo deposit nếu cần.</summary>
    public static Task ConfirmBookingAsync(
        DbContext ctx, int bookingId, string fieldSlotIds,
        bool isFullPayment = true, int? userId = null)
        => ExecuteSpAsync(ctx, "sp_ConfirmBooking",
            new SqlParameter("@BookingId", bookingId),
            new SqlParameter("@FieldSlotIds", fieldSlotIds),
            new SqlParameter("@IsFullPayment", isFullPayment),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value));

    /// <summary>sp_CancelBooking — hủy + hoàn tiền theo policy.</summary>
    public static Task CancelBookingAsync(
        DbContext ctx, int bookingId,
        int? userId = null, string? reason = null, bool isAdminOverride = false)
        => ExecuteSpAsync(ctx, "sp_CancelBooking",
            new SqlParameter("@BookingId", bookingId),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value),
            new SqlParameter("@Reason", (object?)reason ?? DBNull.Value),
            new SqlParameter("@IsAdminOverride", isAdminOverride));

    /// <summary>sp_RecordDeposit — ghi nhận thanh toán đặt cọc.</summary>
    public static Task RecordDepositAsync(
        DbContext ctx, int bookingId, decimal amount,
        int methodId, string? transactionCode = null, int? userId = null)
        => ExecuteSpAsync(ctx, "sp_RecordDeposit",
            new SqlParameter("@BookingId", bookingId),
            new SqlParameter("@Amount", amount),
            new SqlParameter("@MethodId", methodId),
            new SqlParameter("@TransactionCode", (object?)transactionCode ?? DBNull.Value),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value));

    /// <summary>sp_RecordFullPayment — thanh toán phần còn lại.</summary>
    public static Task RecordFullPaymentAsync(
        DbContext ctx, int bookingId, int methodId,
        string? transactionCode = null, int? userId = null)
        => ExecuteSpAsync(ctx, "sp_RecordFullPayment",
            new SqlParameter("@BookingId", bookingId),
            new SqlParameter("@MethodId", methodId),
            new SqlParameter("@TransactionCode", (object?)transactionCode ?? DBNull.Value),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value));

    /// <summary>sp_ApplyPromotion — áp dụng voucher vào booking.</summary>
    public static Task ApplyPromotionAsync(
        DbContext ctx, int bookingId, string code, int? userId = null)
        => ExecuteSpAsync(ctx, "sp_ApplyPromotion",
            new SqlParameter("@BookingId", bookingId),
            new SqlParameter("@Code", code),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value));

    /// <summary>sp_RescheduleBooking — đổi lịch một slot trong booking.</summary>
    public static Task RescheduleBookingAsync(
        DbContext ctx, int bookingDetailId, int newFieldSlotId, int? userId = null)
        => ExecuteSpAsync(ctx, "sp_RescheduleBooking",
            new SqlParameter("@BookingDetailId", bookingDetailId),
            new SqlParameter("@NewFieldSlotId", newFieldSlotId),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value));

    /// <summary>sp_ConfirmPurchaseOrder — xác nhận đơn nhập kho, cập nhật tồn kho.</summary>
    public static Task ConfirmPurchaseOrderAsync(
        DbContext ctx, int purchaseOrderId, int? userId = null)
        => ExecuteSpAsync(ctx, "sp_ConfirmPurchaseOrder",
            new SqlParameter("@PurchaseOrderId", purchaseOrderId),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value));

    /// <summary>sp_GenerateSlots — sinh FieldSlots cho khoảng ngày.</summary>
    public static Task GenerateSlotsAsync(DbContext ctx, DateOnly startDate, DateOnly endDate)
        => ExecuteSpAsync(ctx, "sp_GenerateSlots",
            new SqlParameter("@StartDate", startDate.ToDateTime(TimeOnly.MinValue)),
            new SqlParameter("@EndDate", endDate.ToDateTime(TimeOnly.MinValue)));

    /// <summary>sp_ReleaseExpiredSlots — giải phóng slot hết hạn hold + tự hủy deposit quá hạn.</summary>
    public static Task ReleaseExpiredSlotsAsync(DbContext ctx)
        => ExecuteSpAsync(ctx, "sp_ReleaseExpiredSlots");

    /// <summary>sp_UpdateSystemConfig — cập nhật cấu hình hệ thống.</summary>
    public static Task UpdateSystemConfigAsync(
        DbContext ctx, string configKey, string configValue, int? userId = null)
        => ExecuteSpAsync(ctx, "sp_UpdateSystemConfig",
            new SqlParameter("@ConfigKey", configKey),
            new SqlParameter("@ConfigValue", configValue),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value));

    // ── Internal ─────────────────────────────────────────────────

    private static string BuildParamString(SqlParameter[] parameters)
        => string.Join(", ", parameters.Select(p => p.ParameterName));
}
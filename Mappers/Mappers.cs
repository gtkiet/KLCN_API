using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Entities;

namespace KLCN_API.Mappers;

/// <summary>
/// Mapper tập trung cho User entity → DTO.
/// Dùng chung ở AuthService, ProfileService, UserService để tránh lặp code
/// và đảm bảo mapping nhất quán khi thêm field mới.
/// </summary>
public static class UserMapper
{
    /// <summary>Map sang UserResponse dùng trong danh sách và trong LoginResponse.</summary>
    public static UserResponse ToResponse(User u) => new()
    {
        UserId = u.UserId,
        FullName = u.FullName,
        Email = u.Email,
        Phone = u.Phone,
        Role = u.Role?.Name ?? string.Empty,
        RoleId = u.RoleId,
        Status = u.Status?.Name ?? string.Empty,
        StatusId = u.StatusId,
        AvatarUrl = u.Profile?.AvatarUrl,
        CreatedAt = u.CreatedAt
    };

    /// <summary>Map sang UserDetailResponse dùng trong xem chi tiết và profile cá nhân.</summary>
    public static UserDetailResponse ToDetailResponse(User u) => new()
    {
        UserId = u.UserId,
        FullName = u.FullName,
        Email = u.Email,
        Phone = u.Phone,
        Role = u.Role?.Name ?? string.Empty,
        RoleId = u.RoleId,
        Status = u.Status?.Name ?? string.Empty,
        StatusId = u.StatusId,
        CreatedAt = u.CreatedAt,
        Profile = u.Profile is null ? null : new ProfileResponse
        {
            AvatarUrl = u.Profile.AvatarUrl,
            DateOfBirth = u.Profile.DateOfBirth,
            Address = u.Profile.Address
        }
    };
}

/// <summary>
/// Mapper tập trung cho Field entity → DTO.
/// Dùng chung cho FieldService để tránh lặp code.
/// </summary>
public static class FieldMapper
{
    public static FieldResponse ToResponse(Field f) => new()
    {
        FieldId = f.FieldId,
        Name = f.Name,
        Description = f.Description,
        BasePrice = f.BasePrice,
        PeakPrice = f.PeakPrice,
        ImageUrl = f.ImageUrl,
        FieldType = f.Type?.Name ?? string.Empty,
        TypeId = f.TypeId,
        Status = f.Status?.Name ?? string.Empty,
        StatusId = f.StatusId,
        CreatedAt = f.CreatedAt
    };

    public static SlotResponse ToSlotResponse(FieldSlot fs) => new()
    {
        FieldSlotId = fs.FieldSlotId,
        SlotId = fs.SlotId,
        StartTime = fs.TimeSlot.StartTime,
        EndTime = fs.TimeSlot.EndTime,
        Price = fs.Price,
        IsPeakHour = fs.TimeSlot.IsPeakHour,
        Status = fs.Status?.Name ?? string.Empty,
        StatusId = fs.StatusId,
        HoldRemainingSeconds = fs.StatusId == 2 && fs.HoldExpireAt > DateTime.UtcNow
            ? (int)(fs.HoldExpireAt!.Value - DateTime.UtcNow).TotalSeconds
            : null
    };
}

/// <summary>
/// Mapper tập trung cho Notification entity → DTO.
/// Dùng chung ở NotificationService để tránh lặp code.
/// </summary>
public static class NotificationMapper
{
    public static NotificationResponse ToResponse(Notification n) => new()
    {
        NotificationId = n.NotificationId,
        Title = n.Title,
        Body = n.Body,
        Type = n.Type,
        RefId = n.RefId,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt
    };
}

/// <summary>
/// Mapper tập trung cho SystemConfig entity → DTO.
/// Dùng chung ở SystemConfigService để tránh lặp code.
/// </summary>
public static class SystemConfigMapper
{
    public static SystemConfigResponse ToResponse(SystemConfig c) => new()
    {
        ConfigKey = c.ConfigKey,
        ConfigValue = c.ConfigValue,
        DataType = c.DataType,
        Description = c.Description,
        UpdatedAt = c.UpdatedAt,
        // UpdatedByUser có thể null nếu chưa ai chỉnh sửa
        UpdatedBy = c.UpdatedByUser?.FullName
    };
}


/// <summary>
/// Mapper tập trung cho Incident entity → DTO.
/// Dùng chung ở IncidentService để tránh lặp code.
/// </summary>
public static class IncidentMapper
{
    public static IncidentResponse ToResponse(Incident i) => new()
    {
        IncidentId = i.IncidentId,
        FieldId = i.FieldId,
        FieldName = i.Field?.Name ?? string.Empty,
        ReportedBy = i.ReportedByUser?.FullName ?? string.Empty,
        Title = i.Title,
        Description = i.Description,
        ImageUrl = i.ImageUrl,
        Status = i.Status?.Name ?? string.Empty,
        StatusId = i.StatusId,
        HandledBy = i.HandledByUser?.FullName,
        HandledAt = i.HandledAt,
        HandledNote = i.HandledNote,
        CreatedAt = i.CreatedAt
    };
}

/// <summary>
/// Mapper tập trung cho Review entity → DTO.
/// Dùng chung ở ReviewService để tránh lặp code.
/// </summary>
public static class ReviewMapper
{
    public static ReviewResponse ToResponse(Review r) => new()
    {
        ReviewId = r.ReviewId,
        BookingId = r.BookingId,
        UserId = r.UserId,
        UserName = r.User?.FullName ?? string.Empty,
        AvatarUrl = r.User?.Profile?.AvatarUrl,
        FieldId = r.FieldId,
        FieldName = r.Field?.Name ?? string.Empty,
        Rating = r.Rating,
        Comment = r.Comment,
        ImageUrl = r.ImageUrl,
        IsVisible = r.IsVisible,
        CreatedAt = r.CreatedAt
    };
}

/// <summary>
/// Mapper tập trung cho Booking entity → DTO.
/// Tách ra để BookingService và PaymentService cùng dùng mà không circular dependency.
/// </summary>
public static class BookingMapper
{
    public static BookingResponse MapDetail(Booking b) => new()
    {
        BookingId = b.BookingId,
        Customer = UserMapper.ToResponse(b.User),
        Status = b.Status?.Name ?? string.Empty,
        StatusId = b.StatusId,
        SubTotal = b.SubTotal,
        DiscountAmount = b.DiscountAmount,
        TaxAmount = b.TaxAmount,
        TotalAmount = b.TotalAmount,
        DepositAmount = b.DepositAmount,
        PromotionCode = b.Promotion?.Code,
        Note = b.Note,
        CancelReason = b.CancelReason,
        RescheduleCount = b.RescheduleCount,
        CreatedAt = b.CreatedAt,
        UpdatedAt = b.UpdatedAt,
        Details = b.BookingDetails?.Select(d => new BookingDetailResponse
        {
            BookingDetailId = d.BookingDetailId,
            FieldId = d.FieldSlot.FieldId,
            FieldName = d.FieldSlot.Field?.Name ?? string.Empty,
            FieldType = d.FieldSlot.Field?.Type?.Name ?? string.Empty,
            SlotDate = d.FieldSlot.SlotDate,
            StartTime = d.FieldSlot.TimeSlot.StartTime,
            EndTime = d.FieldSlot.TimeSlot.EndTime,
            Price = d.Price
        }).ToList() ?? [],
        Services = b.BookingServices?.Select(bs => new BookingServiceResponse
        {
            ServiceId = bs.ServiceId,
            ServiceName = bs.Service?.Name ?? string.Empty,
            Quantity = bs.Quantity,
            UnitPrice = bs.UnitPrice
        }).ToList() ?? [],
        Deposit = b.Deposit is null ? null : new DepositResponse
        {
            DepositId = b.Deposit.DepositId,
            BookingId = b.BookingId,
            RequiredAmount = b.Deposit.RequiredAmount,
            PaidAmount = b.Deposit.PaidAmount,
            Status = b.Deposit.Status?.Name ?? string.Empty,
            StatusId = b.Deposit.StatusId,
            DeadlineAt = b.Deposit.DeadlineAt,
            MinutesLeft = Math.Max(0,
                (int)(b.Deposit.DeadlineAt - DateTime.UtcNow).TotalMinutes),
            PaidAt = b.Deposit.PaidAt
        }
    };

    public static BookingSummaryResponse MapSummary(Booking b)
    {
        var earliest = b.BookingDetails?
            .OrderBy(d => d.FieldSlot.SlotDate)
            .ThenBy(d => d.FieldSlot.TimeSlot.StartTime)
            .FirstOrDefault();

        return new BookingSummaryResponse
        {
            BookingId = b.BookingId,
            CustomerName = b.User?.FullName ?? string.Empty,
            CustomerPhone = b.User?.Phone ?? string.Empty,
            Status = b.Status?.Name ?? string.Empty,
            StatusId = b.StatusId,
            TotalAmount = b.TotalAmount,
            SlotCount = b.BookingDetails?.Count ?? 0,
            EarliestSlotDate = earliest?.FieldSlot.SlotDate,
            EarliestSlotTime = earliest?.FieldSlot.TimeSlot.StartTime,
            FieldName = earliest?.FieldSlot.Field?.Name,
            CreatedAt = b.CreatedAt
        };
    }
}


/// <summary>
/// Mapper tập trung cho Inventory entities → DTO.
/// Theo cùng pattern UserMapper: static class, tách file riêng.
/// </summary>
public static class InventoryMapper
{
    // ── Supplier ─────────────────────────────────────────────

    public static SupplierResponse ToResponse(Supplier s) => new()
    {
        SupplierId = s.SupplierId,
        Name = s.Name,
        ContactName = s.ContactName,
        Phone = s.Phone,
        Email = s.Email,
        Address = s.Address
    };

    // ── Product ──────────────────────────────────────────────

    public static ProductResponse ToResponse(Product p) => new()
    {
        ProductId = p.ProductId,
        Name = p.Name,
        Unit = p.Unit,
        StockQty = p.StockQty,
        MinQty = p.MinQty
        // IsLowStock và StockBuffer là computed properties — tự tính từ trên
    };

    // ── PurchaseOrder ────────────────────────────────────────

    public static PurchaseOrderResponse ToResponse(PurchaseOrder po) => new()
    {
        PurchaseOrderId = po.PurchaseOrderId,
        Supplier = ToResponse(po.Supplier),
        CreatedBy = po.CreatedByUser?.FullName ?? string.Empty,
        Status = po.Status?.Name ?? string.Empty,
        StatusId = po.StatusId,
        TotalAmount = po.TotalAmount,
        Note = po.Note,
        CreatedAt = po.CreatedAt,
        ConfirmedAt = po.ConfirmedAt,
        Items = po.Details.Select(ToDetailResponse).ToList()
    };

    public static PurchaseOrderDetailResponse ToDetailResponse(PurchaseOrderDetail d) => new()
    {
        ProductId = d.ProductId,
        ProductName = d.Product?.Name ?? string.Empty,
        Unit = d.Product?.Unit,
        Quantity = d.Quantity,
        UnitPrice = d.UnitPrice
        // SubTotal là computed property
    };
}

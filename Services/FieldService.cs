//using KLCN_API.Data;
//using KLCN_API.Helpers;
//using KLCN_API.Middleware;
//using KLCN_API.Models.DTOs.Request;
//using KLCN_API.Models.DTOs.Response;
//using KLCN_API.Models.Entities;
//using KLCN_API.Repositories.Interfaces;
//using KLCN_API.Services.Interfaces;
//using Microsoft.Data.SqlClient;

//namespace KLCN_API.Services;

//public class FieldService : IFieldService
//{
//    private readonly IFieldRepository _fieldRepo;
//    private readonly StoredProcedureHelper _sp;

//    public FieldService(IFieldRepository fieldRepo, StoredProcedureHelper sp)
//    {
//        _fieldRepo = fieldRepo;
//        _sp = sp;
//    }

//    public async Task<PagedResponse<FieldResponse>> GetFieldsAsync(GetFieldsRequest request)
//    {
//        var (items, total) = await _fieldRepo.GetFieldsAsync(
//            request.Search, request.TypeId, request.StatusId,
//            request.Page, request.PageSize);

//        return new PagedResponse<FieldResponse>
//        {
//            Items = items.Select(MapField).ToList(),
//            TotalCount = total,
//            Page = request.Page,
//            PageSize = request.PageSize
//        };
//    }

//    public async Task<FieldResponse> GetByIdAsync(int fieldId)
//    {
//        var field = await _fieldRepo.GetByIdAsync(fieldId)
//            ?? throw new NotFoundException("Sân bóng", fieldId);

//        return MapField(field);
//    }

//    public async Task<FieldResponse> CreateAsync(int adminId, CreateFieldRequest request)
//    {
//        var field = new Field
//        {
//            Name = request.Name.Trim(),
//            Description = request.Description?.Trim(),
//            BasePrice = request.BasePrice,
//            PeakPrice = request.PeakPrice,
//            TypeId = request.TypeId,
//            ImageUrl = request.ImageUrl,
//            StatusId = 1,
//            CreatedAt = DateTime.UtcNow,
//            UpdatedAt = DateTime.UtcNow
//        };

//        var created = await _fieldRepo.CreateAsync(field);
//        return MapField(created);
//    }

//    public async Task<FieldResponse> UpdateAsync(int fieldId, int adminId, UpdateFieldRequest request)
//    {
//        var field = await _fieldRepo.GetByIdAsync(fieldId)
//            ?? throw new NotFoundException("Sân bóng", fieldId);

//        if (request.Name is not null) field.Name = request.Name.Trim();
//        if (request.Description is not null) field.Description = request.Description.Trim();
//        if (request.ImageUrl is not null) field.ImageUrl = request.ImageUrl;
//        if (request.TypeId.HasValue) field.TypeId = request.TypeId.Value;
//        if (request.StatusId.HasValue) field.StatusId = request.StatusId.Value;

//        if (request.BasePrice.HasValue) field.BasePrice = request.BasePrice.Value;
//        if (request.PeakPrice.HasValue) field.PeakPrice = request.PeakPrice.Value;

//        await _fieldRepo.UpdateAsync(field);
//        return MapField(field);
//    }

//    public async Task DeleteAsync(int fieldId)
//    {
//        await _fieldRepo.GetByIdAsync(fieldId)
//            ?? throw new NotFoundException("Sân bóng", fieldId);

//        await _fieldRepo.SoftDeleteAsync(fieldId);
//    }

//    // ── Schedule & Slots ─────────────────────────────────────────

//    public async Task<List<FieldScheduleResponse>> GetScheduleAsync(GetFieldScheduleRequest request)
//    {
//        var slots = await _fieldRepo.GetScheduleAsync(request.FieldId, request.Date);

//        return slots
//            .GroupBy(fs => fs.Field)
//            .Select(g => new FieldScheduleResponse
//            {
//                FieldId = g.Key.FieldId,
//                FieldName = g.Key.Name,
//                FieldType = g.Key.Type?.Name ?? string.Empty,
//                ImageUrl = g.Key.ImageUrl,
//                SlotDate = request.Date,
//                Slots = g.Select(MapSlot).ToList()
//            })
//            .ToList();
//    }

//    public async Task GenerateSlotsAsync(GenerateSlotsRequest request)
//    {
//        if (request.StartDate > request.EndDate)
//            throw new BusinessException("StartDate phải nhỏ hơn hoặc bằng EndDate.", 400);

//        await _sp.ExecuteAsync("sp_GenerateSlots",
//            new SqlParameter("@StartDate", request.StartDate.ToDateTime(TimeOnly.MinValue)),
//            new SqlParameter("@EndDate", request.EndDate.ToDateTime(TimeOnly.MinValue)));
//    }

//    // ── Price History ────────────────────────────────────────────

//    public async Task<List<FieldPriceHistoryResponse>> GetPriceHistoryAsync(int fieldId)
//    {
//        await _fieldRepo.GetByIdAsync(fieldId)
//            ?? throw new NotFoundException("Sân bóng", fieldId);

//        var history = await _fieldRepo.GetPriceHistoryAsync(fieldId);
//        return history.Select(h => new FieldPriceHistoryResponse
//        {
//            HistoryId = h.HistoryId,
//            OldBasePrice = h.OldBasePrice,
//            OldPeakPrice = h.OldPeakPrice,
//            NewBasePrice = h.NewBasePrice,
//            NewPeakPrice = h.NewPeakPrice,
//            ChangedBy = h.ChangedByUser?.FullName ?? string.Empty,
//            ChangedAt = h.ChangedAt,
//            Reason = h.Reason
//        }).ToList();
//    }

//    // ── Maintenance ──────────────────────────────────────────────

//    public async Task<List<FieldMaintenanceLogResponse>> GetMaintenanceLogsAsync(int fieldId)
//    {
//        await _fieldRepo.GetByIdAsync(fieldId)
//            ?? throw new NotFoundException("Sân bóng", fieldId);

//        var logs = await _fieldRepo.GetMaintenanceLogsAsync(fieldId);
//        return logs.Select(l => new FieldMaintenanceLogResponse
//        {
//            LogId = l.LogId,
//            Reason = l.Reason,
//            StartDate = l.StartDate,
//            EndDate = l.EndDate,
//            CreatedBy = l.CreatedByUser?.FullName ?? string.Empty,
//            CreatedAt = l.CreatedAt
//        }).ToList();
//    }

//    public async Task AddMaintenanceLogAsync(int fieldId, int createdBy, CreateMaintenanceRequest request)
//    {
//        await _fieldRepo.GetByIdAsync(fieldId)
//            ?? throw new NotFoundException("Sân bóng", fieldId);

//        if (request.EndDate.HasValue && request.EndDate.Value < request.StartDate)
//            throw new BusinessException("Ngày kết thúc phải sau ngày bắt đầu.", 400);

//        var log = new FieldMaintenanceLog
//        {
//            FieldId = fieldId,
//            Reason = request.Reason.Trim(),
//            StartDate = request.StartDate,
//            EndDate = request.EndDate,
//            CreatedBy = createdBy,
//            CreatedAt = DateTime.UtcNow
//        };

//        await _fieldRepo.AddMaintenanceLogAsync(log);

//        // Chuyển sân sang trạng thái Bảo trì nếu StartDate là hôm nay
//        if (request.StartDate == DateOnly.FromDateTime(DateTime.UtcNow))
//        {
//            var field = await _fieldRepo.GetByIdAsync(fieldId);
//            if (field is not null)
//            {
//                field.StatusId = 2; // Bảo trì
//                await _fieldRepo.UpdateAsync(field);
//            }
//        }
//    }

//    // ── Mappers ──────────────────────────────────────────────────

//    private static FieldResponse MapField(Field f) => new()
//    {
//        FieldId = f.FieldId,
//        Name = f.Name,
//        Description = f.Description,
//        BasePrice = f.BasePrice,
//        PeakPrice = f.PeakPrice,
//        ImageUrl = f.ImageUrl,
//        FieldType = f.Type?.Name ?? string.Empty,
//        TypeId = f.TypeId,
//        Status = f.Status?.Name ?? string.Empty,
//        StatusId = f.StatusId,
//        CreatedAt = f.CreatedAt
//    };

//    private static SlotResponse MapSlot(FieldSlot fs) => new()
//    {
//        FieldSlotId = fs.FieldSlotId,
//        SlotId = fs.SlotId,
//        StartTime = fs.TimeSlot.StartTime,
//        EndTime = fs.TimeSlot.EndTime,
//        Price = fs.Price,
//        IsPeakHour = fs.TimeSlot.IsPeakHour,
//        Status = fs.Status?.Name ?? string.Empty,
//        StatusId = fs.StatusId,
//        HoldRemainingSeconds = fs.StatusId == 2 && fs.HoldExpireAt > DateTime.UtcNow
//            ? (int)(fs.HoldExpireAt!.Value - DateTime.UtcNow).TotalSeconds
//            : null
//    };
//}
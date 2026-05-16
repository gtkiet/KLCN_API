using KLCN_API.Data;
using KLCN_API.Helpers;
using KLCN_API.Mappers;
using KLCN_API.Middleware;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Entities;
using KLCN_API.Models.Enums;
using KLCN_API.Repositories.Interfaces;
using KLCN_API.Services.Interfaces;

namespace KLCN_API.Services;

public class FieldService : IFieldService
{
    private readonly IFieldRepository _fieldRepo;
    private readonly SportPlusDbContext _ctx;

    public FieldService(IFieldRepository fieldRepo, SportPlusDbContext ctx)
    {
        _fieldRepo = fieldRepo;
        _ctx = ctx;
    }

    // ── CRUD ──────────────────────────────────────────────────────

    public async Task<PagedResponse<FieldResponse>> GetFieldsAsync(GetFieldsRequest request)
    {
        var (items, total) = await _fieldRepo.GetFieldsAsync(
            request.Search, request.TypeId, request.StatusId,
            request.Page, request.PageSize);

        return new PagedResponse<FieldResponse>
        {
            Items = items.Select(FieldMapper.ToResponse).ToList(),
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<FieldResponse> GetByIdAsync(int fieldId)
    {
        var field = await _fieldRepo.GetByIdAsync(fieldId)
            ?? throw new NotFoundException("Sân bóng", fieldId);
        return FieldMapper.ToResponse(field);
    }

    public async Task<FieldResponse> CreateAsync(int adminId, CreateFieldRequest request)
    {
        if (request.PeakPrice < request.BasePrice)
            throw new BusinessException(
                "Giá cao điểm phải lớn hơn hoặc bằng giá cơ bản.", 400);

        var field = new Field
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            BasePrice = request.BasePrice,
            PeakPrice = request.PeakPrice,
            TypeId = request.TypeId,
            ImageUrl = null, // upload ảnh qua endpoint riêng POST /{fieldId}/image
            StatusId = (int)FieldStatusEnum.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _fieldRepo.CreateAsync(field);
        return FieldMapper.ToResponse(created);
    }

    public async Task<FieldResponse> UpdateAsync(
        int fieldId, int adminId, UpdateFieldRequest request)
    {
        var field = await _fieldRepo.GetByIdAsync(fieldId)
            ?? throw new NotFoundException("Sân bóng", fieldId);

        if (request.Name is not null) field.Name = request.Name.Trim();
        if (request.Description is not null) field.Description = request.Description.Trim();
        if (request.TypeId.HasValue) field.TypeId = request.TypeId.Value;
        if (request.StatusId.HasValue) field.StatusId = request.StatusId.Value;
        // ImageUrl không update qua đây — dùng endpoint POST /{fieldId}/image

        var newBase = request.BasePrice ?? field.BasePrice;
        var newPeak = request.PeakPrice ?? field.PeakPrice;

        if (newPeak < newBase)
            throw new BusinessException(
                "Giá cao điểm phải lớn hơn hoặc bằng giá cơ bản.", 400);

        if (request.BasePrice.HasValue) field.BasePrice = request.BasePrice.Value;
        if (request.PeakPrice.HasValue) field.PeakPrice = request.PeakPrice.Value;

        await _fieldRepo.UpdateAsync(field);

        // Reload navigation sau khi đổi TypeId / StatusId
        await _ctx.Entry(field).Reference(f => f.Type).LoadAsync();
        await _ctx.Entry(field).Reference(f => f.Status).LoadAsync();

        return FieldMapper.ToResponse(field);
    }

    public async Task DeleteAsync(int fieldId)
    {
        _ = await _fieldRepo.GetByIdAsync(fieldId)
            ?? throw new NotFoundException("Sân bóng", fieldId);
        await _fieldRepo.SoftDeleteAsync(fieldId);
    }

    // ── Image upload ──────────────────────────────────────────────

    /// <summary>
    /// Upload ảnh sân vào Uploads/fields/.
    /// Lưu file mới trước, xóa ảnh cũ sau — nếu lưu file lỗi thì DB không bị đụng.
    /// Trả về relative URL "/Uploads/fields/{guid}.ext".
    /// </summary>
    public async Task<string> UploadImageAsync(
        int fieldId, IFormFile file, IWebHostEnvironment env)
    {
        var field = await _fieldRepo.GetByIdAsync(fieldId)
            ?? throw new NotFoundException("Sân bóng", fieldId);

        var newUrl = await ImageUploadHelper.SaveAsync(
            file, env.ContentRootPath, subfolder: "fields");

        ImageUploadHelper.DeleteIfExists(field.ImageUrl, env.ContentRootPath);

        field.ImageUrl = newUrl;
        await _fieldRepo.UpdateAsync(field);

        return newUrl;
    }

    // ── Schedule & slots ──────────────────────────────────────────

    public async Task<List<FieldScheduleResponse>> GetScheduleAsync(
        GetFieldScheduleRequest request)
    {
        var slots = await _fieldRepo.GetScheduleAsync(request.FieldId, request.Date);

        return slots
            .GroupBy(fs => fs.Field)
            .Select(g => new FieldScheduleResponse
            {
                FieldId = g.Key.FieldId,
                FieldName = g.Key.Name,
                FieldType = g.Key.Type?.Name ?? string.Empty,
                ImageUrl = g.Key.ImageUrl,
                SlotDate = request.Date,
                Slots = g.Select(FieldMapper.ToSlotResponse).ToList()
            })
            .ToList();
    }

    public async Task GenerateSlotsAsync(GenerateSlotsRequest request)
    {
        if (request.StartDate > request.EndDate)
            throw new BusinessException("StartDate phải nhỏ hơn hoặc bằng EndDate.", 400);

        await StoredProcedureHelper.GenerateSlotsAsync(
            _ctx, request.StartDate, request.EndDate);
    }

    // ── Price history ─────────────────────────────────────────────

    public async Task<List<FieldPriceHistoryResponse>> GetPriceHistoryAsync(int fieldId)
    {
        _ = await _fieldRepo.GetByIdAsync(fieldId)
            ?? throw new NotFoundException("Sân bóng", fieldId);

        var history = await _fieldRepo.GetPriceHistoryAsync(fieldId);

        return history.Select(h => new FieldPriceHistoryResponse
        {
            HistoryId = h.HistoryId,
            OldBasePrice = h.OldBasePrice,
            OldPeakPrice = h.OldPeakPrice,
            NewBasePrice = h.NewBasePrice,
            NewPeakPrice = h.NewPeakPrice,
            ChangedBy = h.ChangedByUser?.FullName ?? string.Empty,
            ChangedAt = h.ChangedAt,
            Reason = h.Reason
        }).ToList();
    }

    // ── Maintenance logs ──────────────────────────────────────────

    public async Task<List<FieldMaintenanceLogResponse>> GetMaintenanceLogsAsync(int fieldId)
    {
        _ = await _fieldRepo.GetByIdAsync(fieldId)
            ?? throw new NotFoundException("Sân bóng", fieldId);

        var logs = await _fieldRepo.GetMaintenanceLogsAsync(fieldId);

        return logs.Select(l => new FieldMaintenanceLogResponse
        {
            LogId = l.LogId,
            FieldId = l.FieldId,
            FieldName = l.Field?.Name ?? string.Empty,
            Reason = l.Reason,
            StartDate = l.StartDate,
            EndDate = l.EndDate,
            CreatedBy = l.CreatedByUser?.FullName ?? string.Empty,
            CreatedAt = l.CreatedAt
        }).ToList();
    }

    public async Task AddMaintenanceLogAsync(
        int fieldId, int createdBy, CreateMaintenanceRequest request)
    {
        var field = await _fieldRepo.GetByIdAsync(fieldId)
            ?? throw new NotFoundException("Sân bóng", fieldId);

        if (request.EndDate.HasValue && request.EndDate.Value < request.StartDate)
            throw new BusinessException(
                "Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.", 400);

        var log = new FieldMaintenanceLog
        {
            FieldId = fieldId,
            Reason = request.Reason.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        await _fieldRepo.AddMaintenanceLogAsync(log);

        // Tự động chuyển sân sang Bảo trì nếu StartDate là hôm nay (giờ VN UTC+7)
        var todayVn = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        if (request.StartDate == todayVn
            && field.StatusId != (int)FieldStatusEnum.Maintenance)
        {
            field.StatusId = (int)FieldStatusEnum.Maintenance;
            await _fieldRepo.UpdateAsync(field);
        }
    }
}
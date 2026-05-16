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

public class IncidentService : IIncidentService
{
    private readonly IIncidentRepository _incidentRepo;
    private readonly IWebHostEnvironment _env;

    public IncidentService(
        IIncidentRepository incidentRepo,
        IWebHostEnvironment env)
    {
        _incidentRepo = incidentRepo;
        _env = env;
    }

    public async Task<PagedResponse<IncidentResponse>> GetIncidentsAsync(
        int? fieldId, int? statusId, int page, int pageSize)
    {
        var (items, total) = await _incidentRepo.GetIncidentsAsync(
            fieldId, statusId, page, pageSize);

        return new PagedResponse<IncidentResponse>
        {
            Items = items.Select(IncidentMapper.ToResponse).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IncidentResponse> GetByIdAsync(int incidentId)
    {
        var incident = await _incidentRepo.GetByIdAsync(incidentId)
            ?? throw new NotFoundException("Sự cố", incidentId);
        return IncidentMapper.ToResponse(incident);
    }

    /// <summary>
    /// Tạo báo cáo sự cố kèm ảnh (tuỳ chọn).
    /// Thứ tự: validate → lưu file → lưu DB.
    /// Nếu lưu DB lỗi → xóa file vừa lưu (rollback).
    /// </summary>
    public async Task<IncidentResponse> CreateAsync(
        CreateIncidentRequest request, int reportedBy)
    {
        // ── 1. Lưu ảnh nếu có — sau validate, trước DB ────────────
        string? imageUrl = null;
        if (request.Image is not null)
            imageUrl = await ImageUploadHelper.SaveAsync(
                request.Image, _env.ContentRootPath, subfolder: "incidents");

        // ── 2. Lưu DB — rollback file nếu lỗi ─────────────────────
        try
        {
            var incident = new Incident
            {
                FieldId = request.FieldId,
                ReportedByUserId = reportedBy,
                Title = request.Title.Trim(),
                Description = request.Description?.Trim(),
                ImageUrl = imageUrl,
                StatusId = (int)IncidentStatusEnum.New,
                CreatedAt = DateTime.UtcNow,
            };

            var created = await _incidentRepo.CreateAsync(incident);
            return IncidentMapper.ToResponse(created);
        }
        catch
        {
            if (imageUrl is not null)
                ImageUploadHelper.DeleteIfExists(imageUrl, _env.ContentRootPath);
            throw;
        }
    }

    /// <summary>
    /// Xử lý sự cố — Staff hoặc Admin.
    /// StatusId: 2=Đang xử lý, 3=Đã giải quyết.
    /// </summary>
    public async Task HandleAsync(
        int incidentId, HandleIncidentRequest request, int handledBy)
    {
        var incident = await _incidentRepo.GetByIdAsync(incidentId)
            ?? throw new NotFoundException("Sự cố", incidentId);

        if (incident.StatusId == (int)IncidentStatusEnum.Resolved)
            throw new BusinessException("Sự cố này đã được giải quyết rồi.", 400);

        incident.StatusId = request.StatusId;
        incident.HandledByUserId = handledBy;
        incident.HandledNote = request.HandledNote?.Trim();
        incident.HandledAt = DateTime.UtcNow;

        await _incidentRepo.UpdateAsync(incident);
    }
}
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
    private readonly IFieldRepository    _fieldRepo;

    public IncidentService(
        IIncidentRepository incidentRepo,
        IFieldRepository    fieldRepo)
    {
        _incidentRepo = incidentRepo;
        _fieldRepo    = fieldRepo;
    }

    public async Task<PagedResponse<IncidentResponse>> GetIncidentsAsync(
        int? fieldId, int? statusId, int page, int pageSize)
    {
        var (items, total) = await _incidentRepo.GetIncidentsAsync(
            fieldId, statusId, page, pageSize);

        return new PagedResponse<IncidentResponse>
        {
            Items      = items.Select(IncidentMapper.ToResponse).ToList(),
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize
        };
    }

    public async Task<IncidentResponse> GetByIdAsync(int incidentId)
    {
        var incident = await _incidentRepo.GetByIdAsync(incidentId)
            ?? throw new NotFoundException("Sự cố", incidentId);

        return IncidentMapper.ToResponse(incident);
    }

    public async Task<IncidentResponse> CreateAsync(
        CreateIncidentRequest request, int reportedBy)
    {
        // Xác nhận sân tồn tại trước khi ghi sự cố
        var field = await _fieldRepo.GetByIdAsync(request.FieldId)
            ?? throw new NotFoundException("Sân bóng", request.FieldId);

        if (field.IsDeleted)
            throw new BusinessException("Sân bóng đã bị xóa, không thể báo sự cố.", 400);

        var incident = new Incident
        {
            FieldId          = request.FieldId,
            ReportedByUserId = reportedBy,
            Title            = request.Title.Trim(),
            Description      = request.Description?.Trim(),
            ImageUrl         = request.ImageUrl,
            StatusId         = (int)IncidentStatusEnum.New,
            CreatedAt        = DateTime.UtcNow
        };

        var created = await _incidentRepo.CreateAsync(incident);
        return IncidentMapper.ToResponse(created);
    }

    public async Task HandleAsync(
        int incidentId, HandleIncidentRequest request, int handledBy)
    {
        var incident = await _incidentRepo.GetByIdAsync(incidentId)
            ?? throw new NotFoundException("Sự cố", incidentId);

        // Không cho phép đặt lại về trạng thái Mới (1)
        if (request.StatusId == (int)IncidentStatusEnum.New)
            throw new BusinessException(
                "Không thể chuyển sự cố về trạng thái 'Mới'.", 400);

        // Không xử lý lại sự cố đã hoàn tất (chỉ cảnh báo nhẹ nếu cần)
        if (incident.StatusId == (int)IncidentStatusEnum.Resolved &&
            request.StatusId  == (int)IncidentStatusEnum.Resolved)
            throw new BusinessException("Sự cố đã được đánh dấu là đã xử lý rồi.", 400);

        incident.StatusId    = request.StatusId;
        incident.HandledNote = request.HandledNote?.Trim();

        // Ghi nhận người xử lý và thời điểm xử lý lần đầu hoặc cập nhật
        incident.HandledByUserId = handledBy;
        incident.HandledAt       = DateTime.UtcNow;

        await _incidentRepo.UpdateAsync(incident);
    }
}

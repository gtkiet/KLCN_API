using KLCN_API.Data;
using KLCN_API.Models.Entities;
using KLCN_API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KLCN_API.Repositories;

public class IncidentRepository : IIncidentRepository
{
    private readonly SportPlusDbContext _ctx;

    public IncidentRepository(SportPlusDbContext ctx) => _ctx = ctx;

    public async Task<Incident?> GetByIdAsync(int incidentId)
        => await _ctx.Incidents
            .Include(i => i.Field)
            .Include(i => i.Status)
            .Include(i => i.ReportedByUser)
            .Include(i => i.HandledByUser)
            .FirstOrDefaultAsync(i => i.IncidentId == incidentId);

    public async Task<(List<Incident> Items, int TotalCount)> GetIncidentsAsync(
        int? fieldId, int? statusId, int page, int pageSize)
    {
        var query = _ctx.Incidents
            .Include(i => i.Field)
            .Include(i => i.Status)
            .Include(i => i.ReportedByUser)
            .Include(i => i.HandledByUser)
            .AsQueryable();

        if (fieldId.HasValue)
            query = query.Where(i => i.FieldId == fieldId.Value);

        if (statusId.HasValue)
            query = query.Where(i => i.StatusId == statusId.Value);

        query = query.OrderByDescending(i => i.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Incident> CreateAsync(Incident incident)
    {
        await _ctx.Incidents.AddAsync(incident);
        await _ctx.SaveChangesAsync();

        // Reload navigations để mapper dùng ngay
        await _ctx.Entry(incident).Reference(i => i.Field).LoadAsync();
        await _ctx.Entry(incident).Reference(i => i.Status).LoadAsync();
        await _ctx.Entry(incident).Reference(i => i.ReportedByUser).LoadAsync();

        return incident;
    }

    public async Task UpdateAsync(Incident incident)
    {
        _ctx.Incidents.Update(incident);
        await _ctx.SaveChangesAsync();
    }
}
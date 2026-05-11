//using KLCN_API.Data;
//using KLCN_API.Models.Entities;
//using KLCN_API.Repositories.Interfaces;
//using Microsoft.EntityFrameworkCore;

//namespace KLCN_API.Repositories;

//public class FieldRepository : IFieldRepository
//{
//    private readonly SportPlusDbContext _ctx;

//    public FieldRepository(SportPlusDbContext ctx) => _ctx = ctx;

//    public async Task<Field?> GetByIdAsync(int fieldId)
//        => await _ctx.Fields
//            .Include(f => f.Type)
//            .Include(f => f.Status)
//            .FirstOrDefaultAsync(f => f.FieldId == fieldId && !f.IsDeleted);

//    public async Task<(List<Field> Items, int TotalCount)> GetFieldsAsync(
//        string? search, int? typeId, int? statusId, int page, int pageSize)
//    {
//        var query = _ctx.Fields
//            .Include(f => f.Type)
//            .Include(f => f.Status)
//            .Where(f => !f.IsDeleted)
//            .AsQueryable();

//        if (!string.IsNullOrWhiteSpace(search))
//        {
//            var s = search.Trim().ToLower();
//            query = query.Where(f => f.Name.ToLower().Contains(s));
//        }

//        if (typeId.HasValue)
//            query = query.Where(f => f.TypeId == typeId.Value);

//        if (statusId.HasValue)
//            query = query.Where(f => f.StatusId == statusId.Value);

//        query = query.OrderBy(f => f.Name);

//        var totalCount = await query.CountAsync();
//        var items = await query
//            .Skip((page - 1) * pageSize)
//            .Take(pageSize)
//            .ToListAsync();

//        return (items, totalCount);
//    }

//    public async Task<Field> CreateAsync(Field field)
//    {
//        await _ctx.Fields.AddAsync(field);
//        await _ctx.SaveChangesAsync();
//        await _ctx.Entry(field).Reference(f => f.Type).LoadAsync();
//        await _ctx.Entry(field).Reference(f => f.Status).LoadAsync();
//        return field;
//    }

//    public async Task UpdateAsync(Field field)
//    {
//        field.UpdatedAt = DateTime.UtcNow;
//        _ctx.Fields.Update(field);
//        await _ctx.SaveChangesAsync();
//    }

//    public async Task SoftDeleteAsync(int fieldId)
//        => await _ctx.Fields
//            .Where(f => f.FieldId == fieldId)
//            .ExecuteUpdateAsync(s => s
//                .SetProperty(f => f.IsDeleted, true)
//                .SetProperty(f => f.UpdatedAt, DateTime.UtcNow));

//    // ── Slots ────────────────────────────────────────────────────

//    public async Task<List<FieldSlot>> GetScheduleAsync(int? fieldId, DateOnly date)
//    {
//        var query = _ctx.FieldSlots
//            .Include(fs => fs.Field).ThenInclude(f => f.Type)
//            .Include(fs => fs.TimeSlot)
//            .Include(fs => fs.Status)
//            .Where(fs => fs.SlotDate == date && !fs.Field.IsDeleted)
//            .AsQueryable();

//        if (fieldId.HasValue)
//            query = query.Where(fs => fs.FieldId == fieldId.Value);

//        return await query
//            .OrderBy(fs => fs.Field.Name)
//            .ThenBy(fs => fs.TimeSlot.StartTime)
//            .ToListAsync();
//    }

//    public async Task<FieldSlot?> GetSlotByIdAsync(int fieldSlotId)
//        => await _ctx.FieldSlots
//            .Include(fs => fs.Field)
//            .Include(fs => fs.TimeSlot)
//            .Include(fs => fs.Status)
//            .FirstOrDefaultAsync(fs => fs.FieldSlotId == fieldSlotId);

//    public async Task<List<FieldSlot>> GetSlotsByIdsAsync(List<int> fieldSlotIds)
//        => await _ctx.FieldSlots
//            .Include(fs => fs.Field)
//            .Include(fs => fs.TimeSlot)
//            .Include(fs => fs.Status)
//            .Where(fs => fieldSlotIds.Contains(fs.FieldSlotId))
//            .ToListAsync();

//    // ── Price History ────────────────────────────────────────────

//    public async Task<List<FieldPriceHistory>> GetPriceHistoryAsync(int fieldId)
//        => await _ctx.FieldPriceHistories
//            .Include(h => h.ChangedByUser)
//            .Where(h => h.FieldId == fieldId)
//            .OrderByDescending(h => h.ChangedAt)
//            .ToListAsync();

//    // ── Maintenance Logs ─────────────────────────────────────────

//    public async Task<List<FieldMaintenanceLog>> GetMaintenanceLogsAsync(int fieldId)
//        => await _ctx.FieldMaintenanceLogs
//            .Include(l => l.CreatedByUser)
//            .Where(l => l.FieldId == fieldId)
//            .OrderByDescending(l => l.StartDate)
//            .ToListAsync();

//    public async Task<FieldMaintenanceLog> AddMaintenanceLogAsync(FieldMaintenanceLog log)
//    {
//        await _ctx.FieldMaintenanceLogs.AddAsync(log);
//        await _ctx.SaveChangesAsync();
//        return log;
//    }
//}
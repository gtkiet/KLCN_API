using KLCN_API.Data;
using KLCN_API.Helpers;
using KLCN_API.Models.Entities;
using KLCN_API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KLCN_API.Repositories;

public class SystemConfigRepository : ISystemConfigRepository
{
    private readonly SportPlusDbContext _ctx;

    public SystemConfigRepository(SportPlusDbContext ctx) => _ctx = ctx;

    public async Task<List<SystemConfig>> GetAllAsync()
        => await _ctx.SystemConfigs
            .Include(c => c.UpdatedByUser)
            .OrderBy(c => c.ConfigKey)
            .ToListAsync();

    public async Task<SystemConfig?> GetByKeyAsync(string key)
        => await _ctx.SystemConfigs
            .Include(c => c.UpdatedByUser)
            .FirstOrDefaultAsync(c => c.ConfigKey == key);

    public async Task UpdateAsync(string key, string value, int updatedBy)
    {
        // sp_UpdateSystemConfig đảm bảo UpdatedAt và UpdatedBy được ghi log đúng
        await StoredProcedureHelper.UpdateSystemConfigAsync(_ctx, key, value, updatedBy);
    }
}

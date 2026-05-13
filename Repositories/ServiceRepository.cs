using KLCN_API.Data;
using KLCN_API.Models.Entities;
using KLCN_API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KLCN_API.Repositories;

public class ServiceRepository : IServiceRepository
{
    private readonly SportPlusDbContext _ctx;

    public ServiceRepository(SportPlusDbContext ctx) => _ctx = ctx;

    public async Task<Service?> GetByIdAsync(int serviceId)
        => await _ctx.Services
            .FirstOrDefaultAsync(s => s.ServiceId == serviceId && !s.IsDeleted);

    public async Task<List<Service>> GetAllAsync(bool? isAvailable = null)
    {
        var query = _ctx.Services
            .Where(s => !s.IsDeleted)
            .AsQueryable();

        if (isAvailable.HasValue)
            query = query.Where(s => s.IsAvailable == isAvailable.Value);

        return await query.OrderBy(s => s.Name).ToListAsync();
    }

    public async Task<Service> CreateAsync(Service service)
    {
        await _ctx.Services.AddAsync(service);
        await _ctx.SaveChangesAsync();
        return service;
    }

    public async Task UpdateAsync(Service service)
    {
        service.UpdatedAt = DateTime.UtcNow;
        _ctx.Services.Update(service);
        await _ctx.SaveChangesAsync();
    }

    public async Task SoftDeleteAsync(int serviceId)
        => await _ctx.Services
            .Where(s => s.ServiceId == serviceId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDeleted, true));
}
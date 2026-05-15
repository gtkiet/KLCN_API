using KLCN_API.Data;
using KLCN_API.Models.Entities;
using KLCN_API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KLCN_API.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private readonly SportPlusDbContext _ctx;

    public SupplierRepository(SportPlusDbContext ctx) => _ctx = ctx;

    public async Task<Supplier?> GetByIdAsync(int supplierId)
        => await _ctx.Suppliers
            .FirstOrDefaultAsync(s => s.SupplierId == supplierId && !s.IsDeleted);

    public async Task<(List<Supplier> Items, int TotalCount)> GetAllAsync(
        string? search, int page, int pageSize)
    {
        var query = _ctx.Suppliers
            .Where(s => !s.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(x =>
                x.Name.Contains(s) ||
                (x.ContactName != null && x.ContactName.Contains(s)) ||
                (x.Phone != null && x.Phone.Contains(s)) ||
                (x.Email != null && x.Email.Contains(s)));
        }

        query = query.OrderBy(s => s.Name);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<bool> NameExistsAsync(string name, int? excludeId = null)
        => await _ctx.Suppliers.AnyAsync(s =>
            s.Name == name &&
            !s.IsDeleted &&
            (excludeId == null || s.SupplierId != excludeId));

    public async Task<Supplier> CreateAsync(Supplier supplier)
    {
        await _ctx.Suppliers.AddAsync(supplier);
        await _ctx.SaveChangesAsync();
        return supplier;
    }

    public async Task UpdateAsync(Supplier supplier)
    {
        _ctx.Suppliers.Update(supplier);
        await _ctx.SaveChangesAsync();
    }

    public async Task SoftDeleteAsync(int supplierId)
        => await _ctx.Suppliers
            .Where(s => s.SupplierId == supplierId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDeleted, true));
}

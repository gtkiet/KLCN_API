using KLCN_API.Data;
using KLCN_API.Models.Entities;
using KLCN_API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KLCN_API.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly SportPlusDbContext _ctx;

    public ProductRepository(SportPlusDbContext ctx) => _ctx = ctx;

    public async Task<Product?> GetByIdAsync(int productId)
        => await _ctx.Products
            .FirstOrDefaultAsync(p => p.ProductId == productId && !p.IsDeleted);

    public async Task<(List<Product> Items, int TotalCount)> GetAllAsync(
        string? search, bool? lowStockOnly, int page, int pageSize)
    {
        var query = _ctx.Products
            .Where(p => !p.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(p => p.Name.Contains(s) || (p.Unit != null && p.Unit.Contains(s)));
        }

        // Lọc hàng sắp hết: StockQty <= MinQty
        if (lowStockOnly == true)
            query = query.Where(p => p.StockQty <= p.MinQty);

        query = query.OrderBy(p => p.Name);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<bool> NameExistsAsync(string name, int? excludeId = null)
        => await _ctx.Products.AnyAsync(p =>
            p.Name == name &&
            !p.IsDeleted &&
            (excludeId == null || p.ProductId != excludeId));

    public async Task<Product> CreateAsync(Product product)
    {
        await _ctx.Products.AddAsync(product);
        await _ctx.SaveChangesAsync();
        return product;
    }

    public async Task UpdateAsync(Product product)
    {
        _ctx.Products.Update(product);
        await _ctx.SaveChangesAsync();
    }

    public async Task SoftDeleteAsync(int productId)
        => await _ctx.Products
            .Where(p => p.ProductId == productId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDeleted, true));
}

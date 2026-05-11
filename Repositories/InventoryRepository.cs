//using KLCN_API.Data;
//using KLCN_API.Models.Entities;
//using KLCN_API.Repositories.Interfaces;
//using Microsoft.EntityFrameworkCore;

//namespace KLCN_API.Repositories;

//public class SupplierRepository : ISupplierRepository
//{
//    private readonly SportPlusDbContext _ctx;

//    public SupplierRepository(SportPlusDbContext ctx) => _ctx = ctx;

//    public async Task<Supplier?> GetByIdAsync(int supplierId)
//        => await _ctx.Suppliers
//            .FirstOrDefaultAsync(s => s.SupplierId == supplierId && !s.IsDeleted);

//    public async Task<(List<Supplier> Items, int TotalCount)> GetSuppliersAsync(
//        string? search, int page, int pageSize)
//    {
//        var query = _ctx.Suppliers
//            .Where(s => !s.IsDeleted)
//            .AsQueryable();

//        if (!string.IsNullOrWhiteSpace(search))
//        {
//            var s = search.Trim().ToLower();
//            query = query.Where(x =>
//                x.Name.ToLower().Contains(s) ||
//                (x.ContactName != null && x.ContactName.ToLower().Contains(s)) ||
//                (x.Phone != null && x.Phone.Contains(s)));
//        }

//        query = query.OrderBy(s => s.Name);

//        var totalCount = await query.CountAsync();
//        var items = await query
//            .Skip((page - 1) * pageSize)
//            .Take(pageSize)
//            .ToListAsync();

//        return (items, totalCount);
//    }

//    public async Task<Supplier> CreateAsync(Supplier supplier)
//    {
//        await _ctx.Suppliers.AddAsync(supplier);
//        await _ctx.SaveChangesAsync();
//        return supplier;
//    }

//    public async Task UpdateAsync(Supplier supplier)
//    {
//        _ctx.Suppliers.Update(supplier);
//        await _ctx.SaveChangesAsync();
//    }

//    public async Task SoftDeleteAsync(int supplierId)
//        => await _ctx.Suppliers
//            .Where(s => s.SupplierId == supplierId)
//            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDeleted, true));
//}

//public class ProductRepository : IProductRepository
//{
//    private readonly SportPlusDbContext _ctx;

//    public ProductRepository(SportPlusDbContext ctx) => _ctx = ctx;

//    public async Task<Product?> GetByIdAsync(int productId)
//        => await _ctx.Products
//            .FirstOrDefaultAsync(p => p.ProductId == productId && !p.IsDeleted);

//    public async Task<(List<Product> Items, int TotalCount)> GetProductsAsync(
//        string? search, int page, int pageSize)
//    {
//        var query = _ctx.Products
//            .Where(p => !p.IsDeleted)
//            .AsQueryable();

//        if (!string.IsNullOrWhiteSpace(search))
//        {
//            var s = search.Trim().ToLower();
//            query = query.Where(p => p.Name.ToLower().Contains(s));
//        }

//        query = query.OrderBy(p => p.Name);

//        var totalCount = await query.CountAsync();
//        var items = await query
//            .Skip((page - 1) * pageSize)
//            .Take(pageSize)
//            .ToListAsync();

//        return (items, totalCount);
//    }

//    public async Task<List<Product>> GetLowStockAsync()
//        => await _ctx.Products
//            .Where(p => !p.IsDeleted && p.StockQty <= p.MinQty)
//            .OrderBy(p => p.StockQty)
//            .ToListAsync();

//    public async Task<Product> CreateAsync(Product product)
//    {
//        await _ctx.Products.AddAsync(product);
//        await _ctx.SaveChangesAsync();
//        return product;
//    }

//    public async Task UpdateAsync(Product product)
//    {
//        _ctx.Products.Update(product);
//        await _ctx.SaveChangesAsync();
//    }

//    public async Task SoftDeleteAsync(int productId)
//        => await _ctx.Products
//            .Where(p => p.ProductId == productId)
//            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDeleted, true));
//}

//public class PurchaseOrderRepository : IPurchaseOrderRepository
//{
//    private readonly SportPlusDbContext _ctx;

//    public PurchaseOrderRepository(SportPlusDbContext ctx) => _ctx = ctx;

//    public async Task<PurchaseOrder?> GetByIdAsync(int purchaseOrderId)
//        => await _ctx.PurchaseOrders
//            .Include(po => po.Supplier)
//            .Include(po => po.CreatedByUser)
//            .Include(po => po.Status)
//            .FirstOrDefaultAsync(po => po.PurchaseOrderId == purchaseOrderId);

//    public async Task<PurchaseOrder?> GetWithDetailsAsync(int purchaseOrderId)
//        => await _ctx.PurchaseOrders
//            .Include(po => po.Supplier)
//            .Include(po => po.CreatedByUser)
//            .Include(po => po.Status)
//            .Include(po => po.Details).ThenInclude(d => d.Product)
//            .FirstOrDefaultAsync(po => po.PurchaseOrderId == purchaseOrderId);

//    public async Task<(List<PurchaseOrder> Items, int TotalCount)> GetPurchaseOrdersAsync(
//        int? statusId, int page, int pageSize)
//    {
//        var query = _ctx.PurchaseOrders
//            .Include(po => po.Supplier)
//            .Include(po => po.CreatedByUser)
//            .Include(po => po.Status)
//            .AsQueryable();

//        if (statusId.HasValue)
//            query = query.Where(po => po.StatusId == statusId.Value);

//        query = query.OrderByDescending(po => po.CreatedAt);

//        var totalCount = await query.CountAsync();
//        var items = await query
//            .Skip((page - 1) * pageSize)
//            .Take(pageSize)
//            .ToListAsync();

//        return (items, totalCount);
//    }

//    public async Task<PurchaseOrder> CreateAsync(PurchaseOrder order, List<PurchaseOrderDetail> details)
//    {
//        await _ctx.PurchaseOrders.AddAsync(order);
//        await _ctx.SaveChangesAsync();

//        foreach (var detail in details)
//        {
//            detail.PurchaseOrderId = order.PurchaseOrderId;
//            await _ctx.PurchaseOrderDetails.AddAsync(detail);
//        }

//        await _ctx.SaveChangesAsync();
//        return order;
//    }

//    public async Task UpdateStatusAsync(int purchaseOrderId, int statusId, DateTime? confirmedAt)
//        => await _ctx.PurchaseOrders
//            .Where(po => po.PurchaseOrderId == purchaseOrderId)
//            .ExecuteUpdateAsync(s => s
//                .SetProperty(po => po.StatusId, statusId)
//                .SetProperty(po => po.ConfirmedAt, confirmedAt));
//}
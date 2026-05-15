using KLCN_API.Data;
using KLCN_API.Models.Entities;
using KLCN_API.Models.Enums;
using KLCN_API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KLCN_API.Repositories;

public class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly SportPlusDbContext _ctx;

    public PurchaseOrderRepository(SportPlusDbContext ctx) => _ctx = ctx;

    public async Task<PurchaseOrder?> GetByIdAsync(int purchaseOrderId)
        => await _ctx.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.CreatedByUser)
            .Include(po => po.Status)
            .Include(po => po.Details)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(po => po.PurchaseOrderId == purchaseOrderId);

    public async Task<(List<PurchaseOrder> Items, int TotalCount)> GetAllAsync(
        int? supplierId, int? statusId, int page, int pageSize)
    {
        var query = _ctx.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.CreatedByUser)
            .Include(po => po.Status)
            .Include(po => po.Details)
                .ThenInclude(d => d.Product)
            .AsQueryable();

        if (supplierId.HasValue)
            query = query.Where(po => po.SupplierId == supplierId.Value);

        if (statusId.HasValue)
            query = query.Where(po => po.StatusId == statusId.Value);

        query = query.OrderByDescending(po => po.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<PurchaseOrder> CreateAsync(PurchaseOrder order, List<PurchaseOrderDetail> details)
    {
        await using var tx = await _ctx.Database.BeginTransactionAsync();

        await _ctx.PurchaseOrders.AddAsync(order);
        await _ctx.SaveChangesAsync(); // lấy PurchaseOrderId

        foreach (var d in details)
            d.PurchaseOrderId = order.PurchaseOrderId;

        await _ctx.PurchaseOrderDetails.AddRangeAsync(details);
        await _ctx.SaveChangesAsync();

        await tx.CommitAsync();

        // Reload navigations để mapper hoạt động đúng
        await _ctx.Entry(order).Reference(o => o.Supplier).LoadAsync();
        await _ctx.Entry(order).Reference(o => o.CreatedByUser).LoadAsync();
        await _ctx.Entry(order).Reference(o => o.Status).LoadAsync();
        foreach (var d in details)
            await _ctx.Entry(d).Reference(x => x.Product).LoadAsync();

        order.Details = details;
        return order;
    }

    public async Task CancelAsync(int purchaseOrderId)
        => await _ctx.PurchaseOrders
            .Where(po => po.PurchaseOrderId == purchaseOrderId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.StatusId, (int)PurchaseOrderStatusEnum.Cancelled));
}

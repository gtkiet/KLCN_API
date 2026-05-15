using KLCN_API.Data;
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

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly IPurchaseOrderRepository _poRepo;
    private readonly ISupplierRepository      _supplierRepo;
    private readonly IProductRepository       _productRepo;
    private readonly SportPlusDbContext        _ctx;

    public PurchaseOrderService(
        IPurchaseOrderRepository poRepo,
        ISupplierRepository      supplierRepo,
        IProductRepository       productRepo,
        SportPlusDbContext        ctx)
    {
        _poRepo       = poRepo;
        _supplierRepo = supplierRepo;
        _productRepo  = productRepo;
        _ctx          = ctx;
    }

    public async Task<PurchaseOrderResponse> GetByIdAsync(int purchaseOrderId)
    {
        var po = await _poRepo.GetByIdAsync(purchaseOrderId)
            ?? throw new NotFoundException("Đơn nhập kho", purchaseOrderId);

        return InventoryMapper.ToResponse(po);
    }

    public async Task<PagedResponse<PurchaseOrderResponse>> GetAllAsync(GetPurchaseOrdersRequest request)
    {
        var (items, total) = await _poRepo.GetAllAsync(
            request.SupplierId, request.StatusId, request.Page, request.PageSize);

        return new PagedResponse<PurchaseOrderResponse>
        {
            Items      = items.Select(InventoryMapper.ToResponse).ToList(),
            TotalCount = total,
            Page       = request.Page,
            PageSize   = request.PageSize
        };
    }

    public async Task<PurchaseOrderResponse> CreateAsync(
        CreatePurchaseOrderRequest request, int createdByUserId)
    {
        // Kiểm tra nhà cung cấp tồn tại
        var supplier = await _supplierRepo.GetByIdAsync(request.SupplierId)
            ?? throw new NotFoundException("Nhà cung cấp", request.SupplierId);

        // Kiểm tra không có ProductId trùng trong request
        var duplicateProduct = request.Items
            .GroupBy(i => i.ProductId)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateProduct is not null)
            throw new BusinessException(
                $"ProductId={duplicateProduct.Key} xuất hiện nhiều hơn 1 lần trong đơn hàng.", 400);

        // Kiểm tra tất cả sản phẩm tồn tại
        foreach (var item in request.Items)
        {
            var product = await _productRepo.GetByIdAsync(item.ProductId)
                ?? throw new NotFoundException("Sản phẩm", item.ProductId);
        }

        var order = new PurchaseOrder
        {
            SupplierId      = request.SupplierId,
            CreatedByUserId = createdByUserId,
            StatusId        = (int)PurchaseOrderStatusEnum.Pending,
            Note            = request.Note?.Trim(),
            CreatedAt       = DateTime.UtcNow
        };

        var details = request.Items.Select(i => new PurchaseOrderDetail
        {
            ProductId = i.ProductId,
            Quantity  = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList();

        var created = await _poRepo.CreateAsync(order, details);
        return InventoryMapper.ToResponse(created);
    }

    public async Task ConfirmAsync(int purchaseOrderId, int confirmedByUserId)
    {
        var po = await _poRepo.GetByIdAsync(purchaseOrderId)
            ?? throw new NotFoundException("Đơn nhập kho", purchaseOrderId);

        if (po.StatusId != (int)PurchaseOrderStatusEnum.Pending)
            throw new BusinessException(
                "Chỉ có thể xác nhận đơn nhập kho ở trạng thái Chờ xác nhận.", 400);

        if (!po.Details.Any())
            throw new BusinessException("Đơn nhập kho không có sản phẩm nào.", 400);

        // Gọi SP để cộng tồn kho và cập nhật trạng thái + TotalAmount + ConfirmedAt
        await StoredProcedureHelper.ConfirmPurchaseOrderAsync(_ctx, purchaseOrderId, confirmedByUserId);
    }

    public async Task CancelAsync(int purchaseOrderId)
    {
        var po = await _poRepo.GetByIdAsync(purchaseOrderId)
            ?? throw new NotFoundException("Đơn nhập kho", purchaseOrderId);

        if (po.StatusId == (int)PurchaseOrderStatusEnum.Confirmed)
            throw new BusinessException(
                "Không thể hủy đơn nhập kho đã được xác nhận (đã nhập kho).", 400);

        if (po.StatusId == (int)PurchaseOrderStatusEnum.Cancelled)
            throw new BusinessException("Đơn nhập kho đã bị hủy rồi.", 400);

        await _poRepo.CancelAsync(purchaseOrderId);
    }
}

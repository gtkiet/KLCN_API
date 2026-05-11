//using KLCN_API.Helpers;
//using KLCN_API.Middleware;
//using KLCN_API.Models.DTOs.Request;
//using KLCN_API.Models.DTOs.Response;
//using KLCN_API.Models.Entities;
//using KLCN_API.Repositories.Interfaces;
//using KLCN_API.Services.Interfaces;
//using Microsoft.Data.SqlClient;

//namespace KLCN_API.Services;

//// ================================================================
//// SupplierService
//// ================================================================

//public class SupplierService : ISupplierService
//{
//    private readonly ISupplierRepository _supplierRepo;

//    public SupplierService(ISupplierRepository supplierRepo) => _supplierRepo = supplierRepo;

//    public async Task<PagedResponse<SupplierResponse>> GetSuppliersAsync(
//        string? search, int page, int pageSize)
//    {
//        var (items, total) = await _supplierRepo.GetSuppliersAsync(search, page, pageSize);
//        return new PagedResponse<SupplierResponse>
//        {
//            Items = items.Select(Map).ToList(),
//            TotalCount = total,
//            Page = page,
//            PageSize = pageSize
//        };
//    }

//    public async Task<SupplierResponse> GetByIdAsync(int supplierId)
//    {
//        var supplier = await _supplierRepo.GetByIdAsync(supplierId)
//            ?? throw new NotFoundException("Nhà cung cấp", supplierId);
//        return Map(supplier);
//    }

//    public async Task<SupplierResponse> CreateAsync(CreateSupplierRequest request)
//    {
//        var supplier = new Supplier
//        {
//            Name = request.Name.Trim(),
//            ContactName = request.ContactName?.Trim(),
//            Phone = request.Phone?.Trim(),
//            Email = request.Email?.Trim().ToLower(),
//            Address = request.Address?.Trim()
//        };

//        var created = await _supplierRepo.CreateAsync(supplier);
//        return Map(created);
//    }

//    public async Task<SupplierResponse> UpdateAsync(int supplierId, UpdateSupplierRequest request)
//    {
//        var supplier = await _supplierRepo.GetByIdAsync(supplierId)
//            ?? throw new NotFoundException("Nhà cung cấp", supplierId);

//        if (request.Name is not null) supplier.Name = request.Name.Trim();
//        if (request.ContactName is not null) supplier.ContactName = request.ContactName.Trim();
//        if (request.Phone is not null) supplier.Phone = request.Phone.Trim();
//        if (request.Email is not null) supplier.Email = request.Email.Trim().ToLower();
//        if (request.Address is not null) supplier.Address = request.Address.Trim();

//        await _supplierRepo.UpdateAsync(supplier);
//        return Map(supplier);
//    }

//    public async Task DeleteAsync(int supplierId)
//    {
//        await _supplierRepo.GetByIdAsync(supplierId)
//            ?? throw new NotFoundException("Nhà cung cấp", supplierId);
//        await _supplierRepo.SoftDeleteAsync(supplierId);
//    }

//    private static SupplierResponse Map(Supplier s) => new()
//    {
//        SupplierId = s.SupplierId,
//        Name = s.Name,
//        ContactName = s.ContactName,
//        Phone = s.Phone,
//        Email = s.Email,
//        Address = s.Address
//    };
//}

//// ================================================================
//// ProductService
//// ================================================================

//public class ProductService : IProductService
//{
//    private readonly IProductRepository _productRepo;

//    public ProductService(IProductRepository productRepo) => _productRepo = productRepo;

//    public async Task<PagedResponse<ProductResponse>> GetProductsAsync(
//        string? search, int page, int pageSize)
//    {
//        var (items, total) = await _productRepo.GetProductsAsync(search, page, pageSize);
//        return new PagedResponse<ProductResponse>
//        {
//            Items = items.Select(Map).ToList(),
//            TotalCount = total,
//            Page = page,
//            PageSize = pageSize
//        };
//    }

//    public async Task<List<ProductResponse>> GetLowStockAsync()
//    {
//        var items = await _productRepo.GetLowStockAsync();
//        return items.Select(Map).ToList();
//    }

//    public async Task<ProductResponse> GetByIdAsync(int productId)
//    {
//        var product = await _productRepo.GetByIdAsync(productId)
//            ?? throw new NotFoundException("Sản phẩm", productId);
//        return Map(product);
//    }

//    public async Task<ProductResponse> CreateAsync(CreateProductRequest request)
//    {
//        var product = new Product
//        {
//            Name = request.Name.Trim(),
//            Unit = request.Unit?.Trim(),
//            StockQty = 0,
//            MinQty = request.MinQty
//        };

//        var created = await _productRepo.CreateAsync(product);
//        return Map(created);
//    }

//    public async Task UpdateAsync(int productId, Product product)
//    {
//        await _productRepo.GetByIdAsync(productId)
//            ?? throw new NotFoundException("Sản phẩm", productId);
//        await _productRepo.UpdateAsync(product);
//    }

//    private static ProductResponse Map(Product p) => new()
//    {
//        ProductId = p.ProductId,
//        Name = p.Name,
//        Unit = p.Unit,
//        StockQty = p.StockQty,
//        MinQty = p.MinQty
//    };
//}

//// ================================================================
//// PurchaseOrderService
//// ================================================================

//public class PurchaseOrderService : IPurchaseOrderService
//{
//    private readonly IPurchaseOrderRepository _poRepo;
//    private readonly ISupplierRepository _supplierRepo;
//    private readonly IProductRepository _productRepo;
//    private readonly StoredProcedureHelper _sp;

//    public PurchaseOrderService(
//        IPurchaseOrderRepository poRepo,
//        ISupplierRepository supplierRepo,
//        IProductRepository productRepo,
//        StoredProcedureHelper sp)
//    {
//        _poRepo = poRepo;
//        _supplierRepo = supplierRepo;
//        _productRepo = productRepo;
//        _sp = sp;
//    }

//    public async Task<PagedResponse<PurchaseOrderResponse>> GetPurchaseOrdersAsync(
//        int? statusId, int page, int pageSize)
//    {
//        var (items, total) = await _poRepo.GetPurchaseOrdersAsync(statusId, page, pageSize);
//        return new PagedResponse<PurchaseOrderResponse>
//        {
//            Items = items.Select(MapSummary).ToList(),
//            TotalCount = total,
//            Page = page,
//            PageSize = pageSize
//        };
//    }

//    public async Task<PurchaseOrderResponse> GetByIdAsync(int purchaseOrderId)
//    {
//        var po = await _poRepo.GetWithDetailsAsync(purchaseOrderId)
//            ?? throw new NotFoundException("Đơn nhập kho", purchaseOrderId);
//        return MapDetail(po);
//    }

//    public async Task<PurchaseOrderResponse> CreateAsync(
//        CreatePurchaseOrderRequest request, int createdBy)
//    {
//        await _supplierRepo.GetByIdAsync(request.SupplierId)
//            ?? throw new NotFoundException("Nhà cung cấp", request.SupplierId);

//        var order = new PurchaseOrder
//        {
//            SupplierId = request.SupplierId,
//            CreatedByUserId = createdBy,
//            StatusId = 1,    // Chờ xác nhận
//            Note = request.Note?.Trim(),
//            CreatedAt = DateTime.UtcNow
//        };

//        var details = request.Items.Select(i => new PurchaseOrderDetail
//        {
//            ProductId = i.ProductId,
//            Quantity = i.Quantity,
//            UnitPrice = i.UnitPrice
//        }).ToList();

//        var created = await _poRepo.CreateAsync(order, details);
//        return await GetByIdAsync(created.PurchaseOrderId);
//    }

//    public async Task ConfirmAsync(int purchaseOrderId, int confirmedBy)
//    {
//        await _poRepo.GetByIdAsync(purchaseOrderId)
//            ?? throw new NotFoundException("Đơn nhập kho", purchaseOrderId);

//        await _sp.ExecuteAsync("sp_ConfirmPurchaseOrder",
//            new SqlParameter("@PurchaseOrderId", purchaseOrderId),
//            new SqlParameter("@UserId", confirmedBy));
//    }

//    public async Task CancelAsync(int purchaseOrderId)
//    {
//        var po = await _poRepo.GetByIdAsync(purchaseOrderId)
//            ?? throw new NotFoundException("Đơn nhập kho", purchaseOrderId);

//        if (po.StatusId != 1)
//            throw new BusinessException("Chỉ hủy được đơn ở trạng thái Chờ xác nhận.", 400);

//        await _poRepo.UpdateStatusAsync(purchaseOrderId, 3, confirmedAt: null);
//    }

//    private static SupplierResponse MapSupplier(Supplier? s) => s is null ? new() : new()
//    {
//        SupplierId = s.SupplierId,
//        Name = s.Name,
//        ContactName = s.ContactName,
//        Phone = s.Phone,
//        Email = s.Email,
//        Address = s.Address
//    };

//    private static PurchaseOrderResponse MapSummary(PurchaseOrder po) => new()
//    {
//        PurchaseOrderId = po.PurchaseOrderId,
//        Supplier = MapSupplier(po.Supplier),
//        CreatedBy = po.CreatedByUser?.FullName ?? string.Empty,
//        Status = po.Status?.Name ?? string.Empty,
//        StatusId = po.StatusId,
//        TotalAmount = po.TotalAmount,
//        Note = po.Note,
//        CreatedAt = po.CreatedAt,
//        ConfirmedAt = po.ConfirmedAt
//    };

//    private static PurchaseOrderResponse MapDetail(PurchaseOrder po) => new()
//    {
//        PurchaseOrderId = po.PurchaseOrderId,
//        Supplier = MapSupplier(po.Supplier),
//        CreatedBy = po.CreatedByUser?.FullName ?? string.Empty,
//        Status = po.Status?.Name ?? string.Empty,
//        StatusId = po.StatusId,
//        TotalAmount = po.TotalAmount,
//        Note = po.Note,
//        CreatedAt = po.CreatedAt,
//        ConfirmedAt = po.ConfirmedAt,
//        Items = po.Details?.Select(d => new PurchaseOrderDetailResponse
//        {
//            ProductId = d.ProductId,
//            ProductName = d.Product?.Name ?? string.Empty,
//            Unit = d.Product?.Unit,
//            Quantity = d.Quantity,
//            UnitPrice = d.UnitPrice
//        }).ToList() ?? []
//    };
//}
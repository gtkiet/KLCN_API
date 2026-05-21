using KLCN_API.Mappers;
using KLCN_API.Middleware;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Entities;
using KLCN_API.Repositories.Interfaces;
using KLCN_API.Services.Interfaces;

namespace KLCN_API.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepo;

    public ProductService(IProductRepository productRepo) => _productRepo = productRepo;

    public async Task<ProductResponse> GetByIdAsync(int productId)
    {
        var product = await _productRepo.GetByIdAsync(productId)
            ?? throw new NotFoundException("Sản phẩm", productId);

        return InventoryMapper.ToResponse(product);
    }

    public async Task<PagedResponse<ProductResponse>> GetAllAsync(GetProductsRequest request)
    {
        var (items, total) = await _productRepo.GetAllAsync(
            request.Search, request.LowStockOnly, request.Page, request.PageSize);

        return new PagedResponse<ProductResponse>
        {
            Items = items.Select(InventoryMapper.ToResponse).ToList(),
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request)
    {
        if (await _productRepo.NameExistsAsync(request.Name))
            throw new ConflictException($"Sản phẩm với tên '{request.Name}' đã tồn tại.");

        var product = new Product
        {
            Name = request.Name.Trim(),
            Unit = request.Unit?.Trim(),
            StockQty = request.InitialStock,
            MinQty = request.MinQty
        };

        var created = await _productRepo.CreateAsync(product);
        return InventoryMapper.ToResponse(created);
    }

    public async Task<ProductResponse> UpdateAsync(int productId, UpdateProductRequest request)
    {
        var product = await _productRepo.GetByIdAsync(productId)
            ?? throw new NotFoundException("Sản phẩm", productId);

        if (request.Name is not null)
        {
            var trimmedName = request.Name.Trim();
            if (await _productRepo.NameExistsAsync(trimmedName, excludeId: productId))
                throw new ConflictException($"Sản phẩm với tên '{trimmedName}' đã tồn tại.");

            product.Name = trimmedName;
        }

        if (request.Unit is not null) product.Unit = request.Unit.Trim();
        if (request.MinQty is not null) product.MinQty = request.MinQty.Value;

        await _productRepo.UpdateAsync(product);
        return InventoryMapper.ToResponse(product);
    }

    public async Task DeleteAsync(int productId)
    {
        var product = await _productRepo.GetByIdAsync(productId)
            ?? throw new NotFoundException("Sản phẩm", productId);

        await _productRepo.SoftDeleteAsync(product.ProductId);
    }
}

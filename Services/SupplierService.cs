using KLCN_API.Mappers;
using KLCN_API.Middleware;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Entities;
using KLCN_API.Repositories.Interfaces;
using KLCN_API.Services.Interfaces;

namespace KLCN_API.Services;

public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _supplierRepo;

    public SupplierService(ISupplierRepository supplierRepo) => _supplierRepo = supplierRepo;

    public async Task<SupplierResponse> GetByIdAsync(int supplierId)
    {
        var supplier = await _supplierRepo.GetByIdAsync(supplierId)
            ?? throw new NotFoundException("Nhà cung cấp", supplierId);

        return InventoryMapper.ToResponse(supplier);
    }

    public async Task<PagedResponse<SupplierResponse>> GetAllAsync(GetSuppliersRequest request)
    {
        var (items, total) = await _supplierRepo.GetAllAsync(
            request.Search, request.Page, request.PageSize);

        return new PagedResponse<SupplierResponse>
        {
            Items = items.Select(InventoryMapper.ToResponse).ToList(),
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<SupplierResponse> CreateAsync(CreateSupplierRequest request)
    {
        if (await _supplierRepo.NameExistsAsync(request.Name))
            throw new ConflictException($"Nhà cung cấp với tên '{request.Name}' đã tồn tại.");

        var supplier = new Supplier
        {
            Name = request.Name.Trim(),
            ContactName = request.ContactName?.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim().ToLower(),
            Address = request.Address?.Trim()
        };

        var created = await _supplierRepo.CreateAsync(supplier);
        return InventoryMapper.ToResponse(created);
    }

    public async Task<SupplierResponse> UpdateAsync(int supplierId, UpdateSupplierRequest request)
    {
        var supplier = await _supplierRepo.GetByIdAsync(supplierId)
            ?? throw new NotFoundException("Nhà cung cấp", supplierId);

        if (request.Name is not null)
        {
            var trimmedName = request.Name.Trim();
            if (await _supplierRepo.NameExistsAsync(trimmedName, excludeId: supplierId))
                throw new ConflictException($"Nhà cung cấp với tên '{trimmedName}' đã tồn tại.");

            supplier.Name = trimmedName;
        }

        if (request.ContactName is not null) supplier.ContactName = request.ContactName.Trim();
        if (request.Phone is not null) supplier.Phone = request.Phone.Trim();
        if (request.Email is not null) supplier.Email = request.Email.Trim().ToLower();
        if (request.Address is not null) supplier.Address = request.Address.Trim();

        await _supplierRepo.UpdateAsync(supplier);
        return InventoryMapper.ToResponse(supplier);
    }

    public async Task DeleteAsync(int supplierId)
    {
        var supplier = await _supplierRepo.GetByIdAsync(supplierId)
            ?? throw new NotFoundException("Nhà cung cấp", supplierId);

        await _supplierRepo.SoftDeleteAsync(supplier.SupplierId);
    }
}

using KLCN_API.Filters;
using KLCN_API.Middleware;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Enums;
using KLCN_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KLCN_API.Controllers;

/// <summary>Quản lý nhà cung cấp — Admin và Staff.</summary>
[ApiController]
[Route("api/suppliers")]
[Authorize]
[AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
        => _supplierService = supplierService;

    /// <summary>Lấy danh sách nhà cung cấp có tìm kiếm + phân trang.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<SupplierResponse>>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] GetSuppliersRequest request)
    {
        var result = await _supplierService.GetAllAsync(request);
        return Ok(ApiResponse<PagedResponse<SupplierResponse>>.Ok(result));
    }

    /// <summary>Lấy chi tiết 1 nhà cung cấp.</summary>
    [HttpGet("{supplierId:int}")]
    [ProducesResponseType(typeof(ApiResponse<SupplierResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> GetById(int supplierId)
    {
        var result = await _supplierService.GetByIdAsync(supplierId);
        return Ok(ApiResponse<SupplierResponse>.Ok(result));
    }

    /// <summary>Tạo nhà cung cấp mới.</summary>
    [HttpPost]
    [AuthorizeRoles(RoleEnum.Admin)]
    [ProducesResponseType(typeof(ApiResponse<SupplierResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 409)]
    public async Task<IActionResult> Create([FromBody] CreateSupplierRequest request)
    {
        var result = await _supplierService.CreateAsync(request);
        return Ok(ApiResponse<SupplierResponse>.Ok(result, "Tạo nhà cung cấp thành công."));
    }

    /// <summary>Cập nhật thông tin nhà cung cấp.</summary>
    [HttpPut("{supplierId:int}")]
    [AuthorizeRoles(RoleEnum.Admin)]
    [ProducesResponseType(typeof(ApiResponse<SupplierResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 409)]
    public async Task<IActionResult> Update(int supplierId, [FromBody] UpdateSupplierRequest request)
    {
        var result = await _supplierService.UpdateAsync(supplierId, request);
        return Ok(ApiResponse<SupplierResponse>.Ok(result, "Cập nhật nhà cung cấp thành công."));
    }

    /// <summary>Xóa mềm nhà cung cấp — chỉ Admin.</summary>
    [HttpDelete("{supplierId:int}")]
    [AuthorizeRoles(RoleEnum.Admin)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> Delete(int supplierId)
    {
        await _supplierService.DeleteAsync(supplierId);
        return Ok(ApiResponse.Ok("Xóa nhà cung cấp thành công."));
    }
}

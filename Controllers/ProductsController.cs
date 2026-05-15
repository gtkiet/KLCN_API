using KLCN_API.Filters;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Enums;
using KLCN_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KLCN_API.Controllers;

/// <summary>Quản lý sản phẩm kho — Admin và Staff.</summary>
[ApiController]
[Route("api/products")]
[Authorize]
[AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
        => _productService = productService;

    /// <summary>Lấy danh sách sản phẩm — có thể lọc theo tên hoặc chỉ lấy hàng sắp hết.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ProductResponse>>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] GetProductsRequest request)
    {
        var result = await _productService.GetAllAsync(request);
        return Ok(ApiResponse<PagedResponse<ProductResponse>>.Ok(result));
    }

    /// <summary>Lấy chi tiết 1 sản phẩm.</summary>
    [HttpGet("{productId:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> GetById(int productId)
    {
        var result = await _productService.GetByIdAsync(productId);
        return Ok(ApiResponse<ProductResponse>.Ok(result));
    }

    /// <summary>Tạo sản phẩm mới — chỉ Admin.</summary>
    [HttpPost]
    [AuthorizeRoles(RoleEnum.Admin)]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 409)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        var result = await _productService.CreateAsync(request);
        return Ok(ApiResponse<ProductResponse>.Ok(result, "Tạo sản phẩm thành công."));
    }

    /// <summary>Cập nhật thông tin sản phẩm (tên, đơn vị, mức cảnh báo) — chỉ Admin.
    /// Tồn kho chỉ thay đổi qua PurchaseOrder, không chỉnh tay.</summary>
    [HttpPut("{productId:int}")]
    [AuthorizeRoles(RoleEnum.Admin)]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 409)]
    public async Task<IActionResult> Update(int productId, [FromBody] UpdateProductRequest request)
    {
        var result = await _productService.UpdateAsync(productId, request);
        return Ok(ApiResponse<ProductResponse>.Ok(result, "Cập nhật sản phẩm thành công."));
    }

    /// <summary>Xóa mềm sản phẩm — chỉ Admin.</summary>
    [HttpDelete("{productId:int}")]
    [AuthorizeRoles(RoleEnum.Admin)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> Delete(int productId)
    {
        await _productService.DeleteAsync(productId);
        return Ok(ApiResponse.Ok("Xóa sản phẩm thành công."));
    }
}

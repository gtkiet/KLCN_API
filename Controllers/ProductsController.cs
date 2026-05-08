//using KLCN_API.Filters;
//using KLCN_API.Models.DTOs.Request;
//using KLCN_API.Models.DTOs.Response;
//using KLCN_API.Models.Enums;
//using KLCN_API.Services.Interfaces;
//using Microsoft.AspNetCore.Mvc;

//namespace KLCN_API.Controllers;

//[ApiController]
//[Route("api/products")]
//[AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
//public class ProductsController : ControllerBase
//{
//    private readonly IInventoryService _inventoryService;

//    public ProductsController(IInventoryService inventoryService)
//    {
//        _inventoryService = inventoryService;
//    }

//    /// <summary>Lấy danh sách sản phẩm.</summary>
//    [HttpGet]
//    public async Task<IActionResult> GetProducts()
//    {
//        var result = await _inventoryService.GetProductsAsync();
//        return Ok(ApiResponse<List<ProductResponse>>.Ok(result));
//    }

//    /// <summary>Lấy sản phẩm sắp hết hàng.</summary>
//    [HttpGet("low-stock")]
//    public async Task<IActionResult> GetLowStock()
//    {
//        var result = await _inventoryService.GetLowStockProductsAsync();
//        return Ok(ApiResponse<List<ProductResponse>>.Ok(result));
//    }

//    /// <summary>Tạo sản phẩm — Admin.</summary>
//    [HttpPost]
//    [AuthorizeRoles(RoleEnum.Admin)]
//    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
//    {
//        var result = await _inventoryService.CreateProductAsync(request);
//        return Ok(ApiResponse<ProductResponse>.Ok(result, "Tạo sản phẩm thành công."));
//    }

//    /// <summary>Cập nhật sản phẩm — Admin.</summary>
//    [HttpPut("{productId:int}")]
//    [AuthorizeRoles(RoleEnum.Admin)]
//    public async Task<IActionResult> Update(int productId, [FromBody] UpdateProductRequest request)
//    {
//        var result = await _inventoryService.UpdateProductAsync(productId, request);
//        return Ok(ApiResponse<ProductResponse>.Ok(result, "Cập nhật sản phẩm thành công."));
//    }
//}
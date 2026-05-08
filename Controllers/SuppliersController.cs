//using KLCN_API.Filters;
//using KLCN_API.Models.DTOs.Request;
//using KLCN_API.Models.DTOs.Response;
//using KLCN_API.Models.Enums;
//using KLCN_API.Services.Interfaces;
//using Microsoft.AspNetCore.Mvc;

//namespace KLCN_API.Controllers;

//[ApiController]
//[Route("api/suppliers")]
//[AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
//public class SuppliersController : ControllerBase
//{
//    private readonly IInventoryService _inventoryService;

//    public SuppliersController(IInventoryService inventoryService)
//    {
//        _inventoryService = inventoryService;
//    }

//    /// <summary>Lấy danh sách nhà cung cấp.</summary>
//    [HttpGet]
//    public async Task<IActionResult> GetSuppliers()
//    {
//        var result = await _inventoryService.GetSuppliersAsync();
//        return Ok(ApiResponse<List<SupplierResponse>>.Ok(result));
//    }

//    /// <summary>Tạo nhà cung cấp — Admin.</summary>
//    [HttpPost]
//    [AuthorizeRoles(RoleEnum.Admin)]
//    public async Task<IActionResult> Create([FromBody] CreateSupplierRequest request)
//    {
//        var result = await _inventoryService.CreateSupplierAsync(request);
//        return Ok(ApiResponse<SupplierResponse>.Ok(result, "Tạo nhà cung cấp thành công."));
//    }

//    /// <summary>Cập nhật nhà cung cấp — Admin.</summary>
//    [HttpPut("{supplierId:int}")]
//    [AuthorizeRoles(RoleEnum.Admin)]
//    public async Task<IActionResult> Update(int supplierId, [FromBody] CreateSupplierRequest request)
//    {
//        var result = await _inventoryService.UpdateSupplierAsync(supplierId, request);
//        return Ok(ApiResponse<SupplierResponse>.Ok(result, "Cập nhật nhà cung cấp thành công."));
//    }

//    /// <summary>Xóa mềm nhà cung cấp — Admin.</summary>
//    [HttpDelete("{supplierId:int}")]
//    [AuthorizeRoles(RoleEnum.Admin)]
//    public async Task<IActionResult> Delete(int supplierId)
//    {
//        await _inventoryService.DeleteSupplierAsync(supplierId);
//        return Ok(ApiResponse.Ok("Xóa nhà cung cấp thành công."));
//    }
//}
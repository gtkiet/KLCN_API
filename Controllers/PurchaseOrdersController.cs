//using KLCN_API.Filters;
//using KLCN_API.Helpers;
//using KLCN_API.Models.DTOs.Request;
//using KLCN_API.Models.DTOs.Response;
//using KLCN_API.Models.Enums;
//using KLCN_API.Services.Interfaces;
//using Microsoft.AspNetCore.Mvc;

//namespace KLCN_API.Controllers;

//[ApiController]
//[Route("api/purchase-orders")]
//[AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
//public class PurchaseOrdersController : ControllerBase
//{
//    private readonly IInventoryService _inventoryService;

//    public PurchaseOrdersController(IInventoryService inventoryService)
//    {
//        _inventoryService = inventoryService;
//    }

//    /// <summary>Lấy danh sách đơn nhập kho.</summary>
//    [HttpGet]
//    public async Task<IActionResult> GetOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
//    {
//        var result = await _inventoryService.GetPurchaseOrdersAsync(page, pageSize);
//        return Ok(ApiResponse<PagedResponse<PurchaseOrderResponse>>.Ok(result));
//    }

//    /// <summary>Lấy chi tiết đơn nhập kho.</summary>
//    [HttpGet("{orderId:int}")]
//    public async Task<IActionResult> GetById(int orderId)
//    {
//        var result = await _inventoryService.GetPurchaseOrderByIdAsync(orderId);
//        return Ok(ApiResponse<PurchaseOrderResponse>.Ok(result));
//    }

//    /// <summary>Tạo đơn nhập kho.</summary>
//    [HttpPost]
//    public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderRequest request)
//    {
//        var userId = User.GetUserId();
//        var result = await _inventoryService.CreatePurchaseOrderAsync(request, userId);
//        return Ok(ApiResponse<PurchaseOrderResponse>.Ok(result, "Tạo đơn nhập kho thành công."));
//    }

//    /// <summary>Xác nhận đơn nhập kho — Admin.</summary>
//    [HttpPost("{orderId:int}/confirm")]
//    [AuthorizeRoles(RoleEnum.Admin)]
//    public async Task<IActionResult> Confirm(int orderId)
//    {
//        var userId = User.GetUserId();
//        await _inventoryService.ConfirmPurchaseOrderAsync(orderId, userId);
//        return Ok(ApiResponse.Ok("Xác nhận nhập kho thành công."));
//    }

//    /// <summary>Hủy đơn nhập kho — Admin.</summary>
//    [HttpDelete("{orderId:int}")]
//    [AuthorizeRoles(RoleEnum.Admin)]
//    public async Task<IActionResult> Cancel(int orderId)
//    {
//        await _inventoryService.CancelPurchaseOrderAsync(orderId);
//        return Ok(ApiResponse.Ok("Hủy đơn nhập kho thành công."));
//    }
//}
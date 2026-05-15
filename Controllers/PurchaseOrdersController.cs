using KLCN_API.Filters;
using KLCN_API.Helpers;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Enums;
using KLCN_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KLCN_API.Controllers;

/// <summary>Quản lý đơn nhập kho — Admin và Staff.</summary>
[ApiController]
[Route("api/purchase-orders")]
[Authorize]
[AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _poService;

    public PurchaseOrdersController(IPurchaseOrderService poService)
        => _poService = poService;

    /// <summary>Lấy danh sách đơn nhập kho — có thể lọc theo nhà cung cấp và trạng thái.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<PurchaseOrderResponse>>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] GetPurchaseOrdersRequest request)
    {
        var result = await _poService.GetAllAsync(request);
        return Ok(ApiResponse<PagedResponse<PurchaseOrderResponse>>.Ok(result));
    }

    /// <summary>Lấy chi tiết 1 đơn nhập kho kèm danh sách sản phẩm.</summary>
    [HttpGet("{purchaseOrderId:int}")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseOrderResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> GetById(int purchaseOrderId)
    {
        var result = await _poService.GetByIdAsync(purchaseOrderId);
        return Ok(ApiResponse<PurchaseOrderResponse>.Ok(result));
    }

    /// <summary>Tạo đơn nhập kho mới ở trạng thái Chờ xác nhận.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PurchaseOrderResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderRequest request)
    {
        var userId = User.GetUserId();
        var result = await _poService.CreateAsync(request, userId);
        return Ok(ApiResponse<PurchaseOrderResponse>.Ok(result, "Tạo đơn nhập kho thành công."));
    }

    /// <summary>Xác nhận nhập kho — cộng tồn kho, chuyển trạng thái sang Đã nhập.
    /// Gọi sp_ConfirmPurchaseOrder. Chỉ Admin.</summary>
    [HttpPatch("{purchaseOrderId:int}/confirm")]
    [AuthorizeRoles(RoleEnum.Admin)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 422)]
    public async Task<IActionResult> Confirm(int purchaseOrderId)
    {
        var userId = User.GetUserId();
        await _poService.ConfirmAsync(purchaseOrderId, userId);
        return Ok(ApiResponse.Ok("Xác nhận nhập kho thành công. Tồn kho đã được cập nhật."));
    }

    /// <summary>Hủy đơn nhập kho — chỉ được hủy khi còn ở trạng thái Chờ xác nhận.</summary>
    [HttpPatch("{purchaseOrderId:int}/cancel")]
    [AuthorizeRoles(RoleEnum.Admin)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> Cancel(int purchaseOrderId)
    {
        await _poService.CancelAsync(purchaseOrderId);
        return Ok(ApiResponse.Ok("Hủy đơn nhập kho thành công."));
    }
}

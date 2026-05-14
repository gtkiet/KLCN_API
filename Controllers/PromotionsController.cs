using KLCN_API.Filters;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Enums;
using KLCN_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KLCN_API.Controllers;

[ApiController]
[Route("api/promotions")]
public class PromotionsController : ControllerBase
{
    private readonly IPromotionService _promotionService;

    public PromotionsController(IPromotionService promotionService)
    {
        _promotionService = promotionService;
    }

    /// <summary>Lấy danh sách promotion — Admin và Staff.</summary>
    [HttpGet]
    [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
    public async Task<IActionResult> GetPromotions([FromQuery] GetPromotionRequest request)
    {
        var result = await _promotionService.GetPromotionsAsync(request);
        return Ok(ApiResponse<PagedResponse<PromotionResponse>>.Ok(result));
    }

    /// <summary>Kiểm tra voucher theo code — đã đăng nhập.</summary>
    [HttpGet("{code}")]
    [Authorize]
    public async Task<IActionResult> GetByCode(string code)
    {
        var result = await _promotionService.GetByCodeAsync(code);
        return Ok(ApiResponse<PromotionResponse>.Ok(result));
    }

    /// <summary>Tạo promotion — Admin.</summary>
    [HttpPost]
    [AuthorizeRoles(RoleEnum.Admin)]
    public async Task<IActionResult> Create(int adminId, [FromBody] CreatePromotionRequest request)
    {
        var result = await _promotionService.CreateAsync(adminId, request);
        return Ok(ApiResponse<PromotionResponse>.Ok(result, "Tạo promotion thành công."));
    }

    /// <summary>Cập nhật promotion — Admin.</summary>
    [HttpPut("{promotionId:int}")]
    [AuthorizeRoles(RoleEnum.Admin)]
    public async Task<IActionResult> Update(int promotionId, [FromBody] UpdatePromotionRequest request)
    {
        var result = await _promotionService.UpdateAsync(promotionId, request);
        return Ok(ApiResponse<PromotionResponse>.Ok(result, "Cập nhật promotion thành công."));
    }

    /// <summary>Bật/tắt promotion — Admin.</summary>
    [HttpPatch("{promotionId:int}/toggle")]
    [AuthorizeRoles(RoleEnum.Admin)]
    public async Task<IActionResult> Toggle(int promotionId)
    {
        await _promotionService.ToggleActiveAsync(promotionId);
        return Ok(ApiResponse.Ok("Cập nhật trạng thái promotion thành công."));
    }
}
//using KLCN_API.Filters;
//using KLCN_API.Helpers;
//using KLCN_API.Models.DTOs.Request;
//using KLCN_API.Models.DTOs.Response;
//using KLCN_API.Models.Enums;
//using KLCN_API.Services.Interfaces;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;

//namespace KLCN_API.Controllers;

//[ApiController]
//[Route("api/reviews")]
//public class ReviewsController : ControllerBase
//{
//    private readonly IReviewService _reviewService;

//    public ReviewsController(IReviewService reviewService)
//    {
//        _reviewService = reviewService;
//    }

//    /// <summary>Tạo review — Customer đã đăng nhập.</summary>
//    [HttpPost]
//    [AuthorizeRoles(RoleEnum.Customer)]
//    public async Task<IActionResult> Create([FromBody] CreateReviewRequest request)
//    {
//        var userId = User.GetUserId();
//        var result = await _reviewService.CreateReviewAsync(request, userId);
//        return Ok(ApiResponse<ReviewResponse>.Ok(result, "Đánh giá thành công."));
//    }

//    /// <summary>Lấy rating + reviews của sân — Public.</summary>
//    [HttpGet("field/{fieldId:int}")]
//    [AllowAnonymous]
//    public async Task<IActionResult> GetByField(
//        int fieldId,
//        [FromQuery] int page = 1,
//        [FromQuery] int pageSize = 10)
//    {
//        var result = await _reviewService.GetReviewsByFieldAsync(fieldId, page, pageSize);
//        return Ok(ApiResponse<PagedResponse<ReviewResponse>>.Ok(result));
//    }

//    /// <summary>Ẩn/hiện review — Admin.</summary>
//    [HttpPatch("{reviewId:int}/toggle")]
//    [AuthorizeRoles(RoleEnum.Admin)]
//    public async Task<IActionResult> Toggle(int reviewId)
//    {
//        await _reviewService.ToggleReviewVisibilityAsync(reviewId);
//        return Ok(ApiResponse.Ok("Cập nhật trạng thái review thành công."));
//    }
//}
using KLCN_API.Filters;
using KLCN_API.Helpers;
using KLCN_API.Middleware;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Enums;
using KLCN_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KLCN_API.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
        => _reviewService = reviewService;

    /// <summary>
    /// Lấy danh sách đánh giá — Public.
    /// isVisible=true: chỉ lấy review hiển thị (dùng cho khách).
    /// isVisible=null: lấy tất cả (Admin/Staff dùng để quản lý).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ReviewResponse>>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] GetReviewsRequest request)
    {
        var result = await _reviewService.GetReviewsAsync(request);
        return Ok(ApiResponse<PagedResponse<ReviewResponse>>.Ok(result));
    }

    /// <summary>
    /// Lấy thống kê rating của một sân kèm 50 review mới nhất.
    /// </summary>
    [HttpGet("field/{fieldId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<FieldRatingResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> GetFieldRating(int fieldId)
    {
        var result = await _reviewService.GetFieldRatingAsync(fieldId);
        return Ok(ApiResponse<FieldRatingResponse>.Ok(result));
    }

    /// <summary>
    /// Tạo đánh giá cho booking đã hoàn thành — Customer.
    /// Gửi multipart/form-data: bookingId, rating, comment (tuỳ chọn), image (tuỳ chọn).
    /// Mỗi booking chỉ được đánh giá một lần.
    /// </summary>
    [HttpPost]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<ReviewResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 403)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 409)]
    public async Task<IActionResult> Create([FromForm] CreateReviewRequest request)
    {
        var result = await _reviewService.CreateAsync(request, User.GetUserId());
        return StatusCode(201, ApiResponse<ReviewResponse>.Ok(
            result, "Đánh giá của bạn đã được ghi nhận."));
    }

    /// <summary>
    /// Ẩn / hiện đánh giá — Admin và Staff.
    /// </summary>
    [HttpPatch("{reviewId:int}/toggle-visibility")]
    [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> ToggleVisibility(int reviewId)
    {
        await _reviewService.ToggleVisibilityAsync(reviewId);
        return Ok(ApiResponse.Ok("Cập nhật trạng thái hiển thị thành công."));
    }
}
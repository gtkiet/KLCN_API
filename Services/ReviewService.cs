using KLCN_API.Helpers;
using KLCN_API.Mappers;
using KLCN_API.Middleware;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Entities;
using KLCN_API.Models.Enums;
using KLCN_API.Repositories.Interfaces;
using KLCN_API.Services.Interfaces;

namespace KLCN_API.Services;

public class ReviewService : IReviewService
{
    private readonly IReviewRepository  _reviewRepo;
    // IBookingRepository được implement ở Group 5 (Bookings + Payments).
    // Đã đăng ký cùng AddRepositories — ReviewService có thể inject ngay.
    private readonly IBookingRepository _bookingRepo;

    public ReviewService(
        IReviewRepository  reviewRepo,
        IBookingRepository bookingRepo)
    {
        _reviewRepo  = reviewRepo;
        _bookingRepo = bookingRepo;
    }

    public async Task<PagedResponse<ReviewResponse>> GetReviewsAsync(
        GetReviewsRequest request)
    {
        var (items, total) = await _reviewRepo.GetReviewsAsync(
            request.FieldId, request.Rating, request.IsVisible,
            request.Page, request.PageSize);

        return new PagedResponse<ReviewResponse>
        {
            Items      = items.Select(ReviewMapper.ToResponse).ToList(),
            TotalCount = total,
            Page       = request.Page,
            PageSize   = request.PageSize
        };
    }

    public async Task<FieldRatingResponse> GetFieldRatingAsync(int fieldId)
    {
        // Lấy dữ liệu aggregate từ view
        var raw = await _reviewRepo.GetFieldRatingRawAsync(fieldId)
            ?? throw new NotFoundException("Sân bóng", fieldId);

        // Lấy danh sách review hiển thị của sân (không phân trang để embed vào response)
        var (reviews, _) = await _reviewRepo.GetReviewsAsync(
            fieldId, rating: null, isVisible: true, page: 1, pageSize: 50);

        return new FieldRatingResponse
        {
            FieldId      = raw.FieldId,
            FieldName    = raw.FieldName,
            FieldType    = raw.FieldType,
            AvgRating    = raw.AvgRating,
            TotalReviews = raw.TotalReviews,
            Stars5       = raw.Stars5,
            Stars4       = raw.Stars4,
            Stars3       = raw.Stars3,
            Stars2       = raw.Stars2,
            Stars1       = raw.Stars1,
            Reviews      = reviews.Select(ReviewMapper.ToResponse).ToList()
        };
    }

    public async Task<ReviewResponse> CreateAsync(
        CreateReviewRequest request, int userId)
    {
        // ── 1. Validate booking tồn tại và thuộc user này ───────────
        var booking = await _bookingRepo.GetWithDetailsAsync(request.BookingId)
            ?? throw new NotFoundException("Booking", request.BookingId);

        if (booking.UserId != userId)
            throw new ForbiddenException("Bạn không có quyền đánh giá booking này.");

        // ── 2. Chỉ cho phép đánh giá booking đã hoàn thành ──────────
        if (booking.StatusId != (int)BookingStatusEnum.Completed)
            throw new BusinessException(
                "Chỉ có thể đánh giá booking có trạng thái 'Đã hoàn thành'.", 400);

        // ── 3. Kiểm tra đã review chưa (unique constraint BookingId) ─
        var existing = await _reviewRepo.GetByBookingAsync(request.BookingId);
        if (existing is not null)
            throw new ConflictException("Booking này đã được đánh giá rồi.");

        // ── 4. Lấy FieldId từ BookingDetail đầu tiên ─────────────────
        // Một booking có thể đặt nhiều slot nhưng cùng một sân.
        var fieldId = booking.BookingDetails
            .FirstOrDefault()?.FieldSlot?.FieldId
            ?? throw new BusinessException(
                "Không tìm thấy thông tin sân trong booking.", 400);

        // ── 5. Tạo review ─────────────────────────────────────────────
        var review = new Review
        {
            BookingId = request.BookingId,
            UserId    = userId,
            FieldId   = fieldId,
            Rating    = (byte)request.Rating,
            Comment   = request.Comment?.Trim(),
            ImageUrl  = request.ImageUrl,
            IsVisible = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _reviewRepo.CreateAsync(review);
        return ReviewMapper.ToResponse(created);
    }

    public async Task ToggleVisibilityAsync(int reviewId)
    {
        var review = await _reviewRepo.GetByIdAsync(reviewId)
            ?? throw new NotFoundException("Đánh giá", reviewId);

        // Đảo trạng thái hiển thị
        await _reviewRepo.UpdateVisibilityAsync(reviewId, !review.IsVisible);
    }
}

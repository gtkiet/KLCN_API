//using KLCN_API.Middleware;
//using KLCN_API.Models.DTOs.Request;
//using KLCN_API.Models.DTOs.Response;
//using KLCN_API.Models.Entities;
//using KLCN_API.Repositories.Interfaces;
//using KLCN_API.Services.Interfaces;

//namespace KLCN_API.Services;

//// ================================================================
//// IncidentService
//// ================================================================

//public class IncidentService : IIncidentService
//{
//    private readonly IIncidentRepository _incidentRepo;

//    public IncidentService(IIncidentRepository incidentRepo) => _incidentRepo = incidentRepo;

//    public async Task<PagedResponse<IncidentResponse>> GetIncidentsAsync(
//        int? fieldId, int? statusId, int page, int pageSize)
//    {
//        var (items, total) = await _incidentRepo.GetIncidentsAsync(fieldId, statusId, page, pageSize);
//        return new PagedResponse<IncidentResponse>
//        {
//            Items = items.Select(Map).ToList(),
//            TotalCount = total,
//            Page = page,
//            PageSize = pageSize
//        };
//    }

//    public async Task<IncidentResponse> GetByIdAsync(int incidentId)
//    {
//        var incident = await _incidentRepo.GetByIdAsync(incidentId)
//            ?? throw new NotFoundException("Sự cố", incidentId);
//        return Map(incident);
//    }

//    public async Task<IncidentResponse> CreateAsync(CreateIncidentRequest request, int reportedBy)
//    {
//        var incident = new Incident
//        {
//            FieldId = request.FieldId,
//            ReportedByUserId = reportedBy,
//            Title = request.Title.Trim(),
//            Description = request.Description?.Trim(),
//            ImageUrl = request.ImageUrl,
//            StatusId = 1,   // Mới
//            CreatedAt = DateTime.UtcNow
//        };

//        var created = await _incidentRepo.CreateAsync(incident);
//        return Map(created);
//    }

//    public async Task HandleAsync(int incidentId, HandleIncidentRequest request, int handledBy)
//    {
//        var incident = await _incidentRepo.GetByIdAsync(incidentId)
//            ?? throw new NotFoundException("Sự cố", incidentId);

//        if (incident.StatusId == 3)
//            throw new BusinessException("Sự cố này đã được xử lý.", 400);

//        if (request.StatusId is not (2 or 3))
//            throw new BusinessException("StatusId phải là 2 (Đang xử lý) hoặc 3 (Đã xử lý).", 400);

//        incident.StatusId = request.StatusId;
//        incident.HandledByUserId = handledBy;
//        incident.HandledNote = request.HandledNote?.Trim();

//        if (request.StatusId == 3)
//            incident.HandledAt = DateTime.UtcNow;

//        await _incidentRepo.UpdateAsync(incident);
//    }

//    private static IncidentResponse Map(Incident i) => new()
//    {
//        IncidentId = i.IncidentId,
//        FieldId = i.FieldId,
//        FieldName = i.Field?.Name ?? string.Empty,
//        ReportedBy = i.ReportedByUser?.FullName ?? string.Empty,
//        Title = i.Title,
//        Description = i.Description,
//        ImageUrl = i.ImageUrl,
//        Status = i.Status?.Name ?? string.Empty,
//        StatusId = i.StatusId,
//        HandledBy = i.HandledByUser?.FullName,
//        HandledAt = i.HandledAt,
//        HandledNote = i.HandledNote,
//        CreatedAt = i.CreatedAt
//    };
//}

//// ================================================================
//// ReviewService
//// ================================================================

//public class ReviewService : IReviewService
//{
//    private readonly IReviewRepository _reviewRepo;
//    private readonly IBookingRepository _bookingRepo;

//    public ReviewService(IReviewRepository reviewRepo, IBookingRepository bookingRepo)
//    {
//        _reviewRepo = reviewRepo;
//        _bookingRepo = bookingRepo;
//    }

//    public async Task<PagedResponse<ReviewResponse>> GetReviewsAsync(GetReviewsRequest request)
//    {
//        var (items, total) = await _reviewRepo.GetReviewsAsync(
//            request.FieldId, request.Rating, request.IsVisible,
//            request.Page, request.PageSize);

//        return new PagedResponse<ReviewResponse>
//        {
//            Items = items.Select(Map).ToList(),
//            TotalCount = total,
//            Page = request.Page,
//            PageSize = request.PageSize
//        };
//    }

//    public async Task<FieldRatingResponse> GetFieldRatingAsync(int fieldId)
//    {
//        var (items, _) = await _reviewRepo.GetReviewsAsync(
//            fieldId, rating: null, isVisible: true, page: 1, pageSize: int.MaxValue);

//        return new FieldRatingResponse
//        {
//            FieldId = fieldId,
//            FieldName = items.FirstOrDefault()?.FieldName ?? string.Empty,
//            FieldType = string.Empty,
//            TotalReviews = items.Count,
//            AvgRating = items.Count > 0
//                ? Math.Round((decimal)items.Average(r => r.Rating), 1)
//                : 0,
//            Stars5 = items.Count(r => r.Rating == 5),
//            Stars4 = items.Count(r => r.Rating == 4),
//            Stars3 = items.Count(r => r.Rating == 3),
//            Stars2 = items.Count(r => r.Rating == 2),
//            Stars1 = items.Count(r => r.Rating == 1),
//            Reviews = items
//        };
//    }

//    public async Task<ReviewResponse> CreateAsync(CreateReviewRequest request, int userId)
//    {
//        // Chỉ được review booking Đã hoàn thành của chính mình
//        var booking = await _bookingRepo.GetByIdAsync(request.BookingId)
//            ?? throw new NotFoundException("Booking", request.BookingId);

//        if (booking.UserId != userId)
//            throw new ForbiddenException("Bạn không có quyền đánh giá booking này.");

//        if (booking.StatusId != 4)
//            throw new BusinessException("Chỉ có thể đánh giá booking đã hoàn thành.", 400);

//        if (await _reviewRepo.GetByBookingAsync(request.BookingId) is not null)
//            throw new BusinessException("Booking này đã được đánh giá.", 400);

//        // Lấy FieldId từ slot đầu tiên
//        var detail = booking.Details?.FirstOrDefault()
//            ?? throw new BusinessException("Booking không có thông tin sân.", 400);
//        var fieldId = detail.FieldSlot.FieldId;

//        var review = new Review
//        {
//            BookingId = request.BookingId,
//            UserId = userId,
//            FieldId = fieldId,
//            Rating = (byte)request.Rating,
//            Comment = request.Comment?.Trim(),
//            ImageUrl = request.ImageUrl,
//            IsVisible = true,
//            CreatedAt = DateTime.UtcNow,
//            UpdatedAt = DateTime.UtcNow
//        };

//        var created = await _reviewRepo.CreateAsync(review);
//        return Map(created);
//    }

//    public async Task ToggleVisibilityAsync(int reviewId)
//    {
//        var review = await _reviewRepo.GetByIdAsync(reviewId)
//            ?? throw new NotFoundException("Review", reviewId);

//        await _reviewRepo.UpdateVisibilityAsync(reviewId, !review.IsVisible);
//    }

//    private static ReviewResponse Map(Review r) => new()
//    {
//        ReviewId = r.ReviewId,
//        BookingId = r.BookingId,
//        UserId = r.UserId,
//        UserName = r.User?.FullName ?? string.Empty,
//        AvatarUrl = r.User?.Profile?.AvatarUrl,
//        FieldId = r.FieldId,
//        FieldName = r.Field?.Name ?? string.Empty,
//        Rating = r.Rating,
//        Comment = r.Comment,
//        ImageUrl = r.ImageUrl,
//        IsVisible = r.IsVisible,
//        CreatedAt = r.CreatedAt
//    };
//}
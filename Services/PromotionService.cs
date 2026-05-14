using KLCN_API.Middleware;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Entities;
using KLCN_API.Repositories.Interfaces;
using KLCN_API.Services.Interfaces;

namespace KLCN_API.Services;

// ================================================================
// PromotionService
// ================================================================

public class PromotionService : IPromotionService
{
    private readonly IPromotionRepository _promoRepo;

    public PromotionService(IPromotionRepository promoRepo) => _promoRepo = promoRepo;

    public async Task<PagedResponse<PromotionResponse>> GetPromotionsAsync(
        bool? isActive, int page, int pageSize)
    {
        var (items, total) = await _promoRepo.GetPromotionsAsync(isActive, page, pageSize);
        return new PagedResponse<PromotionResponse>
        {
            Items = items.Select(Map).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PromotionResponse> GetByIdAsync(int promotionId)
    {
        var promo = await _promoRepo.GetByIdAsync(promotionId)
            ?? throw new NotFoundException("Khuyến mãi", promotionId);
        return Map(promo);
    }

    public async Task<PromotionResponse> CreateAsync(int adminId, CreatePromotionRequest request)
    {
        if (request.EndDate < request.StartDate)
            throw new BusinessException("Ngày kết thúc phải sau ngày bắt đầu.", 400);

        var promo = new Promotion
        {
            Code = request.Code.Trim().ToUpper(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            TypeId = request.TypeId,
            DiscountValue = request.DiscountValue,
            MaxDiscount = request.MaxDiscount,
            MinOrderAmount = request.MinOrderAmount,
            UsageLimit = request.UsageLimit,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = true,
            CreatedBy = adminId,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _promoRepo.CreateAsync(promo);
        return Map(created);
    }

    public async Task<PromotionResponse> UpdateAsync(int promotionId, UpdatePromotionRequest request)
    {
        var promo = await _promoRepo.GetByIdAsync(promotionId)
            ?? throw new NotFoundException("Khuyến mãi", promotionId);

        if (request.Name is not null) promo.Name = request.Name.Trim();
        if (request.Description is not null) promo.Description = request.Description.Trim();
        if (request.DiscountValue.HasValue) promo.DiscountValue = request.DiscountValue.Value;
        if (request.MaxDiscount.HasValue) promo.MaxDiscount = request.MaxDiscount;
        if (request.MinOrderAmount.HasValue) promo.MinOrderAmount = request.MinOrderAmount.Value;
        if (request.UsageLimit.HasValue) promo.UsageLimit = request.UsageLimit.Value;
        if (request.StartDate.HasValue) promo.StartDate = request.StartDate.Value;
        if (request.EndDate.HasValue) promo.EndDate = request.EndDate.Value;
        if (request.IsActive.HasValue) promo.IsActive = request.IsActive.Value;

        if (promo.EndDate < promo.StartDate)
            throw new BusinessException("Ngày kết thúc phải sau ngày bắt đầu.", 400);

        await _promoRepo.UpdateAsync(promo);
        return Map(promo);
    }

    public async Task ToggleActiveAsync(int promotionId)
    {
        var promo = await _promoRepo.GetByIdAsync(promotionId)
            ?? throw new NotFoundException("Khuyến mãi", promotionId);

        promo.IsActive = !promo.IsActive;
        await _promoRepo.UpdateAsync(promo);
    }

    private static PromotionResponse Map(Promotion p) => new()
    {
        PromotionId = p.PromotionId,
        Code = p.Code,
        Name = p.Name,
        Description = p.Description,
        Type = p.Type?.Name ?? string.Empty,
        TypeId = p.TypeId,
        DiscountValue = p.DiscountValue,
        MaxDiscount = p.MaxDiscount,
        MinOrderAmount = p.MinOrderAmount,
        UsageLimit = p.UsageLimit,
        UsageCount = p.UsageCount,
        StartDate = p.StartDate,
        EndDate = p.EndDate,
        IsActive = p.IsActive,
        CreatedAt = p.CreatedAt
    };

    public async Task<PromotionResponse> GetByCodeAsync(string code)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)); // giờ VN

        var promo = await _promoRepo.GetActiveByCodeAsync(code.Trim().ToUpper());

        if (promo is null)
            throw new NotFoundException("Mã khuyến mãi không tồn tại, đã hết hạn hoặc đã dùng hết.");

        return Map(promo);
    }
}

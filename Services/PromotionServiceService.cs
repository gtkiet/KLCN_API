//using KLCN_API.Middleware;
//using KLCN_API.Models.DTOs.Request;
//using KLCN_API.Models.DTOs.Response;
//using KLCN_API.Models.Entities;
//using KLCN_API.Repositories.Interfaces;
//using KLCN_API.Services.Interfaces;

//namespace KLCN_API.Services;

//// ================================================================
//// PromotionService
//// ================================================================

//public class PromotionService : IPromotionService
//{
//    private readonly IPromotionRepository _promoRepo;

//    public PromotionService(IPromotionRepository promoRepo) => _promoRepo = promoRepo;

//    public async Task<PagedResponse<PromotionResponse>> GetPromotionsAsync(
//        bool? isActive, int page, int pageSize)
//    {
//        var (items, total) = await _promoRepo.GetPromotionsAsync(isActive, page, pageSize);
//        return new PagedResponse<PromotionResponse>
//        {
//            Items = items.Select(Map).ToList(),
//            TotalCount = total,
//            Page = page,
//            PageSize = pageSize
//        };
//    }

//    public async Task<PromotionResponse> GetByIdAsync(int promotionId)
//    {
//        var promo = await _promoRepo.GetByIdAsync(promotionId)
//            ?? throw new NotFoundException("Khuyến mãi", promotionId);
//        return Map(promo);
//    }

//    public async Task<PromotionResponse> CreateAsync(int adminId, CreatePromotionRequest request)
//    {
//        if (request.EndDate < request.StartDate)
//            throw new BusinessException("Ngày kết thúc phải sau ngày bắt đầu.", 400);

//        var promo = new Promotion
//        {
//            Code = request.Code.Trim().ToUpper(),
//            Name = request.Name.Trim(),
//            Description = request.Description?.Trim(),
//            TypeId = request.TypeId,
//            DiscountValue = request.DiscountValue,
//            MaxDiscount = request.MaxDiscount,
//            MinOrderAmount = request.MinOrderAmount,
//            UsageLimit = request.UsageLimit,
//            StartDate = request.StartDate,
//            EndDate = request.EndDate,
//            IsActive = true,
//            CreatedBy = adminId,
//            CreatedAt = DateTime.UtcNow
//        };

//        var created = await _promoRepo.CreateAsync(promo);
//        return Map(created);
//    }

//    public async Task<PromotionResponse> UpdateAsync(int promotionId, UpdatePromotionRequest request)
//    {
//        var promo = await _promoRepo.GetByIdAsync(promotionId)
//            ?? throw new NotFoundException("Khuyến mãi", promotionId);

//        if (request.Name is not null) promo.Name = request.Name.Trim();
//        if (request.Description is not null) promo.Description = request.Description.Trim();
//        if (request.DiscountValue.HasValue) promo.DiscountValue = request.DiscountValue.Value;
//        if (request.MaxDiscount.HasValue) promo.MaxDiscount = request.MaxDiscount;
//        if (request.MinOrderAmount.HasValue) promo.MinOrderAmount = request.MinOrderAmount.Value;
//        if (request.UsageLimit.HasValue) promo.UsageLimit = request.UsageLimit.Value;
//        if (request.StartDate.HasValue) promo.StartDate = request.StartDate.Value;
//        if (request.EndDate.HasValue) promo.EndDate = request.EndDate.Value;
//        if (request.IsActive.HasValue) promo.IsActive = request.IsActive.Value;

//        if (promo.EndDate < promo.StartDate)
//            throw new BusinessException("Ngày kết thúc phải sau ngày bắt đầu.", 400);

//        await _promoRepo.UpdateAsync(promo);
//        return Map(promo);
//    }

//    public async Task ToggleActiveAsync(int promotionId)
//    {
//        var promo = await _promoRepo.GetByIdAsync(promotionId)
//            ?? throw new NotFoundException("Khuyến mãi", promotionId);

//        promo.IsActive = !promo.IsActive;
//        await _promoRepo.UpdateAsync(promo);
//    }

//    private static PromotionResponse Map(Promotion p) => new()
//    {
//        PromotionId = p.PromotionId,
//        Code = p.Code,
//        Name = p.Name,
//        Description = p.Description,
//        Type = p.Type?.Name ?? string.Empty,
//        TypeId = p.TypeId,
//        DiscountValue = p.DiscountValue,
//        MaxDiscount = p.MaxDiscount,
//        MinOrderAmount = p.MinOrderAmount,
//        UsageLimit = p.UsageLimit,
//        UsageCount = p.UsageCount,
//        StartDate = p.StartDate,
//        EndDate = p.EndDate,
//        IsActive = p.IsActive,
//        CreatedAt = p.CreatedAt
//    };
//}

//// ================================================================
//// ServiceService
//// ================================================================

//public class ServiceService : IServiceService
//{
//    private readonly IServiceRepository _serviceRepo;

//    public ServiceService(IServiceRepository serviceRepo) => _serviceRepo = serviceRepo;

//    public async Task<List<ServiceResponse>> GetAllAsync(bool? isAvailable)
//    {
//        var items = await _serviceRepo.GetAllAsync(isAvailable);
//        return items.Select(Map).ToList();
//    }

//    public async Task<ServiceResponse> GetByIdAsync(int serviceId)
//    {
//        var svc = await _serviceRepo.GetByIdAsync(serviceId)
//            ?? throw new NotFoundException("Dịch vụ", serviceId);
//        return Map(svc);
//    }

//    public async Task<ServiceResponse> CreateAsync(CreateServiceRequest request)
//    {
//        var svc = new Service
//        {
//            Name = request.Name.Trim(),
//            Description = request.Description?.Trim(),
//            Price = request.Price,
//            ImageUrl = request.ImageUrl,
//            IsAvailable = true,
//            UpdatedAt = DateTime.UtcNow
//        };

//        var created = await _serviceRepo.CreateAsync(svc);
//        return Map(created);
//    }

//    public async Task<ServiceResponse> UpdateAsync(int serviceId, UpdateServiceRequest request)
//    {
//        var svc = await _serviceRepo.GetByIdAsync(serviceId)
//            ?? throw new NotFoundException("Dịch vụ", serviceId);

//        if (request.Name is not null) svc.Name = request.Name.Trim();
//        if (request.Description is not null) svc.Description = request.Description.Trim();
//        if (request.Price.HasValue) svc.Price = request.Price.Value;
//        if (request.ImageUrl is not null) svc.ImageUrl = request.ImageUrl;
//        if (request.IsAvailable.HasValue) svc.IsAvailable = request.IsAvailable.Value;

//        await _serviceRepo.UpdateAsync(svc);
//        return Map(svc);
//    }

//    public async Task DeleteAsync(int serviceId)
//    {
//        await _serviceRepo.GetByIdAsync(serviceId)
//            ?? throw new NotFoundException("Dịch vụ", serviceId);
//        await _serviceRepo.SoftDeleteAsync(serviceId);
//    }

//    private static ServiceResponse Map(Service s) => new()
//    {
//        ServiceId = s.ServiceId,
//        Name = s.Name,
//        Description = s.Description,
//        Price = s.Price,
//        ImageUrl = s.ImageUrl,
//        IsAvailable = s.IsAvailable
//    };
//}
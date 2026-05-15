using KLCN_API.Helpers;
using KLCN_API.Middleware;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Entities;
using KLCN_API.Repositories.Interfaces;
using KLCN_API.Services.Interfaces;

namespace KLCN_API.Services;

public class ServiceService : IServiceService
{
    private readonly IServiceRepository _serviceRepo;

    public ServiceService(IServiceRepository serviceRepo) => _serviceRepo = serviceRepo;

    public async Task<List<ServiceResponse>> GetAllAsync(bool? isAvailable)
    {
        var items = await _serviceRepo.GetAllAsync(isAvailable);
        return items.Select(Map).ToList();
    }

    public async Task<ServiceResponse> GetByIdAsync(int serviceId)
    {
        var svc = await _serviceRepo.GetByIdAsync(serviceId)
            ?? throw new NotFoundException("Dịch vụ", serviceId);
        return Map(svc);
    }

    public async Task<ServiceResponse> CreateAsync(CreateServiceRequest request)
    {
        var svc = new Service
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Price = request.Price,
            ImageUrl = null,   // ảnh upload qua endpoint riêng
            IsAvailable = true,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _serviceRepo.CreateAsync(svc);
        return Map(created);
    }

    public async Task<ServiceResponse> UpdateAsync(int serviceId, UpdateServiceRequest request)
    {
        var svc = await _serviceRepo.GetByIdAsync(serviceId)
            ?? throw new NotFoundException("Dịch vụ", serviceId);

        if (request.Name is not null) svc.Name = request.Name.Trim();
        if (request.Description is not null) svc.Description = request.Description.Trim();
        if (request.Price.HasValue) svc.Price = request.Price.Value;
        if (request.IsAvailable.HasValue) svc.IsAvailable = request.IsAvailable.Value;
        // ImageUrl không update qua đây — dùng endpoint /image riêng

        await _serviceRepo.UpdateAsync(svc);
        return Map(svc);
    }

    public async Task DeleteAsync(int serviceId)
    {
        _ = await _serviceRepo.GetByIdAsync(serviceId)
            ?? throw new NotFoundException("Dịch vụ", serviceId);
        await _serviceRepo.SoftDeleteAsync(serviceId);
    }

    /// <summary>
    /// Upload ảnh dịch vụ: lưu file vào Uploads/services/,
    /// xóa ảnh cũ nếu có, cập nhật ImageUrl trong DB.
    /// </summary>
    public async Task<string> UploadImageAsync(
        int serviceId, IFormFile file, IWebHostEnvironment env)
    {
        var svc = await _serviceRepo.GetByIdAsync(serviceId)
            ?? throw new NotFoundException("Dịch vụ", serviceId);

        var newUrl = await ImageUploadHelper.SaveAsync(
            file, env.ContentRootPath, subfolder: "services");

        ImageUploadHelper.DeleteIfExists(svc.ImageUrl, env.ContentRootPath);

        svc.ImageUrl = newUrl;
        await _serviceRepo.UpdateAsync(svc);

        return newUrl;
    }

    private static ServiceResponse Map(Service s) => new()
    {
        ServiceId = s.ServiceId,
        Name = s.Name,
        Description = s.Description,
        Price = s.Price,
        ImageUrl = s.ImageUrl,
        IsAvailable = s.IsAvailable
    };
}
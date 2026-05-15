using KLCN_API.Mappers;
using KLCN_API.Middleware;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Repositories.Interfaces;
using KLCN_API.Services.Interfaces;

namespace KLCN_API.Services;

public class SystemConfigService : ISystemConfigService
{
    private readonly ISystemConfigRepository _configRepo;

    public SystemConfigService(ISystemConfigRepository configRepo)
        => _configRepo = configRepo;

    public async Task<List<SystemConfigResponse>> GetAllAsync()
    {
        var configs = await _configRepo.GetAllAsync();
        return configs.Select(SystemConfigMapper.ToResponse).ToList();
    }

    public async Task<SystemConfigResponse> GetByKeyAsync(string key)
    {
        var config = await _configRepo.GetByKeyAsync(key)
            ?? throw new NotFoundException($"Cấu hình '{key}' không tồn tại.");

        return SystemConfigMapper.ToResponse(config);
    }

    public async Task UpdateAsync(string key, UpdateSystemConfigRequest request, int updatedBy)
    {
        // Xác nhận key tồn tại trước khi update
        _ = await _configRepo.GetByKeyAsync(key)
            ?? throw new NotFoundException($"Cấu hình '{key}' không tồn tại.");

        await _configRepo.UpdateAsync(key, request.ConfigValue.Trim(), updatedBy);
    }
}

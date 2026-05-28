using KLCN_API.Filters;
using KLCN_API.Helpers;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Enums;
using KLCN_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KLCN_API.Controllers;

[ApiController]
[Route("api/backup")]
public class BackupController : ControllerBase
{
    private readonly IBackupService _backupService;
    private readonly IConfiguration _config;

    public BackupController(IBackupService backupService, IConfiguration config)
    {
        _backupService = backupService;
        _config = config;
    }

    // ================================================================
    // NORMAL ENDPOINTS — yêu cầu JWT + Admin
    // ================================================================

    /// <summary>Download .zip backup toàn bộ dữ liệu — Admin.</summary>
    [HttpGet("export")]
    [Authorize]
    [AuthorizeRoles(RoleEnum.Admin)]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> Export()
    {
        var (zipBytes, fileName) = await _backupService.ExportAsync();
        return File(zipBytes, "application/zip", fileName);
    }

    /// <summary>Danh sách snapshot đang lưu trên server — Admin.</summary>
    [HttpGet("snapshots")]
    [Authorize]
    [AuthorizeRoles(RoleEnum.Admin)]
    [ProducesResponseType(typeof(ApiResponse<List<BackupSnapshotInfo>>), 200)]
    public async Task<IActionResult> ListSnapshots()
    {
        var result = await _backupService.ListSnapshotsAsync();
        return Ok(ApiResponse<List<BackupSnapshotInfo>>.Ok(result));
    }

    /// <summary>Tạo snapshot thủ công — Admin.</summary>
    [HttpPost("snapshot")]
    [Authorize]
    [AuthorizeRoles(RoleEnum.Admin)]
    [ProducesResponseType(typeof(ApiResponse<BackupSnapshotInfo>), 200)]
    public async Task<IActionResult> CreateSnapshot()
    {
        var result = await _backupService.CreateSnapshotAsync();
        return Ok(ApiResponse<BackupSnapshotInfo>.Ok(result, "Tạo snapshot thành công."));
    }

    /// <summary>Download một snapshot cụ thể — Admin.</summary>
    [HttpGet("snapshots/{fileName}")]
    [Authorize]
    [AuthorizeRoles(RoleEnum.Admin)]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> DownloadSnapshot(string fileName)
    {
        var zipBytes = await _backupService.DownloadSnapshotAsync(fileName);
        return File(zipBytes, "application/zip", fileName);
    }

    /// <summary>Xóa snapshot — Admin.</summary>
    [HttpDelete("snapshots/{fileName}")]
    [Authorize]
    [AuthorizeRoles(RoleEnum.Admin)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> DeleteSnapshot(string fileName)
    {
        await _backupService.DeleteSnapshotAsync(fileName);
        return Ok(ApiResponse.Ok("Xóa snapshot thành công."));
    }

    /// <summary>
    /// Restore từ file .zip — Admin.
    /// Tự snapshot trước khi restore. Rollback nếu lỗi.
    /// </summary>
    [HttpPost("restore")]
    [Authorize]
    [AuthorizeRoles(RoleEnum.Admin)]
    [ProducesResponseType(typeof(ApiResponse<RestoreReportResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> Restore(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse.Fail("Vui lòng chọn file backup (.zip)."));

        if (!file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse.Fail("File backup phải có định dạng .zip."));

        await using var stream = file.OpenReadStream();
        var report = await _backupService.RestoreAsync(stream, User.GetUserId());

        return Ok(ApiResponse<RestoreReportResponse>.Ok(report, "Restore thành công."));
    }

    /// <summary>
    /// Restore trực tiếp từ snapshot trên server — Admin.
    /// Không cần tải .zip về máy rồi upload lại.
    /// Tự snapshot trước khi restore. Rollback nếu lỗi.
    /// </summary>
    [HttpPost("snapshots/{fileName}/restore")]
    [Authorize]
    [AuthorizeRoles(RoleEnum.Admin)]
    [ProducesResponseType(typeof(ApiResponse<RestoreReportResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> RestoreFromSnapshot(string fileName)
    {
        var report = await _backupService.RestoreFromSnapshotAsync(fileName, User.GetUserId());
        return Ok(ApiResponse<RestoreReportResponse>.Ok(report,
            $"Restore từ snapshot '{fileName}' thành công."));
    }

    // ================================================================
    // EMERGENCY ENDPOINTS — KHÔNG cần JWT
    // Bảo vệ bằng X-Emergency-Key header (secret key trong appsettings)
    //
    // Dùng khi: DB mất tài khoản admin, không đăng nhập được.
    // Sau khi restore xong → đăng nhập bình thường, không cần dùng nữa.
    // ================================================================

    /// <summary>
    /// [EMERGENCY] Liệt kê snapshot trên server — không cần đăng nhập.
    /// Header: X-Emergency-Key: {giá trị BackupSettings:EmergencyKey trong appsettings}
    /// </summary>
    [HttpGet("emergency/snapshots")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<BackupSnapshotInfo>>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> EmergencyListSnapshots(
        [FromHeader(Name = "X-Emergency-Key")] string? emergencyKey)
    {
        if (!ValidateEmergencyKey(emergencyKey))
            return Unauthorized(ApiResponse.Fail("Emergency key không hợp lệ."));

        var result = await _backupService.ListSnapshotsAsync();
        return Ok(ApiResponse<List<BackupSnapshotInfo>>.Ok(result));
    }

    /// <summary>
    /// [EMERGENCY] Download snapshot — không cần đăng nhập.
    /// Header: X-Emergency-Key: {EmergencyKey}
    /// </summary>
    [HttpGet("emergency/snapshots/{fileName}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> EmergencyDownloadSnapshot(
        string fileName,
        [FromHeader(Name = "X-Emergency-Key")] string? emergencyKey)
    {
        if (!ValidateEmergencyKey(emergencyKey))
            return Unauthorized(ApiResponse.Fail("Emergency key không hợp lệ."));

        var zipBytes = await _backupService.DownloadSnapshotAsync(fileName);
        return File(zipBytes, "application/zip", fileName);
    }

    /// <summary>
    /// [EMERGENCY] Restore trực tiếp từ snapshot trên server — không cần đăng nhập.
    /// Header: X-Emergency-Key: {EmergencyKey}
    ///
    /// Dùng khi DB mất hoàn toàn nhưng snapshot vẫn còn trên server.
    /// Không cần tải file về máy rồi upload lại.
    /// </summary>
    [HttpPost("emergency/snapshots/{fileName}/restore")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RestoreReportResponse>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> EmergencyRestoreFromSnapshot(
        string fileName,
        [FromHeader(Name = "X-Emergency-Key")] string? emergencyKey)
    {
        if (!ValidateEmergencyKey(emergencyKey))
            return Unauthorized(ApiResponse.Fail("Emergency key không hợp lệ."));

        var report = await _backupService.RestoreFromSnapshotAsync(fileName, adminUserId: 0);
        return Ok(ApiResponse<RestoreReportResponse>.Ok(report,
            $"Restore khẩn cấp từ snapshot '{fileName}' thành công. Hãy đăng nhập bằng tài khoản admin đã khôi phục."));
    }

    /// <summary>
    /// [EMERGENCY] Restore từ file .zip — không cần đăng nhập.
    /// Header: X-Emergency-Key: {EmergencyKey}
    ///
    /// Dùng khi DB mất hoàn toàn (kể cả tài khoản admin).
    /// Sau khi restore → tài khoản admin khôi phục → đăng nhập bình thường.
    /// </summary>
    [HttpPost("emergency/restore")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RestoreReportResponse>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> EmergencyRestore(
        IFormFile file,
        [FromHeader(Name = "X-Emergency-Key")] string? emergencyKey)
    {
        if (!ValidateEmergencyKey(emergencyKey))
            return Unauthorized(ApiResponse.Fail("Emergency key không hợp lệ."));

        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse.Fail("Vui lòng chọn file backup (.zip)."));

        if (!file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse.Fail("File backup phải có định dạng .zip."));

        await using var stream = file.OpenReadStream();
        // userId = 0 vì không có user đăng nhập trong tình huống khẩn cấp
        var report = await _backupService.RestoreAsync(stream, adminUserId: 0);

        return Ok(ApiResponse<RestoreReportResponse>.Ok(report,
            "Restore khẩn cấp thành công. Hãy đăng nhập bằng tài khoản admin đã khôi phục."));
    }

    // ── Helper ────────────────────────────────────────────────────

    /// <summary>
    /// So sánh emergency key bằng constant-time để chống timing attack.
    /// Key lấy từ appsettings BackupSettings:EmergencyKey.
    /// </summary>
    private bool ValidateEmergencyKey(string? providedKey)
    {
        if (string.IsNullOrWhiteSpace(providedKey)) return false;

        var expected = _config["BackupSettings:EmergencyKey"];
        if (string.IsNullOrWhiteSpace(expected)) return false;

        // CryptographicOperations.FixedTimeEquals chống timing attack
        var expectedBytes = System.Text.Encoding.UTF8.GetBytes(expected);
        var providedBytes = System.Text.Encoding.UTF8.GetBytes(providedKey);

        return System.Security.Cryptography.CryptographicOperations
            .FixedTimeEquals(expectedBytes, providedBytes);
    }
}
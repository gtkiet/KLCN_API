//// ============================================================
//// Helpers/JwtHelper.cs
//// ============================================================
//// TODO: Tạo/verify JWT access token và refresh token
////       - GenerateAccessToken(User user) → string
////       - GenerateRefreshToken() → string
////       - GetPrincipalFromExpiredToken(string token) → ClaimsPrincipal

//namespace KLCN_API.Helpers;

//public class JwtHelper
//{
//    // TODO: Inject IConfiguration để đọc JwtSettings
//}

//// ============================================================
//// Helpers/PasswordHelper.cs
//// ============================================================
//// TODO: Bcrypt wrapper
////       - HashPassword(string password) → string
////       - VerifyPassword(string password, string hash) → bool

//public class PasswordHelper
//{
//    // TODO: Dùng BCrypt.Net-Next NuGet package
//}

//// ============================================================
//// Helpers/ClaimsHelper.cs
//// ============================================================
//// TODO: Extension method lấy UserId và Role từ HttpContext.User
////       - GetUserId(this ClaimsPrincipal principal) → int
////       - GetRole(this ClaimsPrincipal principal) → string

//public static class ClaimsHelper { }

//// ============================================================
//// Helpers/StoredProcedureHelper.cs
//// ============================================================
//// TODO: Helper để gọi Stored Procedure qua EF Core
////       - ExecuteSpAsync(DbContext ctx, string spName, params SqlParameter[] parameters)
////       Dùng cho: sp_HoldSlots, sp_ConfirmBooking, sp_CancelBooking,
////                 sp_RecordDeposit, sp_ApplyPromotion, sp_RescheduleBooking,
////                 sp_ConfirmPurchaseOrder, sp_GenerateSlots

//public class StoredProcedureHelper { }

//// ============================================================
//// Helpers/PaginationHelper.cs
//// ============================================================
//// TODO: Tạo PagedResponse<T> từ IQueryable<T>
////       - ToPagedAsync<T>(IQueryable<T> query, int page, int pageSize) → PagedResponse<T>

//public static class PaginationHelper { }

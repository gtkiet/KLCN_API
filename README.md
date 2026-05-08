# SportPlus API — Hướng dẫn làm việc theo phiên

## Cấu trúc project

```
KLCN_API/
├── Program.cs                          ← Entry point (DI, Auth, CORS, Middleware, Jobs)
├── appsettings.json                    ← Connection string, JWT, CORS config
│                                         ⚠️ KHÔNG commit secret lên Git — dùng User Secrets ở prod
│
├── Configurations/
│   └── Settings.cs                     ← JwtSettings, CorsSettings POCO
│
├── Data/
│   └── SportPlusDbContext.cs           ← EF Core DbContext (cần OnModelCreating)
│
├── Models/
│   ├── Entities/
│   │   └── Entities.cs                 ← Tất cả Entity class (ánh xạ DB)
│   ├── DTOs/
│   │   ├── Request/
│   │   │   └── Requests.cs            ← Request DTO
│   │   └── Response/
│   │       ├── ApiResponse.cs          ← Wrapper chuẩn + PagedResponse
│   │       └── Responses.cs           ← Response DTO
│   └── Enums/
│       └── Enums.cs                    ← Enum cho lookup tables
│
├── Repositories/
│   ├── Interfaces/
│   │   └── IRepositories.cs           ← Interface data access layer
│   └── (implement files — tạo khi làm)
│
├── Services/
│   ├── Interfaces/
│   │   └── IServices.cs               ← Interface business logic
│   └── (implement files — tạo khi làm)
│
├── Controllers/
│   └── (implement files — tạo khi làm)
│
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs  ← Global error handler (ưu tiên dùng thay ExceptionFilter)
│
├── Filters/
│   └── Filters.cs                      ← ValidationFilter (chỉ validate model), AuthorizeRoles
│
├── Extensions/
│   └── ServiceCollectionExtensions.cs  ← DI registration helpers
│
├── Helpers/
│   └── Helpers.cs                      ← JWT, Password, Claims, SP, Pagination
│
└── Jobs/
    └── BackgroundJobs.cs               ← ReleaseExpiredSlots, GenerateDailySlots
```

---

### Bảo mật appsettings

`appsettings.json` chỉ dùng cho Development. Production phải dùng Environment Variables hoặc User Secrets:

```bash
dotnet user-secrets set "JwtSettings:SecretKey" "your-prod-secret"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-prod-conn"
```

---

## Cách nhờ hỗ trợ từng phần

Khi bắt đầu một phiên mới, gửi kèm:
1. File DB script (`SportPlusDB.sql`) — hoặc đề cập đã có
2. File cụ thể cần làm (vd: `AuthController`, `AuthService`, `AuthRepository`)
3. Nói rõ muốn làm gì

### Template prompt gợi ý

```
Tôi muốn implement AuthController + AuthService + AuthRepository.
- Gồm các API: Register, Login, RefreshToken, Logout, ChangePassword
- Đã có: Entities.cs, Enums.cs, IServices.cs, IRepositories.cs (gửi kèm)
- DB dùng SQL Server, bảng Users + RefreshTokens
- Dùng BCrypt.Net-Next cho password
- JWT: access token 60 phút, refresh token 30 ngày
```

---

## Thứ tự nên làm

### Giai đoạn 1 — Nền tảng ✅ (đã có khung)

- [x] Program.cs (DI, Auth, CORS, Swagger, Jobs)
- [x] ExceptionHandlingMiddleware
- [x] ValidationFilter + AuthorizeRolesAttribute
- [x] ServiceCollectionExtensions (AddDatabase, AddJwtAuthentication, AddCors, AddSwagger)
- [ ] JwtHelper + PasswordHelper + ClaimsHelper (`Helpers.cs` — chưa implement)
- [ ] `SportPlusDbContext.OnModelCreating`
- [ ] `Entities.cs`, `Enums.cs`, `IServices.cs`, `IRepositories.cs` (cần điền nội dung)

### Giai đoạn 2 — Auth & User

- [ ] AuthRepository + AuthService + AuthController
- [ ] UserRepository + UserService + UsersController

### Giai đoạn 3 — Core Booking Flow

- [ ] FieldRepository + FieldService + FieldsController
  - GetFields, GetSchedule, GenerateSlots
- [ ] BookingRepository + BookingService + BookingsController
  - HoldSlots → CreateBooking → ConfirmPayment
  - CancelBooking, Reschedule
- [ ] PaymentService + RecordDeposit + RecordFullPayment

### Giai đoạn 4 — Quản lý

- [ ] PromotionService + PromotionsController
- [ ] ServiceService + ServicesController
- [ ] InventoryService + Suppliers/Products/PurchaseOrders
- [ ] IncidentService + IncidentsController
- [ ] ReviewService + ReviewsController

### Giai đoạn 5 — Dashboard & Jobs

- [ ] DashboardService + DashboardController
- [ ] SystemConfigService + SystemConfigController
- [ ] NotificationService + NotificationsController
- [ ] BackgroundJobs (ReleaseExpiredSlots, GenerateDailySlots) ← đã đăng ký trong Program.cs

---

## Stored Procedures cần gọi qua API

| SP | Gọi từ | Mô tả |
|---|---|---|
| `sp_HoldSlots` | `BookingService.HoldSlotsAsync` | Giữ slot tạm |
| `sp_ConfirmBooking` | `BookingService.CreateBookingAsync` | Xác nhận + tính tiền |
| `sp_CancelBooking` | `BookingService.CancelBookingAsync` | Hủy + hoàn tiền |
| `sp_RecordDeposit` | `PaymentService.RecordDepositAsync` | Ghi nhận cọc |
| `sp_RecordFullPayment` | `PaymentService.RecordFullPaymentAsync` | Thanh toán còn lại |
| `sp_ApplyPromotion` | `BookingService.ApplyVoucherAsync` | Áp voucher |
| `sp_RescheduleBooking` | `BookingService.RescheduleAsync` | Đổi lịch |
| `sp_ConfirmPurchaseOrder` | `InventoryService.ConfirmPurchaseOrderAsync` | Xác nhận đơn nhập kho |
| `sp_GenerateSlots` | `FieldService.GenerateSlotsAsync` + Job | Sinh slot |
| `sp_ReleaseExpiredSlots` | Background Job | Giải phóng slot hết hạn |

---

## NuGet packages

```xml
<PackageReference Include="BCrypt.Net-Next" Version="4.1.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.7" />
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.7" />
<PackageReference Include="Microsoft.Data.SqlClient" Version="7.0.1" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.7" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="10.0.7" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="10.1.7" />
```

> ⚠️ Swashbuckle 10.x có breaking changes so với 6.x — test build sớm sau khi restore packages.

---

## Tóm tắt những gì cần sửa trong code

| File | Vấn đề | Hành động |
|---|---|---|
| `Program.cs` | `ExceptionFilter` redundant với Middleware | Xóa `options.Filters.Add<ExceptionFilter>()` |
| `ValidationFilter.cs` | Đang bắt `BusinessException` — không phải nhiệm vụ của nó | Xóa phần `executed.Exception` handling |
| `Filters/ExceptionFilter.cs` | Redundant hoàn toàn | Có thể xóa file |
| `appsettings.json` | Secret hardcode | Ổn cho dev, cần User Secrets cho prod |
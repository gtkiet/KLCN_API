# SportPlus API — Hướng dẫn làm việc theo phiên

## Cấu trúc project

```
KLCN_API/
├── Program.cs                          ← Entry point (cần hoàn thiện DI, Auth, CORS)
├── appsettings.json                    ← Connection string, JWT, CORS config
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
│   │   │   └── Requests.cs            ← Request DTO placeholder
│   │   └── Response/
│   │       ├── ApiResponse.cs          ← Wrapper chuẩn + PagedResponse
│   │       └── Responses.cs           ← Response DTO placeholder
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
│   └── Controllers.cs                  ← Tất cả controller placeholder
│
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs  ← Global error handler
│
├── Filters/
│   └── Filters.cs                      ← ValidationFilter, AuthorizeRoles
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

## Cách nhờ hỗ trợ từng phần

Khi bắt đầu một phiên mới, gửi kèm:
1. File DB script (SportPlusDB.sql) — hoặc đề cập đã có
2. File cụ thể cần làm (vd: `Controllers/Controllers.cs` phần AuthController)
3. Nói rõ muốn làm gì

### Template prompt gợi ý:

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

### Giai đoạn 1 — Nền tảng
- [ ] Program.cs (DI, Auth, CORS, Swagger)
- [ ] ExceptionHandlingMiddleware
- [ ] ValidationFilter
- [ ] JwtHelper + PasswordHelper + ClaimsHelper
- [ ] ServiceCollectionExtensions (đăng ký đầy đủ)

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
- [ ] BackgroundJobs (ReleaseExpiredSlots, GenerateDailySlots)

---

## Stored Procedures cần gọi qua API

| SP | Gọi từ | Mô tả |
|---|---|---|
| `sp_HoldSlots` | BookingService.HoldSlotsAsync | Giữ slot tạm |
| `sp_ConfirmBooking` | BookingService.CreateBookingAsync | Xác nhận + tính tiền |
| `sp_CancelBooking` | BookingService.CancelBookingAsync | Hủy + hoàn tiền |
| `sp_RecordDeposit` | PaymentService.RecordDepositAsync | Ghi nhận cọc |
| `sp_RecordFullPayment` | PaymentService.RecordFullPaymentAsync | Thanh toán còn lại |
| `sp_ApplyPromotion` | BookingService.ApplyVoucherAsync | Áp voucher |
| `sp_RescheduleBooking` | BookingService.RescheduleAsync | Đổi lịch |
| `sp_ConfirmPurchaseOrder` | InventoryService.ConfirmPurchaseOrderAsync | Xác nhận đơn nhập kho |
| `sp_GenerateSlots` | FieldService.GenerateSlotsAsync + Job | Sinh slot |
| `sp_ReleaseExpiredSlots` | Background Job | Giải phóng slot hết hạn |

---

## NuGet packages cần cài

```
Microsoft.EntityFrameworkCore.SqlServer
Microsoft.EntityFrameworkCore.Tools
Microsoft.AspNetCore.Authentication.JwtBearer
BCrypt.Net-Next
Swashbuckle.AspNetCore
```

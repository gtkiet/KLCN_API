using KLCN_API.Extensions;
using KLCN_API.Filters;
using KLCN_API.Jobs;
using KLCN_API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ================================================================
// SERVICES
// ================================================================

// Controllers + ValidationFilter global
builder.Services.AddControllers(options =>
    options.Filters.Add<ValidationFilter>()
);

// Tắt automatic 400 response mặc định của ASP.NET
// để ValidationFilter của chúng ta xử lý thay thế
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
    options.SuppressModelStateInvalidFilter = true
);

builder.Services.AddEndpointsApiExplorer();

// DbContext (SQL Server)
builder.Services.AddDatabase(builder.Configuration);

// JWT Authentication + Authorization
builder.Services.AddJwtAuthentication(builder.Configuration);

// CORS
builder.Services.AddCorsPolicy(builder.Configuration);

// Swagger với Bearer token support
builder.Services.AddSwaggerWithAuth();

// Application Services & Repositories (điền khi làm Giai đoạn 2+)
builder.Services.AddApplicationServices();
builder.Services.AddRepositories();

// Background Jobs
builder.Services.AddHostedService<ReleaseExpiredSlotsJob>();
builder.Services.AddHostedService<GenerateDailySlotsJob>();

// ================================================================
// PIPELINE
// ================================================================

var app = builder.Build();

// Global exception handler — phải đứng đầu tiên
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger — luôn bật (kể cả production để tiện test)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "SportPlus API V1");
    options.RoutePrefix = "";           // swagger tại root domain
    options.DisplayRequestDuration();          // hiển thị thời gian response
    options.DefaultModelsExpandDepth(-1);      // ẩn schema section mặc định
});

app.UseHttpsRedirection();

// CORS — phải trước Authentication
app.UseCors("AllowAll");

// Auth pipeline — đúng thứ tự: Authentication → Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
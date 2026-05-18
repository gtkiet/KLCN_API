using KLCN_API.Extensions;
using KLCN_API.Filters;
using KLCN_API.Jobs;
using KLCN_API.Middleware;
using KLCN_API.Services;
using KLCN_API.Services.Interfaces;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});

// AddEndpointsApiExplorer cần thiết để Swashbuckle khám phá endpoint
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddSwaggerWithAuth();
builder.Services.AddPaymentGateways(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddRepositories();
builder.Services.AddEmailService(builder.Configuration);
builder.Services.AddFrontendSettings(builder.Configuration);

// Background jobs thay thế SQL Agent (dùng khi host không hỗ trợ SQL Agent)
builder.Services.AddHostedService<ReleaseExpiredSlotsJob>();
builder.Services.AddHostedService<GenerateDailySlotsJob>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();

// ── App ───────────────────────────────────────────────────────────

var app = builder.Build();

// Phải là middleware đầu tiên để bắt mọi exception từ các middleware sau
app.UseMiddleware<Middleware>();

// Swagger chỉ expose ở môi trường Development
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI(options =>
//    {
//        options.SwaggerEndpoint("/swagger/v1/swagger.json", "SportPlus API V1");
//        options.RoutePrefix = "";           // Swagger UI ở / thay vì /swagger
//        options.EnablePersistAuthorization();
//    });
//}
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "SportPlus API V1");
    options.RoutePrefix = "";           // Swagger UI ở / thay vì /swagger
    options.EnablePersistAuthorization();
});

app.UseHttpsRedirection();

// File tĩnh mặc định (wwwroot)
//app.UseStaticFiles();

// File upload — dùng ContentRootPath thay vì GetParent(GetCurrentDirectory())
// để path nhất quán cả khi chạy dev, dotnet run, và sau khi publish.
var uploadPath = Path.Combine(builder.Environment.ContentRootPath, "Uploads");
if (!Directory.Exists(uploadPath))
    Directory.CreateDirectory(uploadPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadPath),
    RequestPath = "/Uploads"
});

// CORS phải trước Authentication/Authorization
app.UseCors("AllowConfigured");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
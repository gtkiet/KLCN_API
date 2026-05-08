using KLCN_API.Extensions;
using KLCN_API.Filters;
using KLCN_API.Jobs;
using KLCN_API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// SERVICES
// ======================================================

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();  // giữ nguyên
    options.Filters.Add<ExceptionFilter>();   // thêm dòng này
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDatabase(builder.Configuration);

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddCorsPolicy(builder.Configuration);

builder.Services.AddSwaggerWithAuth();

builder.Services.AddApplicationServices();

builder.Services.AddRepositories();

//builder.Services.AddScoped<ValidationFilter>();

// Background jobs
builder.Services.AddHostedService<ReleaseExpiredSlotsJob>();
builder.Services.AddHostedService<GenerateDailySlotsJob>();

// ======================================================
// BUILD
// ======================================================

var app = builder.Build();

// ======================================================
// MIDDLEWARE — THỨ TỰ BẮT BUỘC
// ======================================================

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "SportPlus API V1");

        options.RoutePrefix = "";

        // Giữ token sau khi F5 trang
        options.EnablePersistAuthorization();
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowConfigured");

// Authentication PHẢI trước Authorization
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
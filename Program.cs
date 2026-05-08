using KLCN_API.Extensions;
using KLCN_API.Filters;
using KLCN_API.Jobs;
using KLCN_API.Middleware;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDatabase(builder.Configuration);

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddCorsPolicy(builder.Configuration);

builder.Services.AddSwaggerWithAuth();

builder.Services.AddApplicationServices();

builder.Services.AddRepositories();

builder.Services.AddHostedService<ReleaseExpiredSlotsJob>();
builder.Services.AddHostedService<GenerateDailySlotsJob>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "SportPlus API V1");

    options.RoutePrefix = "";

    options.EnablePersistAuthorization();
});

app.UseHttpsRedirection();

//var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

//if (!Directory.Exists(imagePath))
//{
//    Directory.CreateDirectory(imagePath);
//}

//app.UseStaticFiles(new StaticFileOptions
//{
//    FileProvider = new PhysicalFileProvider(imagePath),
//    RequestPath = "/Uploads"
//});
var uploadPath = Path.Combine(
    Directory.GetParent(Directory.GetCurrentDirectory())!.FullName,
    "Uploads");

if (!Directory.Exists(uploadPath))
{
    Directory.CreateDirectory(uploadPath);
}

app.UseStaticFiles();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadPath),
    RequestPath = "/Uploads"
});

app.UseCors("AllowConfigured");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
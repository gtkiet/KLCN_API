using KLCN_API.Extensions;
using KLCN_API.Filters;
using KLCN_API.Jobs;
using KLCN_API.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
    //options.Filters.Add<ExceptionFilter>();
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "SportPlus API V1");

        options.RoutePrefix = "";

        options.EnablePersistAuthorization();
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowConfigured");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
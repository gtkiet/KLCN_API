using System.Text.Json;
using KLCN_API.Configurations;
using KLCN_API.Data;
using KLCN_API.Helpers;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Repositories;
using KLCN_API.Repositories.Interfaces;
using KLCN_API.Services;
using KLCN_API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

namespace KLCN_API.Extensions;

public static class ServiceCollectionExtensions
{
    // ============================================================
    // DATABASE
    // ============================================================

    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddDbContext<SportPlusDbContext>(options =>
            options.UseSqlServer(
                config.GetConnectionString("DefaultConnection"),
                sql => sql.CommandTimeout(30)));

        return services;
    }

    // ============================================================
    // JWT AUTHENTICATION
    // ============================================================

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration config)
    {
        var jwtSection = config.GetSection("JwtSettings");

        if (!jwtSection.Exists())
            throw new InvalidOperationException(
                "Khong tim thay JwtSettings trong appsettings.");

        var jwtSettings =
            jwtSection.Get<JwtSettings>()
            ?? throw new InvalidOperationException(
                "Khong bind duoc JwtSettings.");

        if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
            throw new InvalidOperationException(
                "JwtSettings:SecretKey khong duoc de trong.");

        services.AddSingleton(jwtSettings);
        services.AddSingleton<JwtHelper>();

        var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";

                        var response = ApiResponse.Fail(
                            "Bạn chưa đăng nhập hoặc token không hợp lệ.");

                        await context.Response.WriteAsync(
                            JsonSerializer.Serialize(response, jsonOptions));
                    },

                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";

                        var response = ApiResponse.Fail(
                            "Bạn không có quyền thực hiện thao tác này.");

                        await context.Response.WriteAsync(
                            JsonSerializer.Serialize(response, jsonOptions));
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly",
                policy => policy.RequireRole("Admin"));

            options.AddPolicy("StaffOrAdmin",
                policy => policy.RequireRole("Admin", "Staff"));

            options.AddPolicy("AnyAuth",
                policy => policy.RequireAuthenticatedUser());
        });

        return services;
    }

    // ============================================================
    // CORS
    // ============================================================

    public static IServiceCollection AddCorsPolicy(
        this IServiceCollection services,
        IConfiguration config)
    {
        var corsSettings =
            config.GetSection("CorsSettings").Get<CorsSettings>()
            ?? new CorsSettings();

        services.AddCors(options =>
        {
            options.AddPolicy("AllowConfigured", policy =>
            {
                if (corsSettings.AllowedOrigins.Any())
                    policy.WithOrigins(corsSettings.AllowedOrigins.ToArray());
                else
                    policy.AllowAnyOrigin();

                policy.AllowAnyHeader().AllowAnyMethod();
            });

            options.AddPolicy("AllowAll", policy =>
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
        });

        return services;
    }

    // ============================================================
    // SWAGGER + JWT
    // ============================================================

    public static IServiceCollection AddSwaggerWithAuth(
        this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SportPlus API",
                Version = "v1",
                Description = "Hệ thống quản lý sân bóng SportPlus API"
            });

            var xmlPath = Path.Combine(
                AppContext.BaseDirectory,
                $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml");

            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Nhập JWT access token. Swagger tự thêm prefix 'Bearer ' cho bạn."
            });

            options.AddSecurityRequirement(document =>
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] =
                        new List<string>()
                });

            options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
        });

        return services;
    }

    // ============================================================
    // SERVICES
    // ============================================================

    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();

        return services;
    }

    // ============================================================
    // REPOSITORIES
    // ============================================================

    public static IServiceCollection AddRepositories(
        this IServiceCollection services)
    {
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
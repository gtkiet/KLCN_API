using KLCN_API.Configurations;
using KLCN_API.Data;
using KLCN_API.Helpers;
using KLCN_API.Models.DTOs.Response;
//using KLCN_API.Models.Entities;
using KLCN_API.Repositories;
using KLCN_API.Repositories.Interfaces;
using KLCN_API.Services;
using KLCN_API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using System.Text.Json;

namespace KLCN_API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<SportPlusDbContext>(options =>
            options.UseSqlServer(
                config.GetConnectionString("DefaultConnection"),
                sql => sql.CommandTimeout(30)));
        return services;
    }

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, IConfiguration config)
    {
        var jwtSettings =
            config.GetSection("JwtSettings").Get<JwtSettings>()
            ?? throw new InvalidOperationException("Khong tim thay JwtSettings.");

        if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
            throw new InvalidOperationException("JwtSettings:SecretKey khong duoc de trong.");

        services.AddSingleton(jwtSettings);
        services.AddSingleton<JwtHelper>();

        var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        services
            .AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                o.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(o =>
            {
                o.RequireHttpsMetadata = false;
                o.SaveToken = true;
                o.TokenValidationParameters = new TokenValidationParameters
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
                o.Events = new JwtBearerEvents
                {
                    OnChallenge = async ctx =>
                    {
                        ctx.HandleResponse();
                        ctx.Response.StatusCode = 401;
                        ctx.Response.ContentType = "application/json";
                        await ctx.Response.WriteAsync(JsonSerializer.Serialize(
                            ApiResponse.Fail("Ban chua dang nhap hoac token khong hop le."), jsonOptions));
                    },
                    OnForbidden = async ctx =>
                    {
                        ctx.Response.StatusCode = 403;
                        ctx.Response.ContentType = "application/json";
                        await ctx.Response.WriteAsync(JsonSerializer.Serialize(
                            ApiResponse.Fail("Ban khong co quyen thuc hien thao tac nay."), jsonOptions));
                    }
                };
            });

        services.AddAuthorization(o =>
        {
            o.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
            o.AddPolicy("StaffOrAdmin", p => p.RequireRole("Admin", "Staff"));
            o.AddPolicy("AnyAuth", p => p.RequireAuthenticatedUser());
        });

        return services;
    }

    public static IServiceCollection AddCorsPolicy(
        this IServiceCollection services, IConfiguration config)
    {
        var cors = config.GetSection("CorsSettings").Get<CorsSettings>() ?? new CorsSettings();

        services.AddCors(o =>
        {
            o.AddPolicy("AllowConfigured", p =>
            {
                if (cors.AllowedOrigins.Any())
                    p.WithOrigins(cors.AllowedOrigins.ToArray());
                else
                    p.AllowAnyOrigin();
                p.AllowAnyHeader().AllowAnyMethod();
            });
            o.AddPolicy("AllowAll", p =>
                p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
        });

        return services;
    }

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

    public static IServiceCollection AddVNPaySettings(this IServiceCollection services, IConfiguration config)
    {
        // ServiceCollectionExtensions.cs
        var vnpaySettings = config.GetSection("VNPaySettings").Get<VNPaySettings>()
            ?? throw new InvalidOperationException("Thieu VNPaySettings.");
        services.AddSingleton(vnpaySettings);
        services.AddSingleton<VNPayHelper>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IFieldService, FieldService>();
        //services.AddScoped<IBookingService, BookingService>();
        //services.AddScoped<IPaymentService, PaymentService>();

        //services.AddScoped<IIncidentService, IncidentService>();
        //services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ISystemConfigService, SystemConfigService>();

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IFieldRepository, FieldRepository>();
        //services.AddScoped<IBookingRepository, BookingRepository>();
        //services.AddScoped<IPaymentRepository, PaymentRepository>();
        //services.AddScoped<IDepositRepository, DepositRepository>();
        //services.AddScoped<IServiceRepository, ServiceRepository>();

        //services.AddScoped<IIncidentRepository, IncidentRepository>();
        //services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<ISystemConfigRepository, SystemConfigRepository>();

        return services;
    }
}
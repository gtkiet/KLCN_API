using System.Text;
using KLCN_API.Configurations;
using KLCN_API.Data;
using KLCN_API.Filters;
using KLCN_API.Helpers;
using KLCN_API.Repositories;
using KLCN_API.Repositories.Interfaces;
using KLCN_API.Services;
using KLCN_API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace KLCN_API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<SportPlusDbContext>(options =>
            options.UseSqlServer(
                config.GetConnectionString("DefaultConnection"),
                sql => sql.CommandTimeout(30)
            )
        );
        return services;
    }

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, IConfiguration config)
    {
        var jwtSettings = config.GetSection("JwtSettings").Get<JwtSettings>()
                          ?? throw new InvalidOperationException("JwtSettings chua duoc cau hinh.");

        services.AddSingleton(jwtSettings);
        services.AddSingleton<JwtHelper>();

        var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async ctx =>
                    {
                        ctx.HandleResponse();
                        ctx.Response.StatusCode = 401;
                        ctx.Response.ContentType = "application/json";
                        await ctx.Response.WriteAsync(
                            "{\"success\":false,\"message\":\"Ban chua dang nhap hoac token khong hop le.\"}");
                    },
                    OnForbidden = async ctx =>
                    {
                        ctx.Response.StatusCode = 403;
                        ctx.Response.ContentType = "application/json";
                        await ctx.Response.WriteAsync(
                            "{\"success\":false,\"message\":\"Ban khong co quyen thuc hien thao tac nay.\"}");
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
            options.AddPolicy("StaffOrAdmin", p => p.RequireRole("Admin", "Staff"));
            options.AddPolicy("AnyAuth", p => p.RequireAuthenticatedUser());
        });

        return services;
    }

    public static IServiceCollection AddCorsPolicy(
        this IServiceCollection services, IConfiguration config)
    {
        var corsSettings = config.GetSection("CorsSettings").Get<CorsSettings>()
                           ?? new CorsSettings();

        services.AddCors(options =>
        {
            options.AddPolicy("AllowConfigured", policy =>
            {
                if (corsSettings.AllowedOrigins.Count > 0)
                    policy.WithOrigins(corsSettings.AllowedOrigins.ToArray());
                else
                    policy.AllowAnyOrigin();

                policy.AllowAnyMethod().AllowAnyHeader();
            });

            options.AddPolicy("AllowAll", policy =>
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        });

        return services;
    }

    public static IServiceCollection AddSwaggerWithAuth(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SportPlus API",
                Version = "v1",
                Description = "He thong quan ly san bong Sport Plus"
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Description = "Nhap: Bearer {token}"
            });

            // Bỏ AddSecurityRequirement global — để AuthorizeOperationFilter xử lý
            c.OperationFilter<AuthorizeOperationFilter>();

            c.TagActionsBy(api =>
                new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"]! });
        });

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        // Giai doan 3+:
        // services.AddScoped<IFieldService, FieldService>();
        // services.AddScoped<IBookingService, BookingService>();
        // services.AddScoped<IPaymentService, PaymentService>();
        // services.AddScoped<IPromotionService, PromotionService>();
        // services.AddScoped<IServiceService, ServiceService>();
        // services.AddScoped<IInventoryService, InventoryService>();
        // services.AddScoped<IIncidentService, IncidentService>();
        // services.AddScoped<IReviewService, ReviewService>();
        // services.AddScoped<INotificationService, NotificationService>();
        // services.AddScoped<IDashboardService, DashboardService>();
        // services.AddScoped<ISystemConfigService, SystemConfigService>();
        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        // Giai doan 3+:
        // services.AddScoped<IFieldRepository, FieldRepository>();
        // services.AddScoped<IFieldSlotRepository, FieldSlotRepository>();
        // services.AddScoped<IBookingRepository, BookingRepository>();
        // services.AddScoped<IPaymentRepository, PaymentRepository>();
        // services.AddScoped<IDepositRepository, DepositRepository>();
        // services.AddScoped<IPromotionRepository, PromotionRepository>();
        // services.AddScoped<IProductRepository, ProductRepository>();
        // services.AddScoped<IIncidentRepository, IncidentRepository>();
        // services.AddScoped<IReviewRepository, ReviewRepository>();
        // services.AddScoped<INotificationRepository, NotificationRepository>();
        // services.AddScoped<IDashboardRepository, DashboardRepository>();
        return services;
    }
}
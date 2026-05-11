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
//using Microsoft.OpenApi.Models; // Không dùng vì phiên bản mới nhất không còn Models
using System.Text;
using System.Text.Json;

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
        var jwtSettings =
            config.GetSection("JwtSettings").Get<JwtSettings>()
            ?? throw new InvalidOperationException(
                "Khong tim thay hoac khong bind duoc JwtSettings.");

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

                        await context.Response.WriteAsync(
                            JsonSerializer.Serialize(
                                ApiResponse.Fail("Ban chua dang nhap hoac token khong hop le."),
                                jsonOptions));
                    },

                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";

                        await context.Response.WriteAsync(
                            JsonSerializer.Serialize(
                                ApiResponse.Fail("Ban khong co quyen thuc hien thao tac nay."),
                                jsonOptions));
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

            // Dung cho moi truong dev, khong dung o production
            options.AddPolicy("AllowAll", policy =>
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
        });

        return services;
    }

    // ============================================================
    // SWAGGER
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

    //public static IServiceCollection AddSwaggerWithAuth(
    //    this IServiceCollection services)
    //{
    //    services.AddSwaggerGen(options =>
    //    {
    //        options.SwaggerDoc("v1", new OpenApiInfo
    //        {
    //            Title = "SportPlus API",
    //            Version = "v1",
    //            Description = "He thong quan ly san bong SportPlus"
    //        });

    //        // Embed XML doc comment neu co
    //        var xmlPath = Path.Combine(
    //            AppContext.BaseDirectory,
    //            $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml");

    //        if (File.Exists(xmlPath))
    //            options.IncludeXmlComments(xmlPath);

    //        // Dinh nghia scheme Bearer
    //        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    //        {
    //            Name = "Authorization",
    //            Type = SecuritySchemeType.Http,
    //            Scheme = "bearer",
    //            BearerFormat = "JWT",
    //            In = ParameterLocation.Header,
    //            Description = "Nhap JWT access token. Swagger tu them prefix 'Bearer ' cho ban."
    //        });

    //        // Yeu cau Bearer tren moi request trong Swagger UI
    //        options.AddSecurityRequirement(new OpenApiSecurityRequirement
    //        {
    //            {
    //                new OpenApiSecurityScheme
    //                {
    //                    // Cách này cũ rồi, luôn báo lỗi cái Reference và không tồn tại OpenApiReference
    //                    Reference = new OpenApiReference
    //                    {
    //                        Type = ReferenceType.SecurityScheme,
    //                        Id   = "Bearer"
    //                    }
    //                },
    //                Array.Empty<string>()
    //            }
    //        });

    //        // Tranh trung ten khi co nhieu class cung ten o namespace khac nhau
    //        options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
    //    });

    //    return services;
    //}

    // ============================================================
    // APPLICATION SERVICES
    // ============================================================

    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        //services.AddScoped<IUserService, UserService>();

        // Them service moi o day khi implement them chuc nang:
        // services.AddScoped<IFieldService, FieldService>();
        // services.AddScoped<IBookingService, BookingService>();

        return services;
    }

    // ============================================================
    // REPOSITORIES
    // ============================================================

    public static IServiceCollection AddRepositories(
        this IServiceCollection services)
    {
        services.AddScoped<IAuthRepository, AuthRepository>();
        //services.AddScoped<IUserRepository, UserRepository>();

        // Them repository moi o day khi implement them chuc nang:
        // services.AddScoped<IFieldRepository, FieldRepository>();
        // services.AddScoped<IBookingRepository, BookingRepository>();

        return services;
    }
}
using KLCN_API.Extensions;
using KLCN_API.Filters;
using KLCN_API.Jobs;
using KLCN_API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ================================================================
// SERVICES
// ================================================================

builder.Services.AddControllers(options =>
    options.Filters.Add<ValidationFilter>()
);

builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
    options.SuppressModelStateInvalidFilter = true
);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddSwaggerWithAuth();
builder.Services.AddApplicationServices();
builder.Services.AddRepositories();

builder.Services.AddHostedService<ReleaseExpiredSlotsJob>();
builder.Services.AddHostedService<GenerateDailySlotsJob>();

// ================================================================
// PIPELINE
// ================================================================

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "SportPlus API V1");
    options.RoutePrefix = "";
    options.DisplayRequestDuration();
    options.DefaultModelsExpandDepth(-1);

    // ── Swagger UI login form ────────────────────────────────
    // Inject JavaScript để tự động lấy token từ response /api/auth/login
    // và set vào Authorize header — không cần copy paste thủ công
    options.HeadContent = """
        <script>
        (function () {
            const _fetch = window.fetch;
            window.fetch = async function (...args) {
                const res = await _fetch(...args);
                try {
                    const url = typeof args[0] === 'string' ? args[0] : (args[0]?.url ?? '');
                    if (url.includes('/api/auth/login') && res.ok) {
                        const clone = res.clone();
                        const json  = await clone.json();
                        // Thử cả camelCase lẫn PascalCase
                        const token = json?.data?.accessToken
                                   ?? json?.data?.AccessToken
                                   ?? json?.accessToken
                                   ?? json?.AccessToken;
                        if (token) {
                            // Đợi Swagger UI bundle load xong
                            const trySet = (attempts) => {
                                const ui = window.swaggerUIBundle;
                                if (ui && ui.preauthorizeHttpAuth) {
                                    ui.preauthorizeHttpAuth('Bearer', token);
                                    console.log('[SportPlus] Token da duoc tu dong set!');
                                } else if (ui && ui.preauthorizeApiKey) {
                                    ui.preauthorizeApiKey('Bearer', 'Bearer ' + token);
                                    console.log('[SportPlus] Token da duoc tu dong set (apiKey)!');
                                } else if (attempts > 0) {
                                    setTimeout(() => trySet(attempts - 1), 300);
                                }
                            };
                            trySet(10);
                        } else {
                            console.warn('[SportPlus] Khong tim thay token trong response:', json);
                        }
                    }
                } catch (e) {
                    console.warn('[SportPlus] Loi khi tu dong set token:', e);
                }
                return res;
            };
        })();
        </script>
        """;
});

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
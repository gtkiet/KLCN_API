namespace KLCN_API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ========================
            // 🔧 SERVICES
            // ========================

            builder.Services.AddControllers();

            // Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new()
                {
                    Title = "KLCN API",
                    Version = "v1",
                    Description = "API for KLCN project"
                });
            });

            // (Optional) CORS nếu bạn có frontend
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            // ========================
            // 🚀 PIPELINE
            // ========================

            // ⚠️ Luôn bật Swagger (Production vẫn dùng được)
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "KLCN API V1");

                // 👉 Mở domain là thấy Swagger luôn
                options.RoutePrefix = "";
            });

            // HTTPS (có thể tắt nếu hosting lỗi)
            app.UseHttpsRedirection();

            // CORS
            app.UseCors("AllowAll");

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
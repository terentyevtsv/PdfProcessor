using Infrastructure;
using Infrastructure.Data;
using Infrastructure.RabbitMQ;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApiGateway
{
    public class Startup
    {
        private IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // 1. PostgreSQL
            services.AddDbContext<DocumentsDbContext>(options =>
                options.UseNpgsql(_configuration.GetConnectionString("DefaultConnection")));

            // 2. RabbitMQ Producer
            var rabbitMqSettings = _configuration
                .GetSection("RabbitMQ")
                .Get<RabbitMqSettings>();
            services.AddSingleton(rabbitMqSettings);  // регистрируем настройки как синглтон
            services.AddSingleton<RabbitMqProducer>();

            // 3. Controllers
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                });
            services.AddSwaggerGen();

            // 4. Создаём папку для загрузки файлов
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Автоматические миграции при запуске
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DocumentsDbContext>();
                dbContext.Database.Migrate();
            }

            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}

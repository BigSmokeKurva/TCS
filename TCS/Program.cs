using Microsoft.EntityFrameworkCore;
using NLog.Web;
using Npgsql;
using TCS.BotsManager;
using TCS.Database;
using TCS.Filters;
using TCS.Follow;

namespace TCS
{
    public class Program
    {
        private static NpgsqlDataSource dataSource;

        public static async Task Main(string[] args)
        {
            // Настройка Playwright
            ConfigurePlaywright();

            var builder = WebApplication.CreateBuilder(args);

            // Настройка источника данных PostgreSQL
            ConfigurePostgresDataSource(builder.Configuration);

            // Настройка логирования
            ConfigureLogging(builder);

            // Настройка сервисов
            ConfigureServices(builder);

            // Создание и настройка веб-приложения
            var app = builder.Build();

            // Установка провайдера сервисов
            ServiceProviderAccessor.ServiceProvider = app.Services;

            // База данных: создание пользователя root при первом запуске (закомментировано)
            await InitializeRootUserAsync(app);

            // Регистрация Swagger в режиме разработки
            ConfigureSwaggerInDevelopment(app);

            // Настройка обработки HTTP-запросов и маршрутизации
            ConfigureHttp(app);

            // Передача конфига статичным классам
            User.ConnectThreads = app.Configuration.GetSection("App").GetValue<int>("ConnectThreads");
            User.DisconnectThreads = app.Configuration.GetSection("App").GetValue<int>("DisconnectThreads");
            TokenCheck.Threads = app.Configuration.GetSection("TokenCheck").GetValue<int>("Threads");
            FollowBot.Threads = app.Configuration.GetSection("FollowBot").GetValue<int>("Threads");

            // FollowBot
            FollowBot.StartPolling();

            // Запуск приложения
            await app.RunAsync();
        }
        private static void ConfigurePlaywright()
        {
            Microsoft.Playwright.Program.Main(new string[] { "install", "chromium" });
            Console.Clear();
        }
        private static void ConfigurePostgresDataSource(IConfiguration configuration)
        {
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = configuration.GetSection("Database:Host").Value,
                Username = configuration.GetSection("Database:Username").Value,
                Password = configuration.GetSection("Database:Password").Value,
                Database = configuration.GetSection("Database:DatabaseName").Value,
            };

            // Создание источника данных с поддержкой динамического JSON
            dataSource = new NpgsqlDataSourceBuilder(connectionStringBuilder.ConnectionString)
                .EnableDynamicJson()
                .Build();

            // Включение устаревшего поведения временных меток для Npgsql
            //AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }

        private static void ConfigureLogging(WebApplicationBuilder builder)
        {
            NLog.LogManager.Setup().LoadConfigurationFromAppSettings();
            builder.Logging.ClearProviders();
            builder.Host.UseNLog();
        }

        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            builder.Services.AddDbContext<DatabaseContext>(optionsBuilder =>
            {
                optionsBuilder.UseLazyLoadingProxies().UseNpgsql(dataSource);
            });

            builder.Services.AddRazorPages();
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddMvc();
            builder.Services.AddScoped<AdminAuthorizationFilter>();
            builder.Services.AddScoped<UserAuthorizationFilter>();
            if (!string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
            {
                builder.WebHost.ConfigureKestrel(options =>
                {
                    options.ListenAnyIP(80);
                    options.ListenAnyIP(443, listenOptions =>
                    {
                        listenOptions.UseHttps("cert.pfx", "iop3360A");
                    });
                });
            }
        }

        private static void ConfigureSwaggerInDevelopment(WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
        }

        private static async Task InitializeRootUserAsync(WebApplication app)
        {
            var serviceProvider = ServiceProviderAccessor.ServiceProvider;
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

            var username = "root";
            var password = app.Configuration.GetSection("RootAccount:Password").Value;
            var email = "root@root.com";
            db.Database.EnsureCreated();
            var existingUser = db.Users
                .FirstOrDefault(u => u.Username == username || u.Email == email);

            if (existingUser is not null)
            {
                existingUser.Password = password;
                db.SaveChanges();
            }
            else
            {
                var newUser = new Database.Models.User
                {
                    Username = username,
                    Password = password,
                    Email = email,
                    Admin = true,
                    Configuration = new Database.Models.Configuration(),
                };

                await db.Users.AddAsync(newUser);
                await db.SaveChangesAsync();
            }
        }

        private static void ConfigureHttp(WebApplication app)
        {
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseExceptionHandler("/Error");
            app.UseRouting();
            app.UseHttpsRedirection();

            app.MapControllers();
            app.MapRazorPages();
        }
    }
}
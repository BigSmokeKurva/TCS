using System.Net;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Web;
using Npgsql;
using TCS.Server.Database;
using TCS.Server.Database.Models;
using TCS.Server.Filters;
using TCS.Server.Follow;
using TCS.Server.Services;
using User = TCS.Server.BotsManager.User;

namespace TCS.Server.Server;

public class Program
{
    private static NpgsqlDataSource dataSource;

    public static async Task Main(string[] args)
    {
        // ��������� Playwright
        ConfigurePlaywright();

        var builder = WebApplication.CreateBuilder(args);

        // ��������� ��������� ������ PostgreSQL
        ConfigurePostgresDataSource(builder.Configuration);

        // ��������� �����������
        ConfigureLogging(builder);

        // ��������� ��������
        ConfigureServices(builder);

        // �������� � ��������� ���-����������
        var app = builder.Build();

        // ��������� ���������� ��������
        ServiceProviderAccessor.ServiceProvider = app.Services;

        // ���� ������: �������� ������������ root ��� ������ ������� (����������������)
        await InitializeRootUserAsync(app);

        // ��������� ��������� HTTP-�������� � �������������
        ConfigureApp(app);

        // �������� ������� ��������� �������
        User.ConnectThreads = app.Configuration.GetSection("App").GetValue<int>("ConnectThreads");
        User.DisconnectThreads = app.Configuration.GetSection("App").GetValue<int>("DisconnectThreads");
        TokenCheck.Threads = app.Configuration.GetSection("TokenCheck").GetValue<int>("Threads");
        FollowBot.Threads = app.Configuration.GetSection("FollowBot").GetValue<int>("Threads");

        // FollowBot
        FollowBot.StartPolling();

        // ������ ����������
        await app.RunAsync();
    }

    private static void ConfigurePlaywright()
    {
        Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
        Console.Clear();
    }

    private static void ConfigurePostgresDataSource(IConfiguration configuration)
    {
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = configuration.GetSection("Database:Host").Value,
            Username = configuration.GetSection("Database:Username").Value,
            Password = configuration.GetSection("Database:Password").Value,
            Database = configuration.GetSection("Database:DatabaseName").Value
        };

        // �������� ��������� ������ � ���������� ������������� JSON
        dataSource = new NpgsqlDataSourceBuilder(connectionStringBuilder.ConnectionString)
            .EnableDynamicJson()
            .Build();
    }

    private static void ConfigureLogging(WebApplicationBuilder builder)
    {
        LogManager.Setup().LoadConfigurationFromAppSettings();
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

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddScoped<AdminAuthorizationFilter>();
        builder.Services.AddScoped<UserAuthorizationFilter>();
        builder.Services.AddHostedService<SessionExpiresCheckService>();
        builder.Services.AddHostedService<LastOnlineCheckService>();
        builder.Services.AddHostedService<InviteCodeExpiresCheckService>();
        builder.Services.AddSingleton<HttpClient>();
        var ip = IPAddress.Parse(builder.Configuration.GetValue<string>("IP"));
        if (!string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Listen(ip, 80);
                options.Listen(ip, 443,
                    listenOptions => { listenOptions.UseHttps("cert.pfx", "iop3360A"); });
            });
    }

    private static async Task InitializeRootUserAsync(WebApplication app)
    {
        var serviceProvider = ServiceProviderAccessor.ServiceProvider;
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        var username = "root";
        var password = app.Configuration.GetSection("RootAccount:Password").Value;
        db.Database.EnsureCreated();
        var existingUser = db.Users
            .FirstOrDefault(u => u.Username == username);

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
                Admin = true,
                Configuration = new Configuration()
            };

            await db.Users.AddAsync(newUser);

            await db.SaveChangesAsync();
        }

        await db.SaveChangesAsync();
    }

    private static void ConfigureApp(WebApplication app)
    {
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseHttpsRedirection();
        //app.UseMiddleware<PagesAccessMiddleware>();
        app.MapControllers();
        app.MapFallbackToFile("/index.html");
    }
}
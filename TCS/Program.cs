using TCS.Filters;

namespace TCS
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Config
            Configuration.Init();

            // DB
            await Database.Init();

            builder.Services.AddRazorPages();
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddMvc();
            builder.Services.AddScoped<AdminAuthorizationFilter>();
            builder.Services.AddScoped<UserAuthorizationFilter>();
            builder.Services.AddHttpsRedirection(options =>
            {
                options.HttpsPort = 443;
            });
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(80);
                options.ListenAnyIP(443, listenOptions =>
                {
                    listenOptions.UseHttps("cert.pfx", "iop3360A");
                });
            });
            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseExceptionHandler("/Error");
            app.UseRouting();
            app.UseHttpsRedirection();

            app.MapControllers();
            app.MapRazorPages();

            await app.RunAsync();
        }
    }
}
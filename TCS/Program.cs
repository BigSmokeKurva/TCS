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
            //
            builder.Services.AddRazorPages();
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddMvc();
            builder.Services.AddScoped<AuthTokenPageFilter>();
            var app = builder.Build();
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseExceptionHandler("/Error");
            app.MapControllers();
            app.UseRouting();
            app.MapRazorPages();
            //app.UseEndpoints(endpoints =>
            //{

            //    endpoints.MapControllerRoute(
            //        name: "page",
            //        pattern: "App/LoadPartialView",
            //        defaults: new { controller = "App", action = "LoadPartialView" }
            //    );
            //});
            app.MapControllerRoute(
                    name: "page",
                    pattern: "App/LoadPartialView",
                    defaults: new { controller = "App", action = "LoadPartialView" }
                );

            await app.RunAsync();
        }
    }
}
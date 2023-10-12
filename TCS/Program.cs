namespace TCS
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddRazorPages();
            // Config
            Configuration.Init();
            // DB
            await Database.Init();
            //
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();
            //app.MapPost("/api/registration", async (HttpRequest request) =>
            //{
            //    var body = new StreamReader(request.Body);
            //    string postData = await body.ReadToEndAsync();
            //    Console.WriteLine(postData);
            //    return "Hellol";
            //});
            app.UseExceptionHandler("/Error");
            //app.UseHttpsRedirection();
            //app.UseAuthorization();
            app.MapControllers();
            app.UseRouting();
            app.MapRazorPages();
            await app.RunAsync();
        }
    }
}
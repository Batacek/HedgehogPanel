using Microsoft.Extensions.FileProviders;

namespace HedgehogPanel;

class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var app = builder.Build();

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "html")),
            RequestPath = "/html"
        });

        app.MapGet("/", (IWebHostEnvironment env) =>
            Results.File(Path.Combine(env.ContentRootPath, "html", "index.html"), "text/html; charset=utf-8")
        );

        app.Run();
    }
}
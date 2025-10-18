using Microsoft.Extensions.FileProviders;

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
using HedgehogPanel.Extensions;
using Microsoft.AspNetCore.Builder;

namespace HedgehogPanel;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddHedgehogServices(args);

        var app = builder.Build();

        app.UseHedgehogMiddleware();

        app.Run();
    }
}
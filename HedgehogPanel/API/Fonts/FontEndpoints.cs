using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HedgehogPanel.API.Fonts;

public static class FontEndpoints
{
    // Hardcoded font map: local filename → CDN URL (fallback when file not present locally)
    private static readonly Dictionary<string, string> KnownFonts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["inter-latin.woff2"]     = "https://fonts.gstatic.com/s/inter/v20/UcC73FwrK3iLTeHuS_nVMrMxCp50SjIa1ZL7.woff2",
        ["inter-latin-ext.woff2"] = "https://fonts.gstatic.com/s/inter/v20/UcC73FwrK3iLTeHuS_nVMrMxCp50SjIa25L7SUc.woff2",
    };

    public static IEndpointRouteBuilder MapFontEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/fonts/{filename}", async (
            string filename,
            HttpContext ctx,
            IWebHostEnvironment env,
            IHttpClientFactory httpClientFactory) =>
        {
            // Only serve known fonts — prevents path traversal and enumeration
            if (!KnownFonts.TryGetValue(filename, out var cdnUrl))
                return Results.NotFound();

            ctx.Response.Headers.CacheControl = "public, max-age=31536000, immutable";

            var localPath = Path.Combine(env.ContentRootPath, "Web", "wwwroot", "fonts", filename);

            if (File.Exists(localPath))
            {
                return Results.File(localPath, "font/woff2",
                    entityTag: new Microsoft.Net.Http.Headers.EntityTagHeaderValue($"\"{filename}\""),
                    enableRangeProcessing: false);
            }

            // File not present locally — proxy from CDN transparently
            // (redirect would be blocked by CSP font-src 'self')
            var client = httpClientFactory.CreateClient("FontProxy");
            try
            {
                var bytes = await client.GetByteArrayAsync(cdnUrl);
                return Results.Bytes(bytes, "font/woff2");
            }
            catch (HttpRequestException)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        })
        .AllowAnonymous();

        return endpoints;
    }
}

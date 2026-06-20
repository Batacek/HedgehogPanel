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
    // CDN fallback map: filename → CDN URL, used only when the font is missing locally.
    // Any .woff2 present in wwwroot/fonts is served directly regardless of this map.
    private static readonly Dictionary<string, string> KnownFonts = new(StringComparer.OrdinalIgnoreCase)
    {
        // Login page fonts (static Inter, weight 400).
        ["inter-latin.woff2"]     = "https://fonts.gstatic.com/s/inter/v20/UcC73FwrK3iLTeHuS_nVMrMxCp50SjIa1ZL7.woff2",
        ["inter-latin-ext.woff2"] = "https://fonts.gstatic.com/s/inter/v20/UcC73FwrK3iLTeHuS_nVMrMxCp50SjIa25L7SUc.woff2",

        // Panel fonts (variable Inter, fontsource); subset per file, weight axis covers all weights.
        ["inter-01.woff2"] = "https://cdn.jsdelivr.net/fontsource/fonts/inter:vf@latest/cyrillic-ext-wght-normal.woff2",
        ["inter-02.woff2"] = "https://cdn.jsdelivr.net/fontsource/fonts/inter:vf@latest/cyrillic-wght-normal.woff2",
        ["inter-03.woff2"] = "https://cdn.jsdelivr.net/fontsource/fonts/inter:vf@latest/greek-ext-wght-normal.woff2",
        ["inter-04.woff2"] = "https://cdn.jsdelivr.net/fontsource/fonts/inter:vf@latest/greek-wght-normal.woff2",
        ["inter-05.woff2"] = "https://cdn.jsdelivr.net/fontsource/fonts/inter:vf@latest/vietnamese-wght-normal.woff2",
        ["inter-06.woff2"] = "https://cdn.jsdelivr.net/fontsource/fonts/inter:vf@latest/latin-ext-wght-normal.woff2",
        ["inter-07.woff2"] = "https://cdn.jsdelivr.net/fontsource/fonts/inter:vf@latest/latin-wght-normal.woff2",
        ["inter-08.woff2"] = "https://cdn.jsdelivr.net/fontsource/fonts/inter:vf@latest/cyrillic-ext-wght-normal.woff2",
        ["inter-09.woff2"] = "https://cdn.jsdelivr.net/fontsource/fonts/inter:vf@latest/cyrillic-wght-normal.woff2",
        ["inter-10.woff2"] = "https://cdn.jsdelivr.net/fontsource/fonts/inter:vf@latest/greek-wght-normal.woff2",
        ["inter-11.woff2"] = "https://cdn.jsdelivr.net/fontsource/fonts/inter:vf@latest/vietnamese-wght-normal.woff2",
        ["inter-12.woff2"] = "https://cdn.jsdelivr.net/fontsource/fonts/inter:vf@latest/latin-ext-wght-normal.woff2",
        ["inter-13.woff2"] = "https://cdn.jsdelivr.net/fontsource/fonts/inter:vf@latest/latin-wght-normal.woff2",
    };

    public static IEndpointRouteBuilder MapFontEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/fonts/{filename}", async (
            string filename,
            HttpContext ctx,
            IWebHostEnvironment env,
            IHttpClientFactory httpClientFactory) =>
        {
            // Accept only a bare .woff2 filename — blocks path traversal / enumeration.
            var safeName = Path.GetFileName(filename);
            if (safeName != filename || !safeName.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase))
                return Results.NotFound();

            ctx.Response.Headers.CacheControl = "public, max-age=31536000, immutable";

            // Serve any font that physically exists in the fonts folder.
            var localPath = Path.Combine(env.ContentRootPath, "Web", "wwwroot", "fonts", safeName);
            if (File.Exists(localPath))
            {
                return Results.File(localPath, "font/woff2",
                    entityTag: new Microsoft.Net.Http.Headers.EntityTagHeaderValue($"\"{safeName}\""),
                    enableRangeProcessing: false);
            }

            // Not present locally — fall back to the CDN for fonts we have a URL for
            // (transparent proxy; a redirect would be blocked by CSP font-src 'self').
            if (KnownFonts.TryGetValue(safeName, out var cdnUrl))
            {
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
            }

            return Results.NotFound();
        })
        .AllowAnonymous();

        return endpoints;
    }
}

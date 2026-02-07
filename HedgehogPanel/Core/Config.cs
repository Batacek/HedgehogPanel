using DotNetEnv;

namespace HedgehogPanel.Core;

public static class Config
{
    // Ensures .env variables are loaded when accessed in contexts that may run before Program invoked Env.Load()
    private static bool _envLoaded;
    private static void EnsureEnvLoaded()
    {
        if (_envLoaded) return;
        try
        {
            Env.Load();
        }
        catch
        {
            // ignore
        }
        _envLoaded = true;
    }

    public static string? Get(string key, string? defaultValue = null)
    {
        EnsureEnvLoaded();
        var value = Env.GetString(key);
        return string.IsNullOrEmpty(value) ? defaultValue : value;
    }

    public static string JwtSecret
    {
        get
        {
            EnsureEnvLoaded();
            return Env.GetString("JWT_SECRET");
        }
    }

    public static string JwtIssuer => Get("JWT_ISSUER", "HedgehogPanel")!;
    public static string JwtAudience => Get("JWT_AUDIENCE", "HedgehogPanel.Client")!;
}
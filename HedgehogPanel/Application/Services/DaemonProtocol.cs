using System;
using System.Globalization;
using HedgehogPanel.Domain.Enums;

namespace HedgehogPanel.Application.Services;

/// <summary>
/// The panel's side of the protocol version check.
/// </summary>
public static class DaemonProtocol
{
    /// <summary>Protocol version of the proto/hedgehog.proto this panel is built against.</summary>
    public const string Version = "2.0.0";

    /// <summary>Judges a daemon's reported protocol version against this panel's.</summary>
    public static ProtocolCompatibility Evaluate(string? daemonVersion) => Evaluate(Version, daemonVersion);

    /// <summary>Applies the compatibility rules from the header of hedgehog.proto.</summary>
    public static ProtocolCompatibility Evaluate(string panelVersion, string? daemonVersion)
    {
        if (!TryParse(panelVersion, out var panel) || !TryParse(daemonVersion, out var daemon))
        {
            // Includes daemons from before protocol 2.0.0, which report no version at all.
            return ProtocolCompatibility.Incompatible;
        }

        if (panel.Major == daemon.Major)
        {
            return panel.Minor == daemon.Minor ? ProtocolCompatibility.Full : ProtocolCompatibility.Limited;
        }

        // A pre-release build talking to the first stable line still gets the basics.
        if (Math.Min(panel.Major, daemon.Major) == 0 && Math.Max(panel.Major, daemon.Major) == 1)
        {
            return ProtocolCompatibility.LimitedPreRelease;
        }

        return ProtocolCompatibility.Incompatible;
    }

    private static bool TryParse(string? value, out (int Major, int Minor, int Patch) version)
    {
        version = default;
        var parts = value?.Split('.');
        if (parts is not { Length: 3 })
        {
            return false;
        }

        // NumberStyles.None: no signs, no whitespace, nothing a lenient parse would let through.
        if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var major)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var minor)
            || !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var patch))
        {
            return false;
        }

        version = (major, minor, patch);
        return true;
    }
}

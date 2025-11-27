namespace HedgehogPanel.Servers;

public class ServerConfig
{
    public string IPAddress { get; private set; }
    public string SubnetMask { get; private set; }
    public byte SubnetMaskLength { get; private set; }
    public string Gateway { get; private set; }
    public string DNSPrimary { get; private set; }
    public string? DNSSecondary { get; private set; }
    public string Hostname { get; private set; }
    public string? Domain { get; private set; }
    public bool OSType { get; private set; } // true = Linux, false = Other
    public string OSName { get; private set; }
    public string OSVersion { get; private set; }
    public TimeZoneInfo TimeZone { get; private set; }
    public string? Location { get; private set; }
}
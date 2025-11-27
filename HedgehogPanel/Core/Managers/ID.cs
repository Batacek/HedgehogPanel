using Serilog;

namespace HedgehogPanel.Core.Managers;

internal class ID
{
    private static readonly Serilog.ILogger Logger = Log.ForContext(typeof(ID));
    private readonly HashSet<Guid> _guids = new HashSet<Guid>();
    private readonly object _threadLock = new object();
    
    internal static readonly ID Instance = new ID();

    private ID() { }

    internal Guid GenerateGUID()
    {
        Guid guid;
        lock (_threadLock)
        {
            guid = Guid.NewGuid();
            var attempts = 1;
            while(!_guids.Add(guid))
            {
                attempts++;
                Logger.Warning("GUID collision detected. Generating a new GUID. Attempts: {Attempts}", attempts);
                guid = Guid.NewGuid();
            }
            Logger.Debug("Generated new GUID {Guid} after {Attempts} attempt(s).", guid, attempts);
            return guid;
        }
    }
}
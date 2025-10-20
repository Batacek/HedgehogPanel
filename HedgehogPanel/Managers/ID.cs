namespace HedgehogPanel.Managers;

internal class ID
{
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
            
            while(!_guids.Add(guid))
            {
                guid = Guid.NewGuid();
            }

            return guid;
        }
    }
}
using HedgehogPanel.Core.Logging;
using HedgehogPanel.UserManagment;
using HedgehogPanel.Servers;
using HedgehogPanel.Core.Managers;
using HedgehogPanel.Core.Configuration;

namespace HedgehogPanel.Core.Store;

public class DataProvider : IDataProvider
{
    private readonly IInMemoryStore _store;
    private readonly IAccountManager _accountManager;
    private readonly IServerManager _serverManager;
    private readonly ILoggerService _logger;
    private readonly HedgehogConfig _config;

    public DataProvider(
        IInMemoryStore store, 
        IAccountManager accountManager, 
        IServerManager serverManager, 
        ILoggerService logger,
        HedgehogConfig config)
    {
        _store = store;
        _accountManager = accountManager;
        _serverManager = serverManager;
        _logger = logger;
        _config = config;
    }

    public async Task<Account?> GetAccountByUsernameAsync(string username)
    {
        if (!_config.Cache.Enabled)
        {
            return await _accountManager.GetAccountByUsernameAsync(username);
        }

        var relationKey = $"UserByUsername:{username.ToLowerInvariant()}";
        var existingId = _store.GetRelation(relationKey).FirstOrDefault();

        if (existingId != Guid.Empty)
        {
            var cached = _store.Get<Account>(existingId);
            if (cached != null) return cached;
        }

        var account = await _accountManager.GetAccountByUsernameAsync(username);
        if (account != null)
        {
            _store.Set(account.GUID, account);
            _store.SetRelation(relationKey, new[] { account.GUID });
        }

        return account;
    }

    public async Task<IReadOnlyList<Server>> GetServersByUserIdAsync(Guid userId)
    {
        if (!_config.Cache.Enabled)
        {
            return await _accountManager.GetServerListAsync(userId);
        }

        var relationKey = $"UserServers:{userId}";
        var serverIds = _store.GetRelation(relationKey);

        if (serverIds.Count > 0)
        {
            var servers = new List<Server>();
            bool allFound = true;
            foreach (var id in serverIds)
            {
                var s = _store.Get<Server>(id);
                if (s == null)
                {
                    allFound = false;
                    break;
                }
                servers.Add(s);
            }

            if (allFound) return servers;
        }

        var dbServers = await _accountManager.GetServerListAsync(userId);
        foreach (var s in dbServers)
        {
            _store.Set(s.GUID, s);
        }
        _store.SetRelation(relationKey, dbServers.Select(s => s.GUID));

        return dbServers;
    }

    public async Task WarmupAsync(Guid userId)
    {
        if (!_config.Cache.Enabled || !_config.Cache.Warmup.Enabled) return;

        _logger.Information("Warming up cache for user {UserId} with depth {Depth}", userId, _config.Cache.Warmup.Depth);

        // Depth 1: User
        
        if (_config.Cache.Warmup.Depth >= 2)
        {
            // Depth 2: Servers
            await GetServersByUserIdAsync(userId);
        }
        
        if (_config.Cache.Warmup.Depth >= 3)
        {
        }
    }

    public void InvalidateAccount(Guid userId)
    {
        _store.Remove<Account>(userId);
    }

    public void InvalidateServer(Guid serverId)
    {
        _store.Remove<Server>(serverId);
    }
}

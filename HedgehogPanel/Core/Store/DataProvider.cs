using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    public Task WarmupAsync(Guid userId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                // Defensive checks and progress indication
                if (!_config.Cache.Enabled || _config.Cache.Warmup == null || !_config.Cache.Warmup.Enabled) return;
                var depth = _config.Cache.Warmup.Depth;

                _logger.Information("Warming up cache for user {UserId} with depth {Depth}", userId, depth);

                // Depth 1: User
                if (depth >= 1)
                {
                    try
                    {
                        var account = await _accountManager.GetAccountByIdAsync(userId);
                        if (account != null)
                        {
                            _store.Set(account.GUID, account);
                            _logger.Debug("Depth 1 warmup completed for user {UserId}", userId);
                        }
                        else
                        {
                            _logger.Warning("Depth 1 warmup: user {UserId} not found.", userId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Depth 1 warmup failed for user {UserId}", userId);
                    }
                }
                
                if (depth >= 2)
                {
                    // Depth 2: Servers
                    try
                    {
                        await GetServersByUserIdAsync(userId);
                        _logger.Debug("Depth 2 warmup completed for user {UserId}", userId);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Depth 2 warmup failed for user {UserId}", userId);
                    }
                }
                
                if (depth >= 3)
                {
                }
                
                _logger.Information("Cache warmup completed for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Cache warmup top-level failure for user {UserId}", userId);
            }
        });

        return Task.CompletedTask;
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

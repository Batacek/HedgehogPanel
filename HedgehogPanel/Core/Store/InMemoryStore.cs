using System.Collections.Concurrent;
using HedgehogPanel.Core.Logging;
using HedgehogPanel.Core.Configuration;

namespace HedgehogPanel.Core.Store;

public class InMemoryStore : IInMemoryStore
{
    private readonly ILoggerService _logger;
    private readonly HedgehogConfig _config;
    private readonly ConcurrentDictionary<string, CacheEntry> _entities = new();
    private readonly ConcurrentDictionary<string, CacheEntry> _relations = new();
    private readonly ConcurrentDictionary<string, Task> _inflightLoads = new();

    private record CacheEntry(object Data, DateTime? Expiry);

    public InMemoryStore(ILoggerService logger, HedgehogConfig config)
    {
        _logger = logger;
        _config = config;
    }

    private string GetKey<T>(Guid id) => $"{typeof(T).Name}:{id}";

    private DateTime? GetExpiry() 
    {
        if (_config.Cache.TtlMinutes <= 0) return null;
        return DateTime.UtcNow.AddMinutes(_config.Cache.TtlMinutes);
    }

    private bool IsExpired(CacheEntry entry)
    {
        return entry.Expiry.HasValue && entry.Expiry.Value < DateTime.UtcNow;
    }

    public T? Get<T>(Guid id) where T : class
    {
        var key = GetKey<T>(id);
        if (_entities.TryGetValue(key, out var entry))
        {
            if (IsExpired(entry))
            {
                _entities.TryRemove(key, out _);
                _logger.Debug("Entity {Key} expired and was removed.", key);
                return null;
            }
            return (T)entry.Data;
        }
        return null;
    }

    public void Set<T>(Guid id, T entity) where T : class
    {
        _entities[GetKey<T>(id)] = new CacheEntry(entity, GetExpiry());
        _logger.Debug("Stored entity {Type}:{Id} in memory store.", typeof(T).Name, id);
    }

    public void Remove<T>(Guid id) where T : class
    {
        _entities.TryRemove(GetKey<T>(id), out _);
        _logger.Debug("Removed entity {Type}:{Id} from memory store.", typeof(T).Name, id);
    }

    public IReadOnlyList<Guid> GetRelation(string key)
    {
        if (_relations.TryGetValue(key, out var entry))
        {
            if (IsExpired(entry))
            {
                _relations.TryRemove(key, out _);
                _logger.Debug("Relation {Key} expired and was removed.", key);
                return Array.Empty<Guid>();
            }
            return (IReadOnlyList<Guid>)entry.Data;
        }
        return Array.Empty<Guid>();
    }

    public void SetRelation(string key, IEnumerable<Guid> ids)
    {
        _relations[key] = new CacheEntry(ids.ToList(), GetExpiry());
    }

    public void RemoveRelation(string key)
    {
        _relations.TryRemove(key, out _);
    }

    public async Task<T> GetOrLoadAsync<T>(Guid id, Func<Guid, Task<T>> loader) where T : class
    {
        var key = GetKey<T>(id);

        var cached = Get<T>(id);
        if (cached != null)
        {
            return cached;
        }

        var tcs = new TaskCompletionSource<T>();
        if (_inflightLoads.TryAdd(key, tcs.Task))
        {
            try
            {
                var loaded = await loader(id);
                Set(id, loaded);
                tcs.SetResult(loaded);
                return loaded;
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
                throw;
            }
            finally
            {
                _inflightLoads.TryRemove(key, out _);
            }
        }
        else
        {
            if (_inflightLoads.TryGetValue(key, out var task))
            {
                return await (Task<T>)task;
            }

            var cachedAfter = Get<T>(id);
            if (cachedAfter != null)
            {
                return cachedAfter;
            }
            
            // Should not happen with proper sync, but for safety:
            return await loader(id);
        }
    }
}

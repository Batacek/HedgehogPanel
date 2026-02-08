using System.Collections.Concurrent;
using HedgehogPanel.Core.Logging;

namespace HedgehogPanel.Core.Store;

/// <summary>
/// Defines an interface for an in-memory storage system capable of managing entities,
/// their associations, and loading mechanisms.
/// </summary>
public interface IInMemoryStore
{
    /// <summary>
    /// Retrieves an entity of type <typeparamref name="T"/> from the in-memory store by its unique identifier.
    /// </summary>
    /// <typeparam name="T">The type of the entity to retrieve. Must be a reference type.</typeparam>
    /// <param name="id">The unique identifier of the entity to retrieve.</param>
    /// <returns>
    /// The entity associated with the specified identifier, or <c>null</c> if the entity is not found in the store.
    /// </returns>
    T? Get<T>(Guid id) where T : class;

    /// <summary>
    /// Stores an entity of type <typeparamref name="T"/> in the in-memory store with its associated unique identifier.
    /// </summary>
    /// <typeparam name="T">The type of the entity to store. Must be a reference type.</typeparam>
    /// <param name="id">The unique identifier to associate with the entity.</param>
    /// <param name="entity">The entity to store in the in-memory store.</param>
    void Set<T>(Guid id, T entity) where T : class;

    /// <summary>
    /// Removes an entity of type <typeparamref name="T"/> from the in-memory store using its unique identifier.
    /// </summary>
    /// <typeparam name="T">The type of the entity to remove. Must be a reference type.</typeparam>
    /// <param name="id">The unique identifier of the entity to be removed from the store.</param>
    void Remove<T>(Guid id) where T : class;

    /// <summary>
    /// Retrieves the list of unique identifiers associated with a specified relation key in the in-memory store.
    /// </summary>
    /// <param name="key">The key representing the relation whose associated unique identifiers are to be retrieved.</param>
    /// <returns>
    /// A read-only list of unique identifiers associated with the specified relation key.
    /// If no identifiers are associated with the key, an empty list is returned.
    /// </returns>
    IReadOnlyList<Guid> GetRelation(string key);

    /// <summary>
    /// Associates a set of unique identifiers with a specific key in the in-memory store, replacing any existing associations for the key.
    /// </summary>
    /// <param name="key">A string representing the key to which the identifiers will be associated.</param>
    /// <param name="ids">A collection of unique identifiers to associate with the specified key.</param>
    void SetRelation(string key, IEnumerable<Guid> ids);

    /// <summary>
    /// Removes a relation from the in-memory store using the specified key.
    /// </summary>
    /// <param name="key">The unique key identifying the relation to be removed.</param>
    void RemoveRelation(string key);

    /// <summary>
    /// Attempts to retrieve an entity of type <typeparamref name="T"/> from the in-memory store by its unique identifier.
    /// If the entity is not found in the store, it is loaded using the provided loader function and then cached.
    /// </summary>
    /// <typeparam name="T">The type of the entity to retrieve or load. Must be a reference type.</typeparam>
    /// <param name="id">The unique identifier of the entity to retrieve or load.</param>
    /// <param name="loader">
    /// A function that takes the unique identifier as input and returns a task that asynchronously loads the entity.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. On completion, the task's result contains the entity either
    /// retrieved from the store or loaded via the loader function.
    /// </returns>
    Task<T> GetOrLoadAsync<T>(Guid id, Func<Guid, Task<T>> loader) where T : class;
}
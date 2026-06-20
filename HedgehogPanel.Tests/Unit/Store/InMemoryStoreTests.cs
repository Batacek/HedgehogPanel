using System;
using System.Threading.Tasks;
using HedgehogPanel.Application.Contracts.Logging;
using HedgehogPanel.Domain.Entities;
using HedgehogPanel.Infrastructure.Configuration;
using HedgehogPanel.Infrastructure.Persistence.Store;
using Moq;
using Xunit;

namespace HedgehogPanel.Tests.Unit.Store;

public class InMemoryStoreTests
{
    private static InMemoryStore CreateStore()
    {
        var config = new HedgehogConfig { Cache = new CacheConfig { Enabled = true, TtlMinutes = 0 } };
        return new InMemoryStore(Mock.Of<ILoggerService>(), config);
    }

    private static Server NewServer() => new(Guid.NewGuid(), "srv", "host.hedgehog.batacek.eu");

    [Fact]
    public void Set_ThenGet_ReturnsSameEntity()
    {
        var store = CreateStore();
        var server = NewServer();

        store.Set(server.Guid, server);

        Assert.Same(server, store.Get<Server>(server.Guid));
    }

    [Fact]
    public void Get_WhenNotStored_ReturnsNull()
    {
        var store = CreateStore();
        Assert.Null(store.Get<Server>(Guid.NewGuid()));
    }

    [Fact]
    public void Remove_ThenGet_ReturnsNull()
    {
        var store = CreateStore();
        var server = NewServer();
        store.Set(server.Guid, server);

        store.Remove<Server>(server.Guid);

        Assert.Null(store.Get<Server>(server.Guid));
    }

    [Fact]
    public void Get_WithDifferentType_DoesNotReturnEntityOfAnotherType()
    {
        var store = CreateStore();
        var id = Guid.NewGuid();
        store.Set(id, new Server(id, "srv", "host.hedgehog.batacek.eu"));

        // Same id but a different entity type must not collide.
        Assert.Null(store.Get<Node>(id));
    }

    [Fact]
    public void Set_Twice_OverwritesPreviousValue()
    {
        var store = CreateStore();
        var id = Guid.NewGuid();
        var first = new Server(id, "first", "host.hedgehog.batacek.eu");
        var second = new Server(id, "second", "host.hedgehog.batacek.eu");

        store.Set(id, first);
        store.Set(id, second);

        Assert.Same(second, store.Get<Server>(id));
    }

    [Fact]
    public void SetRelation_ThenGetRelation_ReturnsIds()
    {
        var store = CreateStore();
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };

        store.SetRelation("rel", ids);

        Assert.Equal(ids, store.GetRelation("rel"));
    }

    [Fact]
    public void GetRelation_WhenNotSet_ReturnsEmpty()
    {
        var store = CreateStore();
        Assert.Empty(store.GetRelation("missing"));
    }

    [Fact]
    public void RemoveRelation_ThenGetRelation_ReturnsEmpty()
    {
        var store = CreateStore();
        store.SetRelation("rel", new[] { Guid.NewGuid() });

        store.RemoveRelation("rel");

        Assert.Empty(store.GetRelation("rel"));
    }

    [Fact]
    public async Task GetOrLoadAsync_WhenNotCached_InvokesLoaderOnceAndCaches()
    {
        var store = CreateStore();
        var server = NewServer();
        var calls = 0;
        Task<Server> Loader(Guid _) { calls++; return Task.FromResult(server); }

        var firstResult = await store.GetOrLoadAsync(server.Guid, Loader);
        var secondResult = await store.GetOrLoadAsync(server.Guid, Loader);

        Assert.Same(server, firstResult);
        Assert.Same(server, secondResult);
        Assert.Equal(1, calls); // Second call is served from cache.
    }

    [Fact]
    public async Task GetOrLoadAsync_WhenAlreadyCached_DoesNotInvokeLoader()
    {
        var store = CreateStore();
        var server = NewServer();
        store.Set(server.Guid, server);

        var result = await store.GetOrLoadAsync<Server>(server.Guid,
            _ => throw new InvalidOperationException("loader must not be called"));

        Assert.Same(server, result);
    }
}

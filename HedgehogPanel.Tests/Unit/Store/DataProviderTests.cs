using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HedgehogPanel.Application.Contracts.Logging;
using HedgehogPanel.Application.Services;
using HedgehogPanel.Domain.Entities;
using HedgehogPanel.Infrastructure.Configuration;
using HedgehogPanel.Infrastructure.Persistence.Store;
using Moq;
using Xunit;

namespace HedgehogPanel.Tests.Unit.Store;

public class DataProviderTests
{
    private readonly Mock<IAccountService> _accountService = new();
    private readonly Mock<IServerService> _serverService = new();

    private DataProvider CreateProvider(bool cacheEnabled, out InMemoryStore store)
    {
        var config = new HedgehogConfig { Cache = new CacheConfig { Enabled = cacheEnabled, TtlMinutes = 0 } };
        store = new InMemoryStore(Mock.Of<ILoggerService>(), config);
        return new DataProvider(store, _accountService.Object, _serverService.Object, Mock.Of<ILoggerService>(), config);
    }

    [Fact]
    public async Task GetAccountByUsernameAsync_WhenCacheDisabled_CallsServiceEveryTime()
    {
        var provider = CreateProvider(cacheEnabled: false, out _);
        var account = new Account(Guid.NewGuid(), "joe", "joe@hedgehog.batacek.eu");
        _accountService.Setup(s => s.GetAccountByUsernameAsync("joe")).ReturnsAsync(account);

        await provider.GetAccountByUsernameAsync("joe");
        await provider.GetAccountByUsernameAsync("joe");

        _accountService.Verify(s => s.GetAccountByUsernameAsync("joe"), Times.Exactly(2));
    }

    [Fact]
    public async Task GetAccountByUsernameAsync_CacheMiss_ThenSecondCallIsServedFromCache()
    {
        var provider = CreateProvider(cacheEnabled: true, out _);
        var account = new Account(Guid.NewGuid(), "joe", "joe@hedgehog.batacek.eu");
        _accountService.Setup(s => s.GetAccountByUsernameAsync("joe")).ReturnsAsync(account);

        var first = await provider.GetAccountByUsernameAsync("joe");
        var second = await provider.GetAccountByUsernameAsync("joe");

        Assert.Same(account, first);
        Assert.Same(account, second);
        _accountService.Verify(s => s.GetAccountByUsernameAsync("joe"), Times.Once);
    }

    [Fact]
    public async Task GetAccountByUsernameAsync_WhenServiceReturnsNull_DoesNotCache()
    {
        var provider = CreateProvider(cacheEnabled: true, out _);
        _accountService.Setup(s => s.GetAccountByUsernameAsync("ghost")).ReturnsAsync((Account?)null);

        var first = await provider.GetAccountByUsernameAsync("ghost");
        var second = await provider.GetAccountByUsernameAsync("ghost");

        Assert.Null(first);
        Assert.Null(second);
        _accountService.Verify(s => s.GetAccountByUsernameAsync("ghost"), Times.Exactly(2));
    }

    [Fact]
    public async Task GetServersByUserIdAsync_WhenCacheDisabled_CallsService()
    {
        var provider = CreateProvider(cacheEnabled: false, out _);
        var servers = new List<Server> { new(Guid.NewGuid(), "srv", "host.hedgehog.batacek.eu") };
        _serverService.Setup(s => s.ListServersAsync(100, 0)).ReturnsAsync(servers);

        var result = await provider.GetServersByUserIdAsync(Guid.NewGuid());

        Assert.Single(result);
        _serverService.Verify(s => s.ListServersAsync(100, 0), Times.Once);
    }

    [Fact]
    public async Task GetServersByUserIdAsync_CacheMiss_ThenSecondCallIsServedFromCache()
    {
        var provider = CreateProvider(cacheEnabled: true, out _);
        var userId = Guid.NewGuid();
        var servers = new List<Server> { new(Guid.NewGuid(), "srv", "host.hedgehog.batacek.eu") };
        _serverService.Setup(s => s.ListServersAsync(100, 0)).ReturnsAsync(servers);

        await provider.GetServersByUserIdAsync(userId);
        await provider.GetServersByUserIdAsync(userId);

        _serverService.Verify(s => s.ListServersAsync(100, 0), Times.Once);
    }

    [Fact]
    public async Task GetServersByUserIdAsync_WhenCachedServerEvicted_ReloadsFromService()
    {
        var provider = CreateProvider(cacheEnabled: true, out var store);
        var userId = Guid.NewGuid();
        var server = new Server(Guid.NewGuid(), "srv", "host.hedgehog.batacek.eu");
        _serverService.Setup(s => s.ListServersAsync(100, 0))
            .ReturnsAsync(new List<Server> { server });

        await provider.GetServersByUserIdAsync(userId); // caches the relation + server entity
        store.Remove<Server>(server.Guid);              // evict the entity, keep the relation

        await provider.GetServersByUserIdAsync(userId); // relation present but entity gone -> reload

        _serverService.Verify(s => s.ListServersAsync(100, 0), Times.Exactly(2));
    }

    [Fact]
    public void InvalidateAccount_RemovesAccountFromStore()
    {
        var provider = CreateProvider(cacheEnabled: true, out var store);
        var account = new Account(Guid.NewGuid(), "joe", "joe@hedgehog.batacek.eu");
        store.Set(account.Guid, account);

        provider.InvalidateAccount(account.Guid);

        Assert.Null(store.Get<Account>(account.Guid));
    }

    [Fact]
    public void InvalidateServer_RemovesServerFromStore()
    {
        var provider = CreateProvider(cacheEnabled: true, out var store);
        var server = new Server(Guid.NewGuid(), "srv", "host.hedgehog.batacek.eu");
        store.Set(server.Guid, server);

        provider.InvalidateServer(server.Guid);

        Assert.Null(store.Get<Server>(server.Guid));
    }

    [Fact]
    public void InvalidateNode_RemovesNodeFromStore()
    {
        var provider = CreateProvider(cacheEnabled: true, out var store);
        var node = new Node(Guid.NewGuid(), "node", "10.0.0.1", 50051);
        store.Set(node.Guid, node);

        provider.InvalidateNode(node.Guid);

        Assert.Null(store.Get<Node>(node.Guid));
    }
}

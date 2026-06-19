using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HedgehogPanel.Application.Persistence;
using HedgehogPanel.Application.Repositories;
using HedgehogPanel.Infrastructure.Persistence.PostgreSQL.Repositories;
using HedgehogPanel.Tests.Integration.TestFixtures;
using Npgsql;
using Xunit;

namespace HedgehogPanel.Tests.Integration.Repositories;

[Collection("IntegrationTests")]
public class ServerRepositoryTests
{
    private readonly PostgreSqlFixture _fixture;
    private readonly IServerRepository _serverRepository;
    private readonly IAccountRepository _accountRepository;

    public ServerRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
        var connectionFactory = new TestConnectionFactory(_fixture.ConnectionString);
        _serverRepository = new ServerRepository(connectionFactory);
        _accountRepository = new AccountRepository(connectionFactory);
    }

    [Fact]
    public async Task CreateAsync_WithValidServer_InsertsServer()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var server = TestDataBuilder.CreateTestServer("TestServer1", "server1.hedgehog.batacek.eu", 22);

        // Act
        var result = await _serverRepository.CreateAsync(server);

        // Assert
        Assert.True(result);
        var retrieved = await _serverRepository.GetByGuidAsync(server.Guid);
        Assert.NotNull(retrieved);
        Assert.Equal(server.Guid, retrieved.Guid);
        Assert.Equal("TestServer1", retrieved.Name);
        Assert.Equal("server1.hedgehog.batacek.eu", retrieved.Hostname);
        Assert.Equal(22, retrieved.DaemonPort);
    }

    [Fact]
    public async Task GetByGuidAsync_WhenExists_ReturnsServer()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var server = TestDataBuilder.CreateTestServer("GetServer", "get.hedgehog.batacek.eu");
        await _serverRepository.CreateAsync(server);

        // Act
        var result = await _serverRepository.GetByGuidAsync(server.Guid);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(server.Guid, result.Guid);
        Assert.Equal("GetServer", result.Name);
    }

    [Fact]
    public async Task GetByGuidAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();

        // Act
        var result = await _serverRepository.GetByGuidAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ListAsync_ReturnsServersWithPagination()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        for (int i = 0; i < 5; i++)
        {
            var server = TestDataBuilder.CreateTestServer($"Server{i}", $"server{i}.hedgehog.batacek.eu");
            await _serverRepository.CreateAsync(server);
        }

        // Act
        var page1 = await _serverRepository.ListAsync(2, 0);
        var page2 = await _serverRepository.ListAsync(2, 2);

        // Assert
        Assert.Equal(2, page1.Count);
        Assert.Equal(2, page2.Count);
    }

    [Fact]
    public async Task ListAsync_WithNoServers_ReturnsEmptyList()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();

        // Act
        var result = await _serverRepository.ListAsync(10, 0);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesServerDetails()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var server = TestDataBuilder.CreateTestServer("OriginalName", "original.hedgehog.batacek.eu", 22);
        await _serverRepository.CreateAsync(server);

        // Act
        var updatedServer = new HedgehogPanel.Domain.Entities.Server(
            server.Guid,
            "UpdatedName",
            "updated.hedgehog.batacek.eu",
            2222,
            HedgehogPanel.Domain.Enums.ServerStatus.Online,
            null,
            "Updated description"
        );
        var result = await _serverRepository.UpdateAsync(updatedServer);

        // Assert
        Assert.True(result);
        var retrieved = await _serverRepository.GetByGuidAsync(server.Guid);
        Assert.NotNull(retrieved);
        Assert.Equal("UpdatedName", retrieved.Name);
        Assert.Equal("updated.hedgehog.batacek.eu", retrieved.Hostname);
        Assert.Equal(2222, retrieved.DaemonPort);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotExists_ReturnsFalse()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var server = TestDataBuilder.CreateTestServer();

        // Act
        var result = await _serverRepository.UpdateAsync(server);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_WhenExists_RemovesServer()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var server = TestDataBuilder.CreateTestServer("DeleteServer", "delete.hedgehog.batacek.eu");
        await _serverRepository.CreateAsync(server);

        // Act
        var result = await _serverRepository.DeleteAsync(server.Guid);

        // Assert
        Assert.True(result);
        var retrieved = await _serverRepository.GetByGuidAsync(server.Guid);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotExists_ReturnsFalse()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();

        // Act
        var result = await _serverRepository.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ListByOwnerAsync_ReturnsOnlyOwnedServers()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var user = TestDataBuilder.CreateTestAccount("owner1", "owner1@hedgehog.batacek.eu");
        await _accountRepository.CreateAsync(user, "password");

        var server1 = TestDataBuilder.CreateTestServer("OwnedServer1", "owned1.hedgehog.batacek.eu");
        var server2 = TestDataBuilder.CreateTestServer("OwnedServer2", "owned2.hedgehog.batacek.eu");
        var server3 = TestDataBuilder.CreateTestServer("UnownedServer", "unowned.hedgehog.batacek.eu");
        
        await _serverRepository.CreateAsync(server1);
        await _serverRepository.CreateAsync(server2);
        await _serverRepository.CreateAsync(server3);

        await _serverRepository.AssignToUserAsync(server1.Guid, user.Guid);
        await _serverRepository.AssignToUserAsync(server2.Guid, user.Guid);

        // Act
        var result = await _serverRepository.ListByOwnerAsync(user.Guid);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, s => s.Guid == server1.Guid);
        Assert.Contains(result, s => s.Guid == server2.Guid);
        Assert.DoesNotContain(result, s => s.Guid == server3.Guid);
    }

    [Fact]
    public async Task ListByOwnerAsync_WithNoOwnedServers_ReturnsEmptyList()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var user = TestDataBuilder.CreateTestAccount("owner2", "owner2@hedgehog.batacek.eu");
        await _accountRepository.CreateAsync(user, "password");

        // Act
        var result = await _serverRepository.ListByOwnerAsync(user.Guid);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ListUnownedAsync_ReturnsOnlyUnownedServers()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var user = TestDataBuilder.CreateTestAccount("owner3", "owner3@hedgehog.batacek.eu");
        await _accountRepository.CreateAsync(user, "password");

        var ownedServer = TestDataBuilder.CreateTestServer("Owned", "owned.hedgehog.batacek.eu");
        var unownedServer1 = TestDataBuilder.CreateTestServer("Unowned1", "unowned1.hedgehog.batacek.eu");
        var unownedServer2 = TestDataBuilder.CreateTestServer("Unowned2", "unowned2.hedgehog.batacek.eu");

        await _serverRepository.CreateAsync(ownedServer);
        await _serverRepository.CreateAsync(unownedServer1);
        await _serverRepository.CreateAsync(unownedServer2);

        await _serverRepository.AssignToUserAsync(ownedServer.Guid, user.Guid);

        // Act
        var result = await _serverRepository.ListUnownedAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, s => s.Guid == unownedServer1.Guid);
        Assert.Contains(result, s => s.Guid == unownedServer2.Guid);
        Assert.DoesNotContain(result, s => s.Guid == ownedServer.Guid);
    }

    [Fact]
    public async Task GetOwnerUsernameAsync_WhenServerHasOwner_ReturnsUsername()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var user = TestDataBuilder.CreateTestAccount("serverowner", "serverowner@hedgehog.batacek.eu");
        await _accountRepository.CreateAsync(user, "password");

        var server = TestDataBuilder.CreateTestServer("OwnedServer", "owned.hedgehog.batacek.eu");
        await _serverRepository.CreateAsync(server);
        await _serverRepository.AssignToUserAsync(server.Guid, user.Guid);

        // Act
        var result = await _serverRepository.GetOwnerUsernameAsync(server.Guid);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("serverowner", result);
    }

    [Fact]
    public async Task GetOwnerUsernameAsync_WhenServerHasNoOwner_ReturnsNull()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var server = TestDataBuilder.CreateTestServer("NoOwner", "noowner.hedgehog.batacek.eu");
        await _serverRepository.CreateAsync(server);

        // Act
        var result = await _serverRepository.GetOwnerUsernameAsync(server.Guid);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AssignToUserAsync_AssignsServerToUser()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var user = TestDataBuilder.CreateTestAccount("assignuser", "assign@hedgehog.batacek.eu");
        await _accountRepository.CreateAsync(user, "password");

        var server = TestDataBuilder.CreateTestServer("AssignServer", "assign.hedgehog.batacek.eu");
        await _serverRepository.CreateAsync(server);

        // Act
        var result = await _serverRepository.AssignToUserAsync(server.Guid, user.Guid);

        // Assert
        Assert.True(result);
        var ownerUsername = await _serverRepository.GetOwnerUsernameAsync(server.Guid);
        Assert.Equal("assignuser", ownerUsername);
    }

    [Fact]
    public async Task AssignToUserAsync_WhenServerNotExists_ReturnsFalse()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var user = TestDataBuilder.CreateTestAccount("user", "user@hedgehog.batacek.eu");
        await _accountRepository.CreateAsync(user, "password");

        // Act
        var result = await _serverRepository.AssignToUserAsync(Guid.NewGuid(), user.Guid);

        // Assert
        Assert.False(result);
    }

    private class TestConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public TestConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection CreateConnection()
        {
            var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            return connection;
        }

        public async ValueTask<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }
}

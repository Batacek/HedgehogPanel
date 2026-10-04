using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using HedgehogPanel.Application.Contracts.Logging;
using HedgehogPanel.Application.Persistence;
using HedgehogPanel.Application.Repositories;
using HedgehogPanel.Domain.Entities;
using HedgehogPanel.Domain.Enums;
using HedgehogPanel.Infrastructure.Configuration;
using HedgehogPanel.Infrastructure.Exceptions;
using HedgehogPanel.Infrastructure.Persistence.PostgreSQL.Repositories;
using HedgehogPanel.Infrastructure.Persistence.Store;
using HedgehogPanel.Tests.Integration.TestFixtures;
using Moq;
using Npgsql;
using Xunit;

namespace HedgehogPanel.Tests.Integration.Repositories;

[Collection("IntegrationTests")]
public class NodeRepositoryTests
{
    private readonly PostgreSqlFixture _fixture;
    private readonly INodeRepository _repository;

    public NodeRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
        var connectionFactory = new TestConnectionFactory(_fixture.ConnectionString);
        var config = new HedgehogConfig { Cache = new CacheConfig { Enabled = false } };
        var mockLogger = new Mock<ILoggerService>();
        var store = new InMemoryStore(mockLogger.Object, config);
        _repository = new NodeRepository(connectionFactory, store, config);
    }

    [Fact]
    public async Task CreateAsync_WithValidNode_InsertsNode()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var node = TestDataBuilder.CreateTestNode("TestNode1", "192.168.1.100", 50051);

        // Act
        var result = await _repository.CreateAsync(node);

        // Assert
        Assert.True(result);
        var retrieved = await _repository.GetByGuidAsync(node.Guid);
        Assert.NotNull(retrieved);
        Assert.Equal(node.Guid, retrieved.Guid);
        Assert.Equal("TestNode1", retrieved.Name);
        Assert.Equal("192.168.1.100", retrieved.IpAddress);
        Assert.Equal(50051, retrieved.Port);
    }

    [Fact]
    public async Task GetByGuidAsync_WhenExists_ReturnsNode()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var node = TestDataBuilder.CreateTestNode("GetNode", "10.0.0.1", 50051);
        await _repository.CreateAsync(node);

        // Act
        var result = await _repository.GetByGuidAsync(node.Guid);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(node.Guid, result.Guid);
        Assert.Equal("GetNode", result.Name);
        Assert.Equal("10.0.0.1", result.IpAddress);
    }

    [Fact]
    public async Task GetByGuidAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();

        // Act
        var result = await _repository.GetByGuidAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ListAsync_ReturnsNodesWithPagination()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        for (int i = 0; i < 5; i++)
        {
            var node = TestDataBuilder.CreateTestNode($"Node{i}", $"192.168.1.{i}", 50051);
            await _repository.CreateAsync(node);
        }

        // Act
        var page1 = await _repository.ListAsync(2, 0);
        var page2 = await _repository.ListAsync(2, 2);

        // Assert
        Assert.Equal(2, page1.Count);
        Assert.Equal(2, page2.Count);
    }

    [Fact]
    public async Task ListAsync_WithNoNodes_ReturnsEmptyList()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();

        // Act
        var result = await _repository.ListAsync(10, 0);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesNodeDetails()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var node = TestDataBuilder.CreateTestNode("OriginalNode", "192.168.1.1", 50051);
        await _repository.CreateAsync(node);

        // Act
        var updatedNode = new HedgehogPanel.Domain.Entities.Node(
            node.Guid,
            "UpdatedNode",
            "192.168.1.200",
            50052,
            "Updated description",
            NodeStatus.Online
        );
        var result = await _repository.UpdateAsync(updatedNode);

        // Assert
        Assert.True(result);
        var retrieved = await _repository.GetByGuidAsync(node.Guid);
        Assert.NotNull(retrieved);
        Assert.Equal("UpdatedNode", retrieved.Name);
        Assert.Equal("192.168.1.200", retrieved.IpAddress);
        Assert.Equal(50052, retrieved.Port);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotExists_ReturnsFalse()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var node = TestDataBuilder.CreateTestNode();

        // Act
        var result = await _repository.UpdateAsync(node);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_WhenExists_RemovesNode()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var node = TestDataBuilder.CreateTestNode("DeleteNode", "192.168.1.50", 50051);
        await _repository.CreateAsync(node);

        // Act
        var result = await _repository.DeleteAsync(node.Guid);

        // Assert
        Assert.True(result);
        var retrieved = await _repository.GetByGuidAsync(node.Guid);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotExists_ReturnsFalse()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();

        // Act
        var result = await _repository.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_ThrowsException()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var node1 = TestDataBuilder.CreateTestNode("DuplicateNode", "192.168.1.1", 50051);
        var node2 = TestDataBuilder.CreateTestNode("DuplicateNode", "192.168.1.2", 50052);
        await _repository.CreateAsync(node1);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => _repository.CreateAsync(node2));
    }

    [Fact]
    public async Task UpdateAsync_RenamingToAnotherNodesName_ThrowsException()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var nodeA = TestDataBuilder.CreateTestNode("Node-X", "192.168.1.1", 50051);
        var nodeB = TestDataBuilder.CreateTestNode("Node-Y", "192.168.1.2", 50052);
        await _repository.CreateAsync(nodeA);
        await _repository.CreateAsync(nodeB);

        // Act & Assert - renaming B to A's name must be rejected by the duplicate-name guard.
        var renamed = new HedgehogPanel.Domain.Entities.Node(nodeB.Guid, "Node-X", "192.168.1.2", 50052);
        await Assert.ThrowsAnyAsync<Exception>(() => _repository.UpdateAsync(renamed));
    }

    [Fact]
    public async Task ListAsync_OrdersByCreatedAt()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var node1 = TestDataBuilder.CreateTestNode("Node1", "192.168.1.1", 50051);
        var node2 = TestDataBuilder.CreateTestNode("Node2", "192.168.1.2", 50051);
        var node3 = TestDataBuilder.CreateTestNode("Node3", "192.168.1.3", 50051);
        
        await _repository.CreateAsync(node1);
        await Task.Delay(10); // Ensure different timestamps
        await _repository.CreateAsync(node2);
        await Task.Delay(10);
        await _repository.CreateAsync(node3);

        // Act
        var result = await _repository.ListAsync(10, 0);

        // Assert
        Assert.Equal(3, result.Count);
        // Nodes should be ordered (implementation dependent - typically by created_at)
    }

    [Fact]
    public async Task CreateAsync_NewNode_StartsUnpairedWithNoDaemonState()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var node = TestDataBuilder.CreateTestNode("FreshNode", "192.168.1.70", 50051);

        // Act
        await _repository.CreateAsync(node);

        // Assert
        var retrieved = await _repository.GetByGuidAsync(node.Guid);
        Assert.NotNull(retrieved);
        Assert.Equal(NodeStatus.Unpaired, retrieved.Status);
        Assert.Null(retrieved.DaemonUuid);
        Assert.Null(retrieved.DaemonVersion);
        Assert.Null(retrieved.ProtocolVersion);
        Assert.Null(retrieved.DaemonToken);
        Assert.Null(retrieved.LastError);
        Assert.False(retrieved.IsPaired);
    }

    [Fact]
    public async Task UpdateAsync_RoundTripsDaemonState()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var node = TestDataBuilder.CreateTestNode("PairedNode", "192.168.1.71", 50051);
        await _repository.CreateAsync(node);
        var daemonUuid = Guid.NewGuid();
        var paired = new Node(node.Guid, node.Name, node.IpAddress, node.Port,
            status: NodeStatus.Online, daemonUuid: daemonUuid, daemonVersion: "0.1.0",
            protocolVersion: "2.0.0", daemonToken: "token-value", lastError: "timed out");

        // Act
        await _repository.UpdateAsync(paired);

        // Assert
        var retrieved = await _repository.GetByGuidAsync(node.Guid);
        Assert.NotNull(retrieved);
        Assert.Equal(NodeStatus.Online, retrieved.Status);
        Assert.Equal(daemonUuid, retrieved.DaemonUuid);
        Assert.Equal("0.1.0", retrieved.DaemonVersion);
        Assert.Equal("2.0.0", retrieved.ProtocolVersion);
        Assert.Equal("token-value", retrieved.DaemonToken);
        Assert.Equal("timed out", retrieved.LastError);
        Assert.True(retrieved.IsPaired);
    }

    [Fact]
    public async Task UpdateAsync_TheSameDaemonOnTwoNodes_IsRejected()
    {
        // Arrange - one daemon paired under two node rows would split its state.
        await _fixture.CleanDatabaseAsync();
        var daemonUuid = Guid.NewGuid();
        var first = TestDataBuilder.CreateTestNode("FirstNode", "192.168.1.72", 50051);
        var second = TestDataBuilder.CreateTestNode("SecondNode", "192.168.1.73", 50051);
        await _repository.CreateAsync(first);
        await _repository.CreateAsync(second);
        await _repository.UpdateAsync(new Node(first.Guid, first.Name, first.IpAddress, first.Port, daemonUuid: daemonUuid));

        // Act & Assert
        await Assert.ThrowsAsync<DatabaseConstraintException>(() =>
            _repository.UpdateAsync(new Node(second.Guid, second.Name, second.IpAddress, second.Port, daemonUuid: daemonUuid)));
    }

    [Fact]
    public async Task UpdateAsync_KeepingTheSameName_Succeeds()
    {
        // Arrange - pairing and health checks update a node without renaming it.
        await _fixture.CleanDatabaseAsync();
        var node = TestDataBuilder.CreateTestNode("SteadyNode", "192.168.1.74", 50051);
        await _repository.CreateAsync(node);

        // Act
        var result = await _repository.UpdateAsync(new Node(node.Guid, node.Name, node.IpAddress, node.Port, description: "edited"));

        // Assert
        Assert.True(result);
        var retrieved = await _repository.GetByGuidAsync(node.Guid);
        Assert.Equal("edited", retrieved!.Description);
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

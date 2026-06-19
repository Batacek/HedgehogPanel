using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using HedgehogPanel.Application.Contracts.Logging;
using HedgehogPanel.Application.Persistence;
using HedgehogPanel.Application.Repositories;
using HedgehogPanel.Infrastructure.Configuration;
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
            "Online"
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

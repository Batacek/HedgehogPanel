using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HedgehogPanel.Application.Services;
using HedgehogPanel.Application.Repositories;
using HedgehogPanel.Domain.Entities;
using HedgehogPanel.Domain.Enums;

namespace HedgehogPanel.Tests.Unit.Services;

public class ServerServiceTests
{
    private readonly Mock<IServerRepository> _mockRepo;
    private readonly ServerService _service;

    public ServerServiceTests()
    {
        _mockRepo = new Mock<IServerRepository>();
        _service = new ServerService(_mockRepo.Object);
    }

    [Fact]
    public async Task CreateServerAsync_WithValidInputs_ReturnsCreatedServer()
    {
        // Arrange
        var name = "TestServer";
        var hostname = "test.hedgehog.batacek.eu";
        var port = 22;
        _mockRepo.Setup(r => r.CreateAsync(It.IsAny<Server>())).ReturnsAsync(true);

        // Act
        var result = await _service.CreateServerAsync(name, hostname, port);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(name, result.Name);
        Assert.Equal(hostname, result.Hostname);
        Assert.Equal(port, result.DaemonPort);
        _mockRepo.Verify(r => r.CreateAsync(It.Is<Server>(s => 
            s.Name == name && s.Hostname == hostname && s.DaemonPort == port)), Times.Once);
    }

    [Fact]
    public async Task CreateServerAsync_WhenRepoFails_ThrowsException()
    {
        // Arrange
        _mockRepo.Setup(r => r.CreateAsync(It.IsAny<Server>())).ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _service.CreateServerAsync("server", "host.hedgehog.batacek.eu", 22));
        Assert.Equal("Failed to create server", exception.Message);
    }

    [Fact]
    public async Task GetServerByIdAsync_CallsRepository()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var expectedServer = new Server(guid, "TestServer", "test.hedgehog.batacek.eu", 22);
        _mockRepo.Setup(r => r.GetByGuidAsync(guid)).ReturnsAsync(expectedServer);

        // Act
        var result = await _service.GetServerByIdAsync(guid);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(guid, result.Guid);
        Assert.Equal("TestServer", result.Name);
        _mockRepo.Verify(r => r.GetByGuidAsync(guid), Times.Once);
    }

    [Fact]
    public async Task GetServerByIdAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        var guid = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByGuidAsync(guid)).ReturnsAsync((Server?)null);

        // Act
        var result = await _service.GetServerByIdAsync(guid);

        // Assert
        Assert.Null(result);
        _mockRepo.Verify(r => r.GetByGuidAsync(guid), Times.Once);
    }

    [Fact]
    public async Task ListServersAsync_CallsRepository()
    {
        // Arrange
        var servers = new List<Server>
        {
            new Server(Guid.NewGuid(), "Server1", "host1.hedgehog.batacek.eu", 22),
            new Server(Guid.NewGuid(), "Server2", "host2.hedgehog.batacek.eu", 22)
        };
        _mockRepo.Setup(r => r.ListAsync(100, 0)).ReturnsAsync(servers);

        // Act
        var result = await _service.ListServersAsync(100, 0);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Server1", result[0].Name);
        Assert.Equal("Server2", result[1].Name);
        _mockRepo.Verify(r => r.ListAsync(100, 0), Times.Once);
    }

    [Fact]
    public async Task UpdateServerAsync_CallsRepositoryReturnsTrue()
    {
        // Arrange
        var server = new Server(Guid.NewGuid(), "UpdatedServer", "updated.hedgehog.batacek.eu", 2222);
        _mockRepo.Setup(r => r.UpdateAsync(server)).ReturnsAsync(true);

        // Act
        var result = await _service.UpdateServerAsync(server);

        // Assert
        Assert.True(result);
        _mockRepo.Verify(r => r.UpdateAsync(server), Times.Once);
    }

    [Fact]
    public async Task UpdateServerAsync_WhenRepoFails_ReturnsFalse()
    {
        // Arrange
        var server = new Server(Guid.NewGuid(), "Server", "host.hedgehog.batacek.eu", 22);
        _mockRepo.Setup(r => r.UpdateAsync(server)).ReturnsAsync(false);

        // Act
        var result = await _service.UpdateServerAsync(server);

        // Assert
        Assert.False(result);
        _mockRepo.Verify(r => r.UpdateAsync(server), Times.Once);
    }

    [Fact]
    public async Task DeleteServerAsync_CallsRepositoryReturnsTrue()
    {
        // Arrange
        var guid = Guid.NewGuid();
        _mockRepo.Setup(r => r.DeleteAsync(guid)).ReturnsAsync(true);

        // Act
        var result = await _service.DeleteServerAsync(guid);

        // Assert
        Assert.True(result);
        _mockRepo.Verify(r => r.DeleteAsync(guid), Times.Once);
    }

    [Fact]
    public async Task DeleteServerAsync_WhenNotFound_ReturnsFalse()
    {
        // Arrange
        var guid = Guid.NewGuid();
        _mockRepo.Setup(r => r.DeleteAsync(guid)).ReturnsAsync(false);

        // Act
        var result = await _service.DeleteServerAsync(guid);

        // Assert
        Assert.False(result);
        _mockRepo.Verify(r => r.DeleteAsync(guid), Times.Once);
    }

    [Fact]
    public async Task ListServersByOwnerAsync_CallsRepository()
    {
        // Arrange
        var ownerGuid = Guid.NewGuid();
        var servers = new List<Server>
        {
            new Server(Guid.NewGuid(), "OwnedServer1", "host1.hedgehog.batacek.eu", 22),
            new Server(Guid.NewGuid(), "OwnedServer2", "host2.hedgehog.batacek.eu", 22)
        };
        _mockRepo.Setup(r => r.ListByOwnerAsync(ownerGuid, 100, 0)).ReturnsAsync(servers);

        // Act
        var result = await _service.ListServersByOwnerAsync(ownerGuid, 100, 0);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("OwnedServer1", result[0].Name);
        _mockRepo.Verify(r => r.ListByOwnerAsync(ownerGuid, 100, 0), Times.Once);
    }

    [Fact]
    public async Task ListUnownedServersAsync_CallsRepository()
    {
        // Arrange
        var servers = new List<Server>
        {
            new Server(Guid.NewGuid(), "UnownedServer", "host.hedgehog.batacek.eu", 22)
        };
        _mockRepo.Setup(r => r.ListUnownedAsync(100, 0)).ReturnsAsync(servers);

        // Act
        var result = await _service.ListUnownedServersAsync(100, 0);

        // Assert
        Assert.Single(result);
        Assert.Equal("UnownedServer", result[0].Name);
        _mockRepo.Verify(r => r.ListUnownedAsync(100, 0), Times.Once);
    }

    [Fact]
    public async Task GetServerOwnerUsernameAsync_CallsRepository()
    {
        // Arrange
        var serverGuid = Guid.NewGuid();
        var ownerUsername = "testowner";
        _mockRepo.Setup(r => r.GetOwnerUsernameAsync(serverGuid)).ReturnsAsync(ownerUsername);

        // Act
        var result = await _service.GetServerOwnerUsernameAsync(serverGuid);

        // Assert
        Assert.Equal(ownerUsername, result);
        _mockRepo.Verify(r => r.GetOwnerUsernameAsync(serverGuid), Times.Once);
    }

    [Fact]
    public async Task GetServerOwnerUsernameAsync_WhenNoOwner_ReturnsNull()
    {
        // Arrange
        var serverGuid = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetOwnerUsernameAsync(serverGuid)).ReturnsAsync((string?)null);

        // Act
        var result = await _service.GetServerOwnerUsernameAsync(serverGuid);

        // Assert
        Assert.Null(result);
        _mockRepo.Verify(r => r.GetOwnerUsernameAsync(serverGuid), Times.Once);
    }

    [Fact]
    public async Task AssignServerToUserAsync_CallsRepositoryReturnsTrue()
    {
        // Arrange
        var serverGuid = Guid.NewGuid();
        var userGuid = Guid.NewGuid();
        _mockRepo.Setup(r => r.AssignToUserAsync(serverGuid, userGuid)).ReturnsAsync(true);

        // Act
        var result = await _service.AssignServerToUserAsync(serverGuid, userGuid);

        // Assert
        Assert.True(result);
        _mockRepo.Verify(r => r.AssignToUserAsync(serverGuid, userGuid), Times.Once);
    }

    [Fact]
    public async Task AssignServerToUserAsync_WhenFails_ReturnsFalse()
    {
        // Arrange
        var serverGuid = Guid.NewGuid();
        var userGuid = Guid.NewGuid();
        _mockRepo.Setup(r => r.AssignToUserAsync(serverGuid, userGuid)).ReturnsAsync(false);

        // Act
        var result = await _service.AssignServerToUserAsync(serverGuid, userGuid);

        // Assert
        Assert.False(result);
        _mockRepo.Verify(r => r.AssignToUserAsync(serverGuid, userGuid), Times.Once);
    }
}

using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HedgehogPanel.Application.Services;
using HedgehogPanel.Application.Repositories;
using HedgehogPanel.Domain.Entities;

namespace HedgehogPanel.Tests.Unit.Services;

public class AccountServiceTests
{
    private readonly Mock<IAccountRepository> _mockRepo;
    private readonly AccountService _service;

    public AccountServiceTests()
    {
        _mockRepo = new Mock<IAccountRepository>();
        _service = new AccountService(_mockRepo.Object);
    }

    [Fact]
    public async Task CreateAccountAsync_WithValidInputs_ReturnsCreatedAccount()
    {
        // Arrange
        var username = "testuser";
        var email = "test@example.com";
        var password = "securepass123";
        _mockRepo.Setup(r => r.CreateAsync(It.IsAny<Account>(), It.IsAny<string>())).ReturnsAsync(true);

        // Act
        var result = await _service.CreateAccountAsync(username, email, password);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(username, result.Username);
        Assert.Equal(email, result.Email);
        _mockRepo.Verify(r => r.CreateAsync(It.Is<Account>(a => 
            a.Username == username && a.Email == email), password), Times.Once);
    }

    [Fact]
    public async Task CreateAccountAsync_WhenRepoFails_ThrowsException()
    {
        // Arrange
        _mockRepo.Setup(r => r.CreateAsync(It.IsAny<Account>(), It.IsAny<string>())).ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => _service.CreateAccountAsync("user", "mail@example.com", "pass"));
        Assert.Equal("Failed to create account", exception.Message);
    }

    [Fact]
    public async Task AuthenticateAsync_WithEmptyInputs_ReturnsNull()
    {
        // Act & Assert
        Assert.Null(await _service.AuthenticateAsync("", "pass"));
        Assert.Null(await _service.AuthenticateAsync("user", ""));
        Assert.Null(await _service.AuthenticateAsync("", ""));
        _mockRepo.Verify(r => r.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AuthenticateAsync_WithValidInputs_CallsRepository()
    {
        // Arrange
        var expectedAccount = new Account(Guid.NewGuid(), "validuser", "valid@example.com", true, null);
        _mockRepo.Setup(r => r.AuthenticateAsync("validuser", "validpass")).ReturnsAsync(expectedAccount);

        // Act
        var result = await _service.AuthenticateAsync("validuser", "validpass");

        // Assert
        Assert.Equal(expectedAccount.Guid, result.Guid);
        Assert.Equal("validuser", result.Username);
        _mockRepo.Verify(r => r.AuthenticateAsync("validuser", "validpass"), Times.Once);
    }

    [Fact]
    public async Task GetAccountByUsernameAsync_CallsRepository()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var expectedAccount = new Account(guid, "user", "mail@example.com", true, null);
        _mockRepo.Setup(r => r.GetByUsernameAsync("user")).ReturnsAsync(expectedAccount);

        // Act
        var result = await _service.GetAccountByUsernameAsync("user");

        // Assert
        Assert.Equal(guid, result.Guid);
        _mockRepo.Verify(r => r.GetByUsernameAsync("user"), Times.Once);
    }

    [Fact]
    public async Task GetAccountByIdAsync_CallsRepository()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var expectedAccount = new Account(guid, "user", "mail", true, null);
        _mockRepo.Setup(r => r.GetByGuidAsync(guid)).ReturnsAsync(expectedAccount);

        // Act
        var result = await _service.GetAccountByIdAsync(guid);

        // Assert
        Assert.Equal(guid, result.Guid);
        _mockRepo.Verify(r => r.GetByGuidAsync(guid), Times.Once);
    }

    [Fact]
    public async Task UpdateAccountAsync_CallsRepositoryReturnsTrue()
    {
        // Arrange
        var account = new Account(Guid.NewGuid(), "user", "mail", true, null);
        _mockRepo.Setup(r => r.UpdateAsync(account, null)).ReturnsAsync(true);

        // Act
        var result = await _service.UpdateAccountAsync(account);

        // Assert
        Assert.True(result);
        _mockRepo.Verify(r => r.UpdateAsync(account, null), Times.Once);
    }

    [Fact]
    public async Task DeleteAccountAsync_AccountNotFound_ReturnsFalse()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByUsernameAsync("nonexistent")).ReturnsAsync((Account?)null);

        // Act
        var result = await _service.DeleteAccountAsync("nonexistent");

        // Assert
        Assert.False(result);
        _mockRepo.Verify(r => r.GetByUsernameAsync("nonexistent"), Times.Once);
        _mockRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAccountAsync_ValidAccount_CallsDelete()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var account = new Account(guid, "user", "mail", true, null);
        _mockRepo.Setup(r => r.GetByUsernameAsync("user")).ReturnsAsync(account);
        _mockRepo.Setup(r => r.DeleteAsync(guid)).ReturnsAsync(true);

        // Act
        var result = await _service.DeleteAccountAsync("user");

        // Assert
        Assert.True(result);
        _mockRepo.Verify(r => r.DeleteAsync(guid), Times.Once);
    }

    [Fact]
    public async Task ListAccountsAsync_CallsRepository()
    {
        // Arrange
        var accounts = new List<Account>
        {
            new Account(Guid.NewGuid(), "user1", "mail1", true, null),
            new Account(Guid.NewGuid(), "user2", "mail2", true, null)
        };
        _mockRepo.Setup(r => r.ListAsync(5, 0)).ReturnsAsync(accounts);

        // Act
        var result = await _service.ListAccountsAsync(5, 0);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("user1", result.First().Username);
        _mockRepo.Verify(r => r.ListAsync(5, 0), Times.Once);
    }
}

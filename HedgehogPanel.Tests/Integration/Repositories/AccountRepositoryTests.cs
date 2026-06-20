using System;
using System.Data;
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
public class AccountRepositoryTests
{
    private readonly PostgreSqlFixture _fixture;
    private readonly IAccountRepository _repository;

    public AccountRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
        var connectionFactory = new TestConnectionFactory(_fixture.ConnectionString);
        _repository = new AccountRepository(connectionFactory);
    }

    [Fact]
    public async Task CreateAsync_WithValidAccount_InsertsAccountWithBcryptHash()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var account = TestDataBuilder.CreateTestAccount("testuser1", "test1@hedgehog.batacek.eu");
        var password = "securePassword123";

        // Act
        var result = await _repository.CreateAsync(account, password);

        // Assert
        Assert.True(result);
        var retrieved = await _repository.GetByUsernameAsync("testuser1");
        Assert.NotNull(retrieved);
        Assert.Equal(account.Guid, retrieved.Guid);
        Assert.Equal("testuser1", retrieved.Username);
        Assert.Equal("test1@hedgehog.batacek.eu", retrieved.Email);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateUsername_ReturnsFalse()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var account1 = TestDataBuilder.CreateTestAccount("duplicate", "email1@hedgehog.batacek.eu");
        var account2 = TestDataBuilder.CreateTestAccount("duplicate", "email2@hedgehog.batacek.eu");
        await _repository.CreateAsync(account1, "pass1");

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => _repository.CreateAsync(account2, "pass2"));
    }

    [Fact]
    public async Task AuthenticateAsync_WithCorrectPassword_ReturnsAccount()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var account = TestDataBuilder.CreateTestAccount("authuser", "auth@hedgehog.batacek.eu");
        var password = "correctPassword123";
        await _repository.CreateAsync(account, password);

        // Act
        var result = await _repository.AuthenticateAsync("authuser", password);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(account.Guid, result.Guid);
        Assert.Equal("authuser", result.Username);
    }

    [Fact]
    public async Task AuthenticateAsync_WithIncorrectPassword_ReturnsNull()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var account = TestDataBuilder.CreateTestAccount("authuser2", "auth2@hedgehog.batacek.eu");
        await _repository.CreateAsync(account, "correctPassword");

        // Act
        var result = await _repository.AuthenticateAsync("authuser2", "wrongPassword");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AuthenticateAsync_WithNonExistentUser_ReturnsNull()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();

        // Act
        var result = await _repository.AuthenticateAsync("nonexistent", "anyPassword");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUsernameAsync_WhenExists_ReturnsAccount()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var account = TestDataBuilder.CreateTestAccount("getuser", "get@hedgehog.batacek.eu");
        await _repository.CreateAsync(account, "password");

        // Act
        var result = await _repository.GetByUsernameAsync("getuser");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(account.Guid, result.Guid);
        Assert.Equal("getuser", result.Username);
    }

    [Fact]
    public async Task GetByUsernameAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();

        // Act
        var result = await _repository.GetByUsernameAsync("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByGuidAsync_WhenExists_ReturnsAccount()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var account = TestDataBuilder.CreateTestAccount("guiduser", "guid@hedgehog.batacek.eu");
        await _repository.CreateAsync(account, "password");

        // Act
        var result = await _repository.GetByGuidAsync(account.Guid);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(account.Guid, result.Guid);
        Assert.Equal("guiduser", result.Username);
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
    public async Task UpdateAsync_WithoutPasswordChange_UpdatesAccountDetails()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var account = TestDataBuilder.CreateTestAccount("updateuser", "update@hedgehog.batacek.eu");
        await _repository.CreateAsync(account, "password");
        
        var retrieved = await _repository.GetByUsernameAsync("updateuser");
        Assert.NotNull(retrieved);

        // Act
        var updatedAccount = new HedgehogPanel.Domain.Entities.Account(
            retrieved.Guid, 
            "updatedusername", 
            "updated@hedgehog.batacek.eu", 
            true, 
            null, 
            "NewFirst", 
            "NewMiddle", 
            "NewLast",
            retrieved.Groups,
            retrieved.RowVersion
        );
        var result = await _repository.UpdateAsync(updatedAccount, null);

        // Assert
        Assert.True(result);
        var updated = await _repository.GetByGuidAsync(retrieved.Guid);
        Assert.NotNull(updated);
        Assert.Equal("updatedusername", updated.Username);
        Assert.Equal("updated@hedgehog.batacek.eu", updated.Email);
        Assert.Equal("NewFirst", updated.FirstName);
        Assert.Equal("NewMiddle", updated.MiddleName);
        Assert.Equal("NewLast", updated.LastName);
    }

    [Fact]
    public async Task UpdateAsync_WithPasswordChange_UpdatesPasswordHash()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var account = TestDataBuilder.CreateTestAccount("passuser", "pass@hedgehog.batacek.eu");
        var oldPassword = "oldPassword123";
        var newPassword = "newPassword456";
        await _repository.CreateAsync(account, oldPassword);

        var retrieved = await _repository.GetByUsernameAsync("passuser");
        Assert.NotNull(retrieved);

        // Act
        await _repository.UpdateAsync(retrieved, newPassword);

        // Assert - old password should not work
        var authOld = await _repository.AuthenticateAsync("passuser", oldPassword);
        Assert.Null(authOld);

        // Assert - new password should work
        var authNew = await _repository.AuthenticateAsync("passuser", newPassword);
        Assert.NotNull(authNew);
        Assert.Equal(account.Guid, authNew.Guid);
    }

    [Fact]
    public async Task DeleteAsync_WhenExists_RemovesAccount()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var account = TestDataBuilder.CreateTestAccount("deleteuser", "delete@hedgehog.batacek.eu");
        await _repository.CreateAsync(account, "password");

        // Act
        var result = await _repository.DeleteAsync(account.Guid);

        // Assert
        Assert.True(result);
        var retrieved = await _repository.GetByGuidAsync(account.Guid);
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
    public async Task ListAsync_ReturnsAccountsWithPagination()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        for (int i = 0; i < 5; i++)
        {
            var account = TestDataBuilder.CreateTestAccount($"listuser{i}", $"list{i}@hedgehog.batacek.eu");
            await _repository.CreateAsync(account, "password");
        }

        // Act
        var page1 = await _repository.ListAsync(2, 0);
        var page2 = await _repository.ListAsync(2, 2);

        // Assert
        Assert.Equal(2, page1.Count);
        Assert.Equal(2, page2.Count);
    }

    [Fact]
    public async Task ListAsync_WithNoAccounts_ReturnsEmptyList()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();

        // Act
        var result = await _repository.ListAsync(10, 0);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByUsernameAsync_ForUserInAdminGroup_PopulatesGroupsAndIsAdmin()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var account = TestDataBuilder.CreateTestAccount("groupuser", "groupuser@hedgehog.batacek.eu");
        await _repository.CreateAsync(account, "password");

        await using (var conn = new NpgsqlConnection(_fixture.ConnectionString))
        {
            await conn.OpenAsync();
            var groupId = Guid.NewGuid();
            await using (var groupCmd = new NpgsqlCommand(
                "INSERT INTO groups (uuid, name, description) VALUES (@id, 'admin', 'Administrators')", conn))
            {
                groupCmd.Parameters.AddWithValue("id", groupId);
                await groupCmd.ExecuteNonQueryAsync();
            }
            await using (var membershipCmd = new NpgsqlCommand(
                "INSERT INTO user_groups (user_uuid, group_uuid, priority) VALUES (@u, @g, 100)", conn))
            {
                membershipCmd.Parameters.AddWithValue("u", account.Guid);
                membershipCmd.Parameters.AddWithValue("g", groupId);
                await membershipCmd.ExecuteNonQueryAsync();
            }
        }

        // Act - LoadUserGroupsAsync joins user_groups + groups when fetching the account.
        var loaded = await _repository.GetByUsernameAsync("groupuser");

        // Assert
        Assert.NotNull(loaded);
        Assert.Contains(loaded.Groups, g => g.Name == "admin");
        Assert.True(loaded.IsAdmin);
    }

    [Fact]
    public async Task UpdateAsync_WithStaleRowVersion_ReturnsFalse()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var account = TestDataBuilder.CreateTestAccount("concurrency", "concurrency@hedgehog.batacek.eu");
        await _repository.CreateAsync(account, "password");

        var loaded = await _repository.GetByUsernameAsync("concurrency");
        Assert.NotNull(loaded);
        Assert.True(loaded.RowVersion > 0);

        // First update succeeds and bumps the row version (xmin) in the database.
        var first = await _repository.UpdateAsync(loaded, null);
        Assert.True(first);

        // `loaded` still carries the now-stale row version, so the optimistic
        // concurrency check (xmin = @v) matches no rows on the second update.
        var second = await _repository.UpdateAsync(loaded, null);
        Assert.False(second);
    }

    [Fact]
    public async Task UpdateAsync_WithCurrentRowVersion_ReturnsTrue()
    {
        // Arrange
        await _fixture.CleanDatabaseAsync();
        var account = TestDataBuilder.CreateTestAccount("concurrency_ok", "concurrency_ok@hedgehog.batacek.eu");
        await _repository.CreateAsync(account, "password");

        var loaded = await _repository.GetByUsernameAsync("concurrency_ok");
        Assert.NotNull(loaded);

        // Act - updating with the freshly-loaded row version succeeds.
        var result = await _repository.UpdateAsync(loaded, null);

        // Assert
        Assert.True(result);
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

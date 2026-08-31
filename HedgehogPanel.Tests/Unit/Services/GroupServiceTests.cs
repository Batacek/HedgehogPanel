using Xunit;
using Moq;
using System;
using System.Threading.Tasks;
using HedgehogPanel.Application.Repositories;
using HedgehogPanel.Application.Services;
using HedgehogPanel.Domain.Entities;
using HedgehogPanel.Infrastructure.Persistence.Store;

namespace HedgehogPanel.Tests.Unit.Services;

public class GroupServiceTests
{
    private readonly Mock<IGroupRepository> _groups = new();
    private readonly Mock<IAccountRepository> _accounts = new();
    private readonly Mock<IDataProvider> _dataProvider = new();
    private readonly GroupService _service;

    private static readonly Guid GroupGuid = Guid.NewGuid();
    private static readonly Guid UserGuid = Guid.NewGuid();

    public GroupServiceTests()
    {
        _service = new GroupService(_groups.Object, _accounts.Object, _dataProvider.Object);
    }

    private void SetupGroupAndUser()
    {
        _groups.Setup(r => r.GetByNameAsync("staff")).ReturnsAsync(new Group(GroupGuid, "staff"));
        _accounts.Setup(r => r.GetByUsernameAsync("bob")).ReturnsAsync(new Account(UserGuid, "bob", "bob@hedgehog.batacek.eu"));
    }

    [Fact]
    public async Task AddMemberAsync_WithValidInputs_AddsAndInvalidatesCache()
    {
        SetupGroupAndUser();
        _groups.Setup(r => r.AddMemberAsync(GroupGuid, UserGuid, 50)).ReturnsAsync(true);

        var result = await _service.AddMemberAsync("staff", "bob", 50);

        Assert.Equal(GroupMemberOperationResult.Success, result);
        _groups.Verify(r => r.AddMemberAsync(GroupGuid, UserGuid, 50), Times.Once);
        _dataProvider.Verify(d => d.InvalidateAccount(UserGuid), Times.Once);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(256)]
    public async Task AddMemberAsync_WithOutOfRangePriority_ReturnsInvalidPriority(int priority)
    {
        var result = await _service.AddMemberAsync("staff", "bob", priority);

        Assert.Equal(GroupMemberOperationResult.InvalidPriority, result);
        _groups.Verify(r => r.GetByNameAsync(It.IsAny<string>()), Times.Never);
        _groups.Verify(r => r.AddMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task AddMemberAsync_WhenGroupMissing_ReturnsGroupNotFound()
    {
        _groups.Setup(r => r.GetByNameAsync("ghost")).ReturnsAsync((Group?)null);

        var result = await _service.AddMemberAsync("ghost", "bob", 0);

        Assert.Equal(GroupMemberOperationResult.GroupNotFound, result);
        _accounts.Verify(r => r.GetByUsernameAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddMemberAsync_WhenUserMissing_ReturnsUserNotFound()
    {
        _groups.Setup(r => r.GetByNameAsync("staff")).ReturnsAsync(new Group(GroupGuid, "staff"));
        _accounts.Setup(r => r.GetByUsernameAsync("nobody")).ReturnsAsync((Account?)null);

        var result = await _service.AddMemberAsync("staff", "nobody", 0);

        Assert.Equal(GroupMemberOperationResult.UserNotFound, result);
        _groups.Verify(r => r.AddMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
        _dataProvider.Verify(d => d.InvalidateAccount(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task RemoveMemberAsync_WhenRemoved_InvalidatesCache()
    {
        SetupGroupAndUser();
        _groups.Setup(r => r.RemoveMemberAsync(GroupGuid, UserGuid)).ReturnsAsync(true);

        var result = await _service.RemoveMemberAsync("staff", "bob");

        Assert.Equal(GroupMemberOperationResult.Success, result);
        _dataProvider.Verify(d => d.InvalidateAccount(UserGuid), Times.Once);
    }

    [Fact]
    public async Task RemoveMemberAsync_WhenNotAMember_DoesNotInvalidateCache()
    {
        SetupGroupAndUser();
        _groups.Setup(r => r.RemoveMemberAsync(GroupGuid, UserGuid)).ReturnsAsync(false);

        var result = await _service.RemoveMemberAsync("staff", "bob");

        Assert.Equal(GroupMemberOperationResult.Success, result);
        _dataProvider.Verify(d => d.InvalidateAccount(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteGroupByNameAsync_WhenMissing_ReturnsFalse()
    {
        _groups.Setup(r => r.GetByNameAsync("ghost")).ReturnsAsync((Group?)null);

        var result = await _service.DeleteGroupByNameAsync("ghost");

        Assert.False(result);
        _groups.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CreateGroupAsync_CreatesViaRepository()
    {
        _groups.Setup(r => r.CreateAsync(It.IsAny<Group>())).ReturnsAsync(true);

        var result = await _service.CreateGroupAsync("staff", "Staff members");

        Assert.Equal("staff", result.Name);
        Assert.Equal("Staff members", result.Description);
        _groups.Verify(r => r.CreateAsync(It.Is<Group>(g => g.Name == "staff" && g.Description == "Staff members")), Times.Once);
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HedgehogPanel.Application.Repositories;
using HedgehogPanel.Domain.Entities;
using HedgehogPanel.Infrastructure.Persistence.Store;

namespace HedgehogPanel.Application.Services;

public class GroupService : IGroupService
{
    // user_groups.priority maps onto Group.Priority (a byte), so it is bounded to 0..255.
    private const int MinPriority = 0;
    private const int MaxPriority = 255;

    private readonly IGroupRepository _groups;
    private readonly IAccountRepository _accounts;
    private readonly IDataProvider _dataProvider;

    public GroupService(IGroupRepository groups, IAccountRepository accounts, IDataProvider dataProvider)
    {
        _groups = groups;
        _accounts = accounts;
        _dataProvider = dataProvider;
    }

    public Task<IReadOnlyList<Group>> ListGroupsAsync(int limit = 100, int offset = 0)
        => _groups.ListAsync(limit, offset);

    public Task<Group?> GetGroupByNameAsync(string name)
        => _groups.GetByNameAsync(name);

    public async Task<Group> CreateGroupAsync(string name, string? description)
    {
        var group = new Group(Guid.NewGuid(), name, description);
        var success = await _groups.CreateAsync(group);
        if (!success) throw new Exception("Failed to create group");
        return group;
    }

    public async Task<bool> DeleteGroupByNameAsync(string name)
    {
        var group = await _groups.GetByNameAsync(name);
        if (group is null) return false;
        return await _groups.DeleteAsync(group.Guid);
    }

    public async Task<IReadOnlyList<GroupMember>?> GetMembersByGroupNameAsync(string name)
    {
        var group = await _groups.GetByNameAsync(name);
        if (group is null) return null;
        return await _groups.GetMembersAsync(group.Guid);
    }

    public async Task<GroupMemberOperationResult> AddMemberAsync(string groupName, string username, int priority)
    {
        if (priority < MinPriority || priority > MaxPriority)
            return GroupMemberOperationResult.InvalidPriority;

        var group = await _groups.GetByNameAsync(groupName);
        if (group is null) return GroupMemberOperationResult.GroupNotFound;

        var user = await _accounts.GetByUsernameAsync(username);
        if (user is null) return GroupMemberOperationResult.UserNotFound;

        await _groups.AddMemberAsync(group.Guid, user.Guid, priority);

        // Groups are loaded as part of the account, so the cached account is now stale.
        _dataProvider.InvalidateAccount(user.Guid);
        return GroupMemberOperationResult.Success;
    }

    public async Task<GroupMemberOperationResult> RemoveMemberAsync(string groupName, string username)
    {
        var group = await _groups.GetByNameAsync(groupName);
        if (group is null) return GroupMemberOperationResult.GroupNotFound;

        var user = await _accounts.GetByUsernameAsync(username);
        if (user is null) return GroupMemberOperationResult.UserNotFound;

        var removed = await _groups.RemoveMemberAsync(group.Guid, user.Guid);
        if (removed)
        {
            _dataProvider.InvalidateAccount(user.Guid);
        }
        return GroupMemberOperationResult.Success;
    }
}

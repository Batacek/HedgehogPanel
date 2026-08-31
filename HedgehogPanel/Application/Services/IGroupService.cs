using System.Collections.Generic;
using System.Threading.Tasks;
using HedgehogPanel.Application.Repositories;
using HedgehogPanel.Domain.Entities;

namespace HedgehogPanel.Application.Services;

/// <summary>Outcome of adding/removing a user to/from a group.</summary>
public enum GroupMemberOperationResult
{
    Success,
    GroupNotFound,
    UserNotFound,
    InvalidPriority
}

public interface IGroupService
{
    Task<IReadOnlyList<Group>> ListGroupsAsync(int limit = 100, int offset = 0);

    Task<Group?> GetGroupByNameAsync(string name);

    /// <summary>Creates a group. Throws <see cref="HedgehogPanel.Domain.Exceptions.DuplicateEntityException"/> if the name is taken.</summary>
    Task<Group> CreateGroupAsync(string name, string? description);

    /// <summary>Deletes a group by name. Returns false if no such group exists.</summary>
    Task<bool> DeleteGroupByNameAsync(string name);

    /// <summary>Lists members of a group, or null if the group does not exist.</summary>
    Task<IReadOnlyList<GroupMember>?> GetMembersByGroupNameAsync(string name);

    /// <summary>Adds (or re-prioritizes) a user in a group. Invalidates the affected account cache.</summary>
    Task<GroupMemberOperationResult> AddMemberAsync(string groupName, string username, int priority);

    /// <summary>Removes a user from a group. Invalidates the affected account cache.</summary>
    Task<GroupMemberOperationResult> RemoveMemberAsync(string groupName, string username);
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HedgehogPanel.Domain.Entities;

namespace HedgehogPanel.Application.Repositories;

/// <summary>A user's membership in a group, including the per-membership priority.</summary>
public record GroupMember(Guid UserGuid, string Username, string Email, int Priority);

public interface IGroupRepository
{
    /// <summary>Retrieves a group by its globally unique identifier (GUID).</summary>
    Task<Group?> GetByGuidAsync(Guid guid);

    /// <summary>Retrieves a group by its (unique) name.</summary>
    Task<Group?> GetByNameAsync(string name);

    /// <summary>Retrieves a list of groups with pagination support.</summary>
    Task<IReadOnlyList<Group>> ListAsync(int limit, int offset);

    /// <summary>Creates a new group.</summary>
    Task<bool> CreateAsync(Group group);

    /// <summary>Updates an existing group's name/description.</summary>
    Task<bool> UpdateAsync(Group group);

    /// <summary>Deletes a group by GUID. Memberships are removed via ON DELETE CASCADE.</summary>
    Task<bool> DeleteAsync(Guid guid);

    /// <summary>Lists the members of a group together with their priority.</summary>
    Task<IReadOnlyList<GroupMember>> GetMembersAsync(Guid groupGuid);

    /// <summary>Adds a user to a group, or updates the priority if already a member.</summary>
    Task<bool> AddMemberAsync(Guid groupGuid, Guid userGuid, int priority);

    /// <summary>Removes a user from a group. Returns false if the user was not a member.</summary>
    Task<bool> RemoveMemberAsync(Guid groupGuid, Guid userGuid);
}

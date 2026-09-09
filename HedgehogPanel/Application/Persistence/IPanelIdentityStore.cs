using System;

namespace HedgehogPanel.Application.Persistence;

/// <summary>
/// The panel's identity, and whether this process is the one that created it.
/// </summary>
public readonly record struct PanelIdentity(Guid Uuid, bool Created);

/// <summary>
/// Stores the panel's identity on the machine the panel runs on.
/// </summary>
/// <remarks>
/// Deliberately not the database. Daemons accept a panel only when the token and
/// the panel UUID match, so keeping the UUID local means a copy of the database
/// is not by itself enough to impersonate the panel: a fresh install pointed at
/// the same database gets a new identity and has to be paired again.
/// </remarks>
public interface IPanelIdentityStore
{
    /// <summary>
    /// Reads the stored identity, creating one if this install has none yet.
    /// </summary>
    PanelIdentity LoadOrCreate();
}

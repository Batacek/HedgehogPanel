using System;

namespace HedgehogPanel.Application.Services;

/// <summary>
/// Supplies this panel's stable identity.
/// </summary>
/// <remarks>
/// Daemons remember the panel by this UUID once paired, so it has to be the same
/// value on every restart of this install — and a different value on any other
/// install, including one connected to the same database.
/// </remarks>
public interface IPanelIdentityProvider
{
    /// <summary>
    /// This panel's UUID, resolved during startup.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if read before startup has resolved it.
    /// </exception>
    Guid PanelUuid { get; }
}

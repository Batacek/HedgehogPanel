using System;

namespace HedgehogPanel.Application.Exceptions;

/// <summary>Why a call to a daemon failed, in terms the panel can act on.</summary>
public enum DaemonFailure
{
    /// <summary>The node's address cannot be turned into a daemon endpoint.</summary>
    InvalidAddress,

    /// <summary>The daemon could not be reached at its address.</summary>
    Unreachable,

    /// <summary>The daemon did not answer within the configured timeout.</summary>
    Timeout,

    /// <summary>The node has no token yet, so authenticated calls cannot be made.</summary>
    NotPaired,

    /// <summary>The daemon rejected the panel's credentials.</summary>
    Unauthenticated,

    /// <summary>The credentials were accepted but this operation is not allowed.</summary>
    PermissionDenied,

    /// <summary>The daemon does not implement this call.</summary>
    NotSupported,

    /// <summary>The daemon's protocol version cannot work with this panel's.</summary>
    Incompatible,

    /// <summary>A different daemon answered than the one this node was paired with.</summary>
    IdentityMismatch,

    /// <summary>The one-time pairing code was wrong, expired or already used.</summary>
    InvalidOneTimeCode,

    /// <summary>The daemon says this panel is already registered.</summary>
    AlreadyRegistered,

    /// <summary>The daemon rejected the request as malformed.</summary>
    InvalidRequest,

    /// <summary>Anything else: an unexpected status, or a response that makes no sense.</summary>
    Unexpected
}

/// <summary>Raised when a call to a daemon does not produce a usable result.</summary>
public class DaemonException : HedgehogException
{
    public DaemonFailure Failure { get; }

    public DaemonException(DaemonFailure failure, string message) : base(message)
    {
        Failure = failure;
    }

    public DaemonException(DaemonFailure failure, string message, Exception inner) : base(message, inner)
    {
        Failure = failure;
    }
}

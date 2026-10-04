namespace HedgehogPanel.Domain.Enums;

/// <summary>
/// How far the panel and a daemon can work together, judged by their protocol versions.
/// The rules are written out in the header of hedgehog.proto.
/// </summary>
public enum ProtocolCompatibility
{
    /// <summary>Same major and minor version: everything both sides know is available.</summary>
    Full,

    /// <summary>Same major version only: pairing and health, nothing added since.</summary>
    Limited,

    /// <summary>A 0.y.z build against a 1.y.z one: limited, and the user must be warned it is a pre-release.</summary>
    LimitedPreRelease,

    /// <summary>Different major versions, or a version that cannot be read: do not connect.</summary>
    Incompatible
}

namespace HedgehogPanel.Core.Logging;

public record SecurityEvent(
    string EventType,
    Guid? UserId,
    Guid? ActorUserId,
    string IpAddress,
    string? UserAgent,
    bool Success,
    object? Metadata = null
);

using System;
using HedgehogPanel.Domain.Entities;
using HedgehogPanel.Domain.Enums;
using Xunit;

namespace HedgehogPanel.Tests.Unit.Domain;

public class ServerTests
{
    [Fact]
    public void UpdateStatus_ChangesStatusAndStampsLastSeen()
    {
        var server = new Server(Guid.NewGuid(), "srv", "host.hedgehog.batacek.eu");
        Assert.Null(server.LastSeen);

        server.UpdateStatus(ServerStatus.Online);

        Assert.Equal(ServerStatus.Online, server.Status);
        Assert.NotNull(server.LastSeen);
    }

    [Fact]
    public void Constructor_DefaultsStatusToUnknownAndPortTo22()
    {
        var server = new Server(Guid.NewGuid(), "srv", "host.hedgehog.batacek.eu");

        Assert.Equal(ServerStatus.Unknown, server.Status);
        Assert.Equal(22, server.DaemonPort);
    }

    [Fact]
    public void Constructor_WithNullName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Server(Guid.NewGuid(), null!, "host.hedgehog.batacek.eu"));
    }

    [Fact]
    public void Constructor_WithNullHostname_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Server(Guid.NewGuid(), "srv", null!));
    }

    [Fact]
    public void Constructor_StoresProvidedValues()
    {
        var id = Guid.NewGuid();
        var server = new Server(id, "srv", "host.hedgehog.batacek.eu", 2222, ServerStatus.Offline, description: "desc");

        Assert.Equal(id, server.Guid);
        Assert.Equal("srv", server.Name);
        Assert.Equal("host.hedgehog.batacek.eu", server.Hostname);
        Assert.Equal(2222, server.DaemonPort);
        Assert.Equal(ServerStatus.Offline, server.Status);
        Assert.Equal("desc", server.Description);
    }
}

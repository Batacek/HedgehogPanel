using System;
using System.Threading;
using System.Threading.Tasks;
using HedgehogPanel.Application.Persistence;
using HedgehogPanel.Application.Services;
using Moq;
using Xunit;

namespace HedgehogPanel.Tests.Unit.Services;

public class PanelIdentityProviderTests
{
    private static PanelIdentityProvider ProviderOver(PanelIdentity identity)
    {
        var store = new Mock<IPanelIdentityStore>();
        store.Setup(s => s.LoadOrCreate()).Returns(identity);
        return new PanelIdentityProvider(store.Object);
    }

    [Fact]
    public async Task StartAsync_ExposesTheStoredIdentity()
    {
        // Arrange
        var stored = Guid.NewGuid();
        var provider = ProviderOver(new PanelIdentity(stored, Created: false));

        // Act
        await provider.StartAsync(CancellationToken.None);

        // Assert
        Assert.Equal(stored, provider.PanelUuid);
    }

    [Fact]
    public async Task StartAsync_ExposesAFreshlyCreatedIdentity()
    {
        // Arrange
        var created = Guid.NewGuid();
        var provider = ProviderOver(new PanelIdentity(created, Created: true));

        // Act
        await provider.StartAsync(CancellationToken.None);

        // Assert
        Assert.Equal(created, provider.PanelUuid);
    }

    [Fact]
    public async Task StartAsync_ReadsTheStoreOnlyOnce()
    {
        // Arrange
        var store = new Mock<IPanelIdentityStore>();
        store.Setup(s => s.LoadOrCreate()).Returns(new PanelIdentity(Guid.NewGuid(), false));
        var provider = new PanelIdentityProvider(store.Object);

        // Act
        await provider.StartAsync(CancellationToken.None);
        _ = provider.PanelUuid;
        _ = provider.PanelUuid;

        // Assert
        store.Verify(s => s.LoadOrCreate(), Times.Once);
    }

    [Fact]
    public async Task StartAsync_PropagatesAStoreFailure()
    {
        // Arrange — a corrupted identity must stop the host, not be swallowed.
        var store = new Mock<IPanelIdentityStore>();
        store.Setup(s => s.LoadOrCreate()).Throws(new InvalidOperationException("bad identity file"));
        var provider = new PanelIdentityProvider(store.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.StartAsync(CancellationToken.None));
        Assert.Contains("bad identity file", ex.Message);
    }

    [Fact]
    public void PanelUuid_BeforeStartup_Throws()
    {
        // Arrange
        var provider = new PanelIdentityProvider(new Mock<IPanelIdentityStore>().Object);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => provider.PanelUuid);
    }
}

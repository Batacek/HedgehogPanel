using System;
using System.IO;
using HedgehogPanel.Infrastructure.Configuration;
using HedgehogPanel.Infrastructure.Identity;
using Xunit;

namespace HedgehogPanel.Tests.Unit.Identity;

public class FilePanelIdentityStoreTests : IDisposable
{
    private readonly string _directory;
    private readonly string _path;

    public FilePanelIdentityStoreTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"hedgehog_identity_{Guid.NewGuid():N}");
        _path = Path.Combine(_directory, "panel-identity");
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private FilePanelIdentityStore CreateStore() =>
        new(new HedgehogConfig { Identity = new IdentityConfig { FilePath = _path } });

    [Fact]
    public void LoadOrCreate_WithNoFile_CreatesTheIdentity()
    {
        // Act
        var identity = CreateStore().LoadOrCreate();

        // Assert
        Assert.True(identity.Created);
        Assert.NotEqual(Guid.Empty, identity.Uuid);
        Assert.Equal(identity.Uuid.ToString(), File.ReadAllText(_path).Trim());
    }

    [Fact]
    public void LoadOrCreate_CreatesTheDirectory_WhenMissing()
    {
        // Arrange
        Assert.False(Directory.Exists(_directory));

        // Act
        CreateStore().LoadOrCreate();

        // Assert
        Assert.True(File.Exists(_path));
    }

    [Fact]
    public void LoadOrCreate_OnLaterCalls_ReturnsTheSameIdentity()
    {
        // Arrange
        var first = CreateStore().LoadOrCreate();

        // Act — a separate instance, as a restarted panel would be
        var second = CreateStore().LoadOrCreate();

        // Assert
        Assert.False(second.Created);
        Assert.Equal(first.Uuid, second.Uuid);
    }

    [Fact]
    public void LoadOrCreate_WithADifferentFile_YieldsADifferentIdentity()
    {
        // Arrange — this is the security property: a second install does not
        // inherit the first one's identity just by sharing a database.
        var first = CreateStore().LoadOrCreate();
        var otherPath = Path.Combine(_directory, "other-install");
        var otherStore = new FilePanelIdentityStore(
            new HedgehogConfig { Identity = new IdentityConfig { FilePath = otherPath } });

        // Act
        var second = otherStore.LoadOrCreate();

        // Assert
        Assert.True(second.Created);
        Assert.NotEqual(first.Uuid, second.Uuid);
    }

    [Fact]
    public void LoadOrCreate_WithCorruptedContents_Throws()
    {
        // Arrange
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_path, "not-a-uuid");

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => CreateStore().LoadOrCreate());
        Assert.Contains("not-a-uuid", ex.Message);
    }

    [Fact]
    public void LoadOrCreate_WithAnEmptyFile_CreatesTheIdentity()
    {
        // Arrange — an interrupted first write leaves a zero-byte file behind.
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_path, string.Empty);

        // Act
        var identity = CreateStore().LoadOrCreate();

        // Assert
        Assert.NotEqual(Guid.Empty, identity.Uuid);
        Assert.Equal(identity.Uuid.ToString(), File.ReadAllText(_path).Trim());
    }

    [Fact]
    public void LoadOrCreate_OnUnix_RestrictsTheFileToItsOwner()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        // Act
        CreateStore().LoadOrCreate();

        // Assert
        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(_path));
    }
}

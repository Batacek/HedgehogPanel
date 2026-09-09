using System;
using System.IO;
using HedgehogPanel.Application.Persistence;
using HedgehogPanel.Infrastructure.Configuration;

namespace HedgehogPanel.Infrastructure.Identity;

/// <summary>
/// Keeps the panel's identity in a file next to the panel, readable only by the
/// account the panel runs as.
/// </summary>
public sealed class FilePanelIdentityStore : IPanelIdentityStore
{
    private readonly string _path;

    public FilePanelIdentityStore(HedgehogConfig config)
    {
        _path = config.Identity.FilePath;
    }

    public PanelIdentity LoadOrCreate()
    {
        var existing = TryRead();
        if (existing.HasValue)
        {
            return new PanelIdentity(existing.Value, false);
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(_path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var candidate = Guid.NewGuid();

        // A file that exists but reads as empty is a first write that never
        // finished, so replacing it is safe. Otherwise CreateNew is what stops
        // two panels starting together from overwriting each other's identity.
        var mode = File.Exists(_path) ? FileMode.Create : FileMode.CreateNew;

        try
        {
            using var stream = new FileStream(_path, mode, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream);
            writer.Write(candidate.ToString());
        }
        catch (IOException) when (File.Exists(_path))
        {
            var winner = TryRead() ?? throw new InvalidOperationException(
                $"Identity file '{_path}' is held by another process and holds no usable identity.");
            return new PanelIdentity(winner, false);
        }

        RestrictToOwner();
        return new PanelIdentity(candidate, true);
    }

    private Guid? TryRead()
    {
        if (!File.Exists(_path))
        {
            return null;
        }

        var contents = File.ReadAllText(_path).Trim();
        if (contents.Length == 0)
        {
            return null;
        }

        if (!Guid.TryParse(contents, out var uuid))
        {
            throw new InvalidOperationException(
                $"Identity file '{_path}' holds '{contents}', which is not a UUID. Correct it, or " +
                "delete the file to start a new identity — every paired daemon then has to be paired again.");
        }

        return uuid;
    }

    private void RestrictToOwner()
    {
        // Windows inherits the directory ACL; there is no cheap equivalent of
        // chmod 600 that does not risk locking the panel out of its own file.
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        File.SetUnixFileMode(_path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
}

// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Projects;

using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

/// <summary>
///     Prepares a referenced Godot project on the test host, before the Godot runtime starts:
///     decides whether its assets must be imported and packs its files into a resource pack the runtime mounts.
/// </summary>
internal static class GodotProjectPacker
{
    // Files of the C# build and the Godot editor backups, they are no project resources.
    private static readonly string[] BuildFileExtensions = [".csproj", ".sln", ".slnx", ".old"];

    /// <summary>
    ///     Lists the files of a Godot project, ordered by their entry path.
    ///     Hidden directories, C# build outputs and nested Godot projects are skipped, from `.godot/` only the imported data is listed.
    ///     A linked directory is listed once, a link to a parent directory does not loop.
    ///     A directory that can't be read and a link to a missing file are skipped, the tests don't need every project file.
    /// </summary>
    /// <param name="projectDir">The Godot project directory.</param>
    /// <param name="skipped">Collects the entry paths of the skipped files and directories.</param>
    /// <returns>The full path and the entry path relative to the project of each file.</returns>
    internal static IReadOnlyList<(string Path, string Entry)> Scan(string projectDir, ISet<string>? skipped = null)
    {
        var files = new List<(string Path, string Entry)>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        Collect(new DirectoryInfo(projectDir), string.Empty);
        files.Sort((left, right) => string.CompareOrdinal(left.Entry, right.Entry));
        return files;

        void Collect(DirectoryInfo directory, string prefix)
        {
            if (!visited.Add(directory.ResolveLinkTarget(true)?.FullName ?? directory.FullName))
                return;

            try
            {
                foreach (var file in directory.EnumerateFiles())
                {
                    if (IsReadable(file))
                        files.Add((file.FullName, prefix + file.Name));
                    else
                        _ = skipped?.Add(prefix + file.Name);
                }

                foreach (var subDirectory in directory.EnumerateDirectories())
                {
                    var entry = $"{prefix}{subDirectory.Name}/";
                    if (IsScanned(entry, subDirectory.FullName))
                        Collect(subDirectory, entry);
                }
            }
            catch (Exception e) when (e is UnauthorizedAccessException or IOException)
            {
                _ = skipped?.Add(prefix);
            }
        }
    }

    /// <summary>
    ///     Locks the import of a referenced project, parallel runs of all test projects that reference it import it
    ///     one after the other. The operating system releases the lock when the process ends.
    /// </summary>
    /// <param name="reference">The referenced Godot project.</param>
    /// <param name="timeout">The maximum time to wait for a parallel run.</param>
    /// <returns>The lock, released on dispose.</returns>
    /// <exception cref="TimeoutException">Thrown when a parallel run holds the lock longer than the timeout.</exception>
    internal static IDisposable Lock(GodotProjectReference reference, TimeSpan timeout)
        => Lock(reference.LockFile, timeout, $"The referenced Godot project '{reference.ProjectDir}' is prepared by a parallel test run");

    internal static bool IsImportUpToDate(GodotProjectReference reference, IReadOnlyList<(string Path, string Entry)> files, string godotBinary)
    {
        var stampFile = reference.ImportStampFile;
        return File.Exists(stampFile) && File.ReadAllText(stampFile) == ImportStamp(files, godotBinary);
    }

    // Taken after the import, the import itself may rewrite project files, e.g. the project.godot, the `.import` or `.uid` files.
    internal static void WriteImportStamp(GodotProjectReference reference, IReadOnlyList<(string Path, string Entry)> files, string godotBinary)
    {
        var stampFile = reference.ImportStampFile;
        _ = Directory.CreateDirectory(Path.GetDirectoryName(stampFile)!);
        File.WriteAllText(stampFile, ImportStamp(files, godotBinary));
    }

    /// <summary>
    ///     Computes the fingerprint of the files an import depends on and of the imported data, from their paths, sizes and
    ///     modification times. C# sources are compiled, not imported.
    /// </summary>
    /// <param name="files">The project files.</param>
    /// <returns>The fingerprint as hex string.</returns>
    internal static string ImportFingerprint(IReadOnlyList<(string Path, string Entry)> files)
        => Fingerprint(files.Where(file => (!IsCacheEntry(file.Entry) || file.Entry.StartsWith(".godot/imported/", StringComparison.Ordinal))
                                           && !HasExtension(file.Entry, ".cs")
                                           && !HasExtension(file.Entry, BuildFileExtensions)));

    /// <summary>
    ///     Packs the project files into a resource pack named by their fingerprint, an existing pack with the same files is reused.
    ///     The pack is marked as in use by this process until the marker is deleted. The packs of earlier runs are deleted,
    ///     unless a live process still uses them.
    /// </summary>
    /// <param name="reference">The referenced Godot project.</param>
    /// <param name="testProjectDir">The test project directory.</param>
    /// <param name="files">The project files.</param>
    /// <param name="timeout">The maximum time to wait for a parallel run of the test project.</param>
    /// <returns>The pack, its in-use marker, and the project files that also exist in the test project and stay hidden by them.</returns>
    /// <exception cref="TimeoutException">Thrown when a parallel run of the test project writes its pack longer than the timeout.</exception>
    internal static (string PackFile, string InUseMarker, IReadOnlyList<string> Collisions) CreatePack(
        GodotProjectReference reference,
        string testProjectDir,
        IReadOnlyList<(string Path, string Entry)> files,
        TimeSpan timeout)
    {
        var packFiles = files.Where(file => IsPackEntry(file.Entry)).ToList();
        var fingerprint = Fingerprint(packFiles);
        var packFile = reference.PackFile(testProjectDir, fingerprint);

        // the packs are per test project, parallel runs of other test projects are not blocked
        using var packLock = Lock(
            Path.Combine(Path.GetDirectoryName(packFile)!, $"{reference.Id}.lock"),
            timeout,
            $"The resource pack of '{reference.ProjectDir}' is written by a parallel run of the test project");

        // marked before it is created, so a parallel run never deletes it in between
        var inUseMarker = $"{packFile}.{Environment.ProcessId}.{Guid.NewGuid():N}.inuse";
        File.WriteAllBytes(inUseMarker, []);
        try
        {
            if (!File.Exists(packFile))
                WritePack(packFile, packFiles, fingerprint);
            DeleteUnusedPacks(reference, packFile);
        }
        catch
        {
            File.Delete(inUseMarker);
            throw;
        }

        // the `.import` and `.uid` files and the imported data of a shared file are no collisions of their own
        var collisions = packFiles
            .Select(file => file.Entry)
            .Where(entry => !entry.StartsWith(".godot/", StringComparison.Ordinal) && !HasExtension(entry, ".import", ".uid"))
            .Where(entry => File.Exists(Path.Combine(testProjectDir, entry)))
            .ToList();
        return (packFile, inUseMarker, collisions);
    }

    // From `.godot/` only the imported data is needed; the project settings are applied by the runtime instead.
    internal static bool IsPackEntry(string entry)
    {
        if (entry.StartsWith(".godot/", StringComparison.Ordinal))
            return entry.StartsWith(".godot/imported/", StringComparison.Ordinal);
        return !IsCacheEntry(entry)
               && entry is not ("project.godot" or "override.cfg")
               && !HasExtension(entry, BuildFileExtensions);
    }

    /// <summary>
    ///     Keeps the project files the Godot import may migrate, e.g. it rewrites the C# project and keeps the original as `.old`.
    ///     Running the tests must not change the sources of the referenced project.
    /// </summary>
    /// <param name="projectDir">The Godot project directory.</param>
    /// <returns>The action that restores the project files.</returns>
    internal static Action BackupProjectFiles(string projectDir)
    {
        var backups = Directory.GetFiles(projectDir, "*.csproj")
            .Append(Path.Combine(projectDir, "project.godot"))
            .Where(File.Exists)
            .Select(file => (File: file, Content: File.ReadAllBytes(file), HasOldFile: File.Exists($"{file}.old")))
            .ToList();
        return () =>
        {
            foreach (var (file, content, hasOldFile) in backups)
            {
                if (!File.Exists(file) || !File.ReadAllBytes(file).AsSpan().SequenceEqual(content))
                    File.WriteAllBytes(file, content);
                if (!hasOldFile)
                    TryDelete($"{file}.old");
            }
        };
    }

    // Cleanup only frees disk space, a file another process holds open is deleted by a later run.
    internal static void TryDelete(string file)
    {
        try
        {
            File.Delete(file);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static FileStream Lock(string lockFile, TimeSpan timeout, string holder)
    {
        _ = Directory.CreateDirectory(Path.GetDirectoryName(lockFile)!);
        var waiting = Stopwatch.StartNew();
        while (true)
        {
            try
            {
                return new FileStream(lockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException) when (waiting.Elapsed < timeout)
            {
                Thread.Sleep(100);
            }
            catch (IOException)
            {
                throw new TimeoutException($"{holder}, waiting for it timed out after {timeout.TotalMilliseconds}ms.");
            }
        }
    }

    // The imported data depends on the Godot version, an import of another Godot binary is not reused.
    private static string ImportStamp(IReadOnlyList<(string Path, string Entry)> files, string godotBinary)
    {
        var binary = new FileInfo(godotBinary);
        return $"{ImportFingerprint(files)}\n{binary.FullName}|{(binary.Exists ? binary.Length : 0)}|{binary.LastWriteTimeUtc.Ticks}";
    }

    private static bool IsReadable(FileInfo file)
    {
        if (!file.Attributes.HasFlag(FileAttributes.ReparsePoint))
            return true;
        try
        {
            return file.ResolveLinkTarget(true) is { Exists: true };
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static bool IsScanned(string entry, string directory)
    {
        if (entry.StartsWith(".godot/", StringComparison.Ordinal))
            return entry is ".godot/" || entry.StartsWith(".godot/imported/", StringComparison.Ordinal);
        return !IsCacheEntry(entry) && !File.Exists(Path.Combine(directory, "project.godot")) && !IsBuildOutput(directory);
    }

    // Hidden files and directories at any depth, the Godot cache and the C# build output of the project.
    private static bool IsCacheEntry(string entry)
        => entry.StartsWith("bin/", StringComparison.Ordinal)
           || entry.StartsWith("obj/", StringComparison.Ordinal)
           || entry.Split('/').Any(segment => segment.StartsWith('.'));

    // A `bin` or `obj` directory is a build output next to a C# project, otherwise it holds resources, e.g. `models/obj/`.
    private static bool IsBuildOutput(string directory)
        => Path.GetFileName(directory) is "bin" or "obj"
           && Directory.EnumerateFiles(Path.GetDirectoryName(directory)!, "*.csproj").Any();

    private static bool HasExtension(string entry, params string[] extensions)
        => extensions.Contains(Path.GetExtension(entry), StringComparer.OrdinalIgnoreCase);

    private static string Fingerprint(IEnumerable<(string Path, string Entry)> files)
    {
        var lines = files.Select(file =>
        {
            var info = new FileInfo(file.Path);
            return $"{file.Entry}|{info.Length}|{info.LastWriteTimeUtc.Ticks}";
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', lines))));
    }

    private static void WritePack(string packFile, List<(string Path, string Entry)> files, string fingerprint)
    {
        var tempFile = $"{packFile}.{Guid.NewGuid():N}.tmp";
        try
        {
            using (var pack = ZipFile.Open(tempFile, ZipArchiveMode.Create))
            {
                foreach (var (path, entry) in files)
                    _ = pack.CreateEntryFromFile(path, entry, CompressionLevel.NoCompression);
            }

            // a parallel import of another test project may rewrite the imported data, a pack must hold the files its name stands for
            if (Fingerprint(files) != fingerprint)
                throw new IOException($"The files of the resource pack '{packFile}' changed while packing them, run the tests again.");

            File.Move(tempFile, packFile);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // Godot reopens a mounted pack on every file access, a pack is only deleted when no live process marks it as in use.
    // Runs under the pack lock of the test project, a parallel run can't mark the pack between the check and the delete.
    private static void DeleteUnusedPacks(GodotProjectReference reference, string packFile)
    {
        var workingDir = Path.GetDirectoryName(packFile)!;
        foreach (var pack in Directory.GetFiles(workingDir, $"{reference.Id}-*.zip"))
        {
            var inUse = false;
            foreach (var marker in Directory.GetFiles(workingDir, $"{Path.GetFileName(pack)}.*.inuse"))
            {
                if (IsMarkerOwnerAlive(marker))
                    inUse = true;
                else
                    TryDelete(marker);
            }

            if (!inUse && pack != packFile)
                TryDelete(pack);
        }

        // left by a run that ended while writing a pack, no parallel run writes one while the lock is held
        foreach (var tempFile in Directory.GetFiles(workingDir, $"{reference.Id}-*.tmp"))
            TryDelete(tempFile);
    }

    // The marker is named `<pack>.<process id>.<run id>.inuse`.
    private static bool IsMarkerOwnerAlive(string marker)
    {
        var parts = Path.GetFileName(marker).Split('.');
        if (!int.TryParse(parts[^3], out var processId))
            return false;
        try
        {
            using var process = Process.GetProcessById(processId);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}

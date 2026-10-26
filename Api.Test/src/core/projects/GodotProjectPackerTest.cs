namespace GdUnit4.Tests.Core.Projects;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

using GdUnit4.Core.Projects;

using static Assertions;

[TestSuite]
public class GodotProjectPackerTest
{
    private string TempDir { get; set; } = string.Empty;

    private string TestProjectDir => Path.Combine(TempDir, "Game.Test");

    private GodotProjectReference Reference => new(Path.Combine(TempDir, "Game"));

    private string GodotBinary => Path.Combine(TempDir, "godot.exe");

    [BeforeTest]
    public void CreateProjects()
    {
        TempDir = Path.Combine(Path.GetTempPath(), "gdunit4_" + Guid.NewGuid().ToString("N"));
        WriteFile(TestProjectDir, "project.godot");
        WriteFile(TestProjectDir, "Scenes/TestScene.tscn");

        WriteFile(Reference.ProjectDir, "project.godot");
        WriteFile(Reference.ProjectDir, "Game.csproj");
        WriteFile(Reference.ProjectDir, "icon.svg");
        WriteFile(Reference.ProjectDir, "icon.svg.import");
        WriteFile(Reference.ProjectDir, "Characters/Player.cs");
        WriteFile(Reference.ProjectDir, "Characters/Player.cs.uid");
        WriteFile(Reference.ProjectDir, "Characters/Player.tscn");
        WriteFile(Reference.ProjectDir, ".godot/imported/icon.svg-1234.ctex");
        WriteFile(Reference.ProjectDir, ".godot/uid_cache.bin");
        WriteFile(Reference.ProjectDir, ".godot/mono/temp/bin/Debug/Game.dll");
        WriteFile(TempDir, "godot.exe", "4.5");
    }

    [AfterTest]
    public void DeleteTempDir() => Directory.Delete(TempDir, true);

    #region Scan

    [TestCase]
    public void Scan()
    {
        WriteFile(Reference.ProjectDir, "bin/Debug/Game.dll");
        WriteFile(Reference.ProjectDir, "Tools/Tools.csproj");
        WriteFile(Reference.ProjectDir, "Tools/obj/project.assets.json");
        WriteFile(Reference.ProjectDir, "models/obj/tree.obj");
        WriteFile(Reference.ProjectDir, "Characters/.cache/Player.tmp");
        WriteFile(Reference.ProjectDir, "Game.Test/project.godot");
        WriteFile(Reference.ProjectDir, "Game.Test/Scenes/TestScene.tscn");

        AssertThat(GodotProjectPacker.Scan(Reference.ProjectDir).Select(file => file.Entry)).ContainsExactly(
            ".godot/imported/icon.svg-1234.ctex",
            ".godot/uid_cache.bin",
            "Characters/Player.cs",
            "Characters/Player.cs.uid",
            "Characters/Player.tscn",
            "Game.csproj",
            "Tools/Tools.csproj",
            "icon.svg",
            "icon.svg.import",
            "models/obj/tree.obj",
            "project.godot");
    }

    [TestCase]
    public void ScanFollowsALinkToAParentDirectoryOnce()
    {
        var link = Path.Combine(Reference.ProjectDir, "Characters", "Game");
        try
        {
            _ = Directory.CreateSymbolicLink(link, Reference.ProjectDir);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // creating symbolic links needs the developer mode on Windows
            return;
        }

        AssertThat(GodotProjectPacker.Scan(Reference.ProjectDir).Select(file => file.Entry))
            .Contains("Characters/Player.tscn")
            .NotContains("Characters/Game/Characters/Player.tscn");
    }

    [TestCase]
    public void ScanSkipsALinkToAMissingFile()
    {
        var link = Path.Combine(Reference.ProjectDir, "Characters", "Missing.tscn");
        try
        {
            _ = File.CreateSymbolicLink(link, Path.Combine(TempDir, "Missing.tscn"));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // creating symbolic links needs the developer mode on Windows
            return;
        }

        var skipped = new HashSet<string>();

        AssertThat(GodotProjectPacker.Scan(Reference.ProjectDir, skipped).Select(file => file.Entry))
            .Contains("Characters/Player.tscn")
            .NotContains("Characters/Missing.tscn");
        AssertThat(skipped).ContainsExactly("Characters/Missing.tscn");
    }

    [TestCase]
    public void ScanReturnsTheFullPaths()
        => AssertThat(GodotProjectPacker.Scan(Reference.ProjectDir).Where(file => file.Entry == "Characters/Player.tscn").Select(file => file.Path))
            .ContainsExactly(Path.Combine(Reference.ProjectDir, "Characters", "Player.tscn"));

    #endregion

    #region IsPackEntry

    [TestCase("icon.svg", true)]
    [TestCase("icon.svg.import", true)]
    [TestCase("Characters/Player.tscn", true)]
    [TestCase("Characters/Player.cs", true)]
    [TestCase("Characters/Player.cs.uid", true)]
    [TestCase(".godot/imported/icon.svg-1234.ctex", true)]
    [TestCase("project.godot", false)]
    [TestCase("override.cfg", false)]
    [TestCase("Game.csproj", false)]
    [TestCase("Game.CSPROJ", false)]
    [TestCase("Game.csproj.old", false)]
    [TestCase("Game.sln", false)]
    [TestCase(".godot/uid_cache.bin", false)]
    [TestCase(".godot/mono/temp/bin/Debug/Game.dll", false)]
    [TestCase(".godot/editor/project_metadata.cfg", false)]
    [TestCase("bin/Debug/Game.dll", false)]
    [TestCase("obj/project.assets.json", false)]
    [TestCase(".gitignore", false)]
    [TestCase("models/obj/tree.obj", true)]
    [TestCase("Characters/.cache/Player.tmp", false)]
    public void IsPackEntry(string entry, bool expected)
        => AssertThat(GodotProjectPacker.IsPackEntry(entry)).IsEqual(expected);

    #endregion

    #region CreatePack

    [TestCase]
    public void CreatePack()
    {
        var (packFile, inUseMarker, collisions) = CreatePack(Reference);

        AssertThat(Path.GetDirectoryName(packFile)).IsEqual(GodotProjectReference.WorkingDir(TestProjectDir));
        AssertThat(Path.GetFileName(packFile)).StartsWith($"{Reference.Id}-").EndsWith(".zip");
        AssertThat(PackEntries(packFile)).ContainsExactly(
            ".godot/imported/icon.svg-1234.ctex",
            "Characters/Player.cs",
            "Characters/Player.cs.uid",
            "Characters/Player.tscn",
            "icon.svg",
            "icon.svg.import");
        AssertThat(Path.GetFileName(inUseMarker)).StartsWith($"{Path.GetFileName(packFile)}.{Environment.ProcessId}.").EndsWith(".inuse");
        AssertThat(File.Exists(inUseMarker)).IsTrue();
        AssertThat(collisions).IsEmpty();
    }

    [TestCase]
    public void CreatePackReportsFilesThatExistInTheTestProject()
    {
        WriteFile(TestProjectDir, "Characters/Player.tscn");

        AssertThat(CreatePack(Reference).Collisions).ContainsExactly("Characters/Player.tscn");
    }

    [TestCase]
    public void CreatePackReportsASharedFileOnce()
    {
        WriteFile(TestProjectDir, "icon.svg");
        WriteFile(TestProjectDir, "icon.svg.import");
        WriteFile(TestProjectDir, ".godot/imported/icon.svg-1234.ctex");
        WriteFile(TestProjectDir, "Characters/Player.cs");
        WriteFile(TestProjectDir, "Characters/Player.cs.uid");

        AssertThat(CreatePack(Reference).Collisions).ContainsExactly("Characters/Player.cs", "icon.svg");
    }

    [TestCase]
    public void CreatePackDeletesLeftoversOfEndedRuns()
    {
        var (packFile, inUseMarker, _) = CreatePack(Reference);
        File.Delete(inUseMarker);
        var endedProcessMarker = $"{packFile}.{int.MaxValue}.0123456789abcdef.inuse";
        var tempFile = $"{packFile}.0123456789abcdef.tmp";
        File.WriteAllText(endedProcessMarker, string.Empty);
        File.WriteAllText(tempFile, string.Empty);

        AssertThat(CreatePack(Reference).PackFile).IsEqual(packFile);
        AssertThat(File.Exists(endedProcessMarker)).IsFalse();
        AssertThat(File.Exists(tempFile)).IsFalse();
    }

    [TestCase]
    public void CreatePackReusesThePackOfUnchangedFiles()
    {
        var packFile = CreatePack(Reference).PackFile;
        var created = File.GetLastWriteTimeUtc(packFile);

        AssertThat(CreatePack(Reference).PackFile).IsEqual(packFile);
        AssertThat(File.GetLastWriteTimeUtc(packFile)).IsEqual(created);
    }

    [TestCase]
    public void CreatePackReplacesAnUnusedPackOfChangedFiles()
    {
        var (previousPack, previousMarker, _) = CreatePack(Reference);
        File.Delete(previousMarker);
        File.Delete(Path.Combine(Reference.ProjectDir, "icon.svg"));

        var packFile = CreatePack(Reference).PackFile;

        AssertThat(packFile).IsNotEqual(previousPack);
        AssertThat(File.Exists(previousPack)).IsFalse();
        AssertThat(PackEntries(packFile)).NotContains("icon.svg");
        AssertThat(Directory.GetFiles(GodotProjectReference.WorkingDir(TestProjectDir), "*.tmp")).IsEmpty();
    }

    [TestCase]
    public void CreatePackKeepsAPackInUseByALiveProcess()
    {
        var previousPack = CreatePack(Reference).PackFile;
        File.Delete(Path.Combine(Reference.ProjectDir, "icon.svg"));

        var packFile = CreatePack(Reference).PackFile;

        AssertThat(packFile).IsNotEqual(previousPack);
        AssertThat(File.Exists(previousPack)).IsTrue();
    }

    [TestCase]
    public void CreatePackDeletesAPackInUseByAnEndedProcess()
    {
        var (previousPack, previousMarker, _) = CreatePack(Reference);
        File.Delete(previousMarker);
        var endedProcessMarker = $"{previousPack}.{int.MaxValue}.0123456789abcdef.inuse";
        File.WriteAllText(endedProcessMarker, string.Empty);
        File.Delete(Path.Combine(Reference.ProjectDir, "icon.svg"));

        _ = CreatePack(Reference);

        AssertThat(File.Exists(previousPack)).IsFalse();
        AssertThat(File.Exists(endedProcessMarker)).IsFalse();
    }

    [TestCase]
    public void CreatePackIgnoresAnOldReadOnlyPackOnWindows()
    {
        // On Unix, read-only files can still be unlinked, only Windows blocks this delete.
        if (!OperatingSystem.IsWindows())
            return;

        var (previousPack, previousMarker, _) = CreatePack(Reference);
        File.Delete(previousMarker);
        File.Delete(Path.Combine(Reference.ProjectDir, "icon.svg"));
        File.SetAttributes(previousPack, FileAttributes.ReadOnly);
        try
        {
            var packFile = CreatePack(Reference).PackFile;

            AssertThat(PackEntries(packFile)).NotContains("icon.svg");
        }
        finally
        {
            File.SetAttributes(previousPack, FileAttributes.Normal);
        }
    }

    [TestCase]
    public void CreatePackWaitsForAParallelRunOfTheTestProject()
    {
        var lockFile = Path.Combine(GodotProjectReference.WorkingDir(TestProjectDir), $"{Reference.Id}.lock");
        _ = Directory.CreateDirectory(Path.GetDirectoryName(lockFile)!);
        using (new FileStream(lockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
        {
            AssertThrown(() => GodotProjectPacker.CreatePack(Reference, TestProjectDir, GodotProjectPacker.Scan(Reference.ProjectDir), TimeSpan.FromMilliseconds(200)))
                .IsInstanceOf<TimeoutException>()
                .HasMessage($"The resource pack of '{Reference.ProjectDir}' is written by a parallel run of the test project, waiting for it timed out after 200ms.");
        }

        AssertThat(File.Exists(CreatePack(Reference).PackFile)).IsTrue();
    }

    [TestCase]
    public void CreatePackIsNotBlockedByTheImportLock()
    {
        using var importLock = GodotProjectPacker.Lock(Reference, TimeSpan.FromSeconds(1));

        AssertThat(File.Exists(CreatePack(Reference).PackFile)).IsTrue();
    }

    [TestCase]
    public void CreatePackKeepsThePacksOfOtherProjects()
    {
        var otherReference = new GodotProjectReference(Path.Combine(TempDir, "Other", "Game"));
        WriteFile(otherReference.ProjectDir, "project.godot");
        WriteFile(otherReference.ProjectDir, "icon.svg");
        var (otherPack, otherMarker, _) = CreatePack(otherReference);
        File.Delete(otherMarker);

        var packFile = CreatePack(Reference).PackFile;

        AssertThat(packFile).IsNotEqual(otherPack);
        AssertThat(File.Exists(otherPack)).IsTrue();
    }

    #endregion

    #region BackupProjectFiles

    [TestCase]
    public void BackupProjectFilesRestoresTheMigratedProjectFiles()
    {
        WriteFile(Reference.ProjectDir, "Game.csproj", "<Project Sdk=\"Godot.NET.Sdk\"/>");
        WriteFile(Reference.ProjectDir, "project.godot", "config/features=PackedStringArray(\"4.5\")");
        var restore = GodotProjectPacker.BackupProjectFiles(Reference.ProjectDir);
        WriteFile(Reference.ProjectDir, "Game.csproj", "<Project Sdk=\"Godot.NET.Sdk/4.7.0\" />");
        WriteFile(Reference.ProjectDir, "Game.csproj.old", "<Project Sdk=\"Godot.NET.Sdk\"/>");
        WriteFile(Reference.ProjectDir, "project.godot", "config/features=PackedStringArray(\"4.7\")");

        restore();

        AssertThat(File.ReadAllText(Path.Combine(Reference.ProjectDir, "Game.csproj"))).IsEqual("<Project Sdk=\"Godot.NET.Sdk\"/>");
        AssertThat(File.ReadAllText(Path.Combine(Reference.ProjectDir, "project.godot"))).IsEqual("config/features=PackedStringArray(\"4.5\")");
        AssertThat(File.Exists(Path.Combine(Reference.ProjectDir, "Game.csproj.old"))).IsFalse();
    }

    [TestCase]
    public void BackupProjectFilesKeepsUnchangedFilesAndOwnOldFiles()
    {
        WriteFile(Reference.ProjectDir, "Game.csproj.old", "own backup");
        var projectFile = Path.Combine(Reference.ProjectDir, "Game.csproj");
        var modified = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(projectFile, modified);
        var restore = GodotProjectPacker.BackupProjectFiles(Reference.ProjectDir);

        restore();

        AssertThat(File.GetLastWriteTimeUtc(projectFile)).IsEqual(modified);
        AssertThat(File.ReadAllText(Path.Combine(Reference.ProjectDir, "Game.csproj.old"))).IsEqual("own backup");
    }

    #endregion

    #region Lock

    [TestCase]
    public void LockWaitsForAParallelRun()
    {
        using (GodotProjectPacker.Lock(Reference, TimeSpan.FromSeconds(1)))
        {
            AssertThrown(() => GodotProjectPacker.Lock(Reference, TimeSpan.FromMilliseconds(200)))
                .IsInstanceOf<TimeoutException>()
                .HasMessage($"The referenced Godot project '{Reference.ProjectDir}' is prepared by a parallel test run, waiting for it timed out after 200ms.");
        }

        using var projectLock = GodotProjectPacker.Lock(Reference, TimeSpan.FromMilliseconds(200));
        AssertObject(projectLock).IsNotNull();
    }

    [TestCase]
    public void LockDoesNotCreateTheProjectCache()
    {
        var reference = new GodotProjectReference(Path.Combine(TempDir, "NotImported"));
        WriteFile(reference.ProjectDir, "project.godot");

        using var projectLock = GodotProjectPacker.Lock(reference, TimeSpan.FromSeconds(1));

        AssertThat(File.Exists(reference.LockFile)).IsTrue();
        AssertThat(Directory.Exists(Path.Combine(reference.ProjectDir, ".godot"))).IsFalse();
    }

    [TestCase]
    public void LockIsPerProject()
    {
        var otherReference = new GodotProjectReference(Path.Combine(TempDir, "Other", "Game"));
        using var projectLock = GodotProjectPacker.Lock(Reference, TimeSpan.FromSeconds(1));
        using var otherLock = GodotProjectPacker.Lock(otherReference, TimeSpan.FromMilliseconds(200));

        AssertObject(otherLock).IsNotNull();
    }

    #endregion

    #region ImportFingerprint

    [TestCase("icon.svg")]
    [TestCase("icon.svg.import")]
    [TestCase("Characters/Player.cs.uid")]
    [TestCase(".godot/imported/icon.svg-1234.ctex")]
    public void ImportFingerprintChangesWithAnAsset(string entry)
    {
        var fingerprint = ImportFingerprint();

        WriteFile(Reference.ProjectDir, entry, "changed content");

        AssertThat(ImportFingerprint()).IsNotEqual(fingerprint);
    }

    [TestCase]
    public void ImportFingerprintChangesWithANewAsset()
    {
        var fingerprint = ImportFingerprint();

        WriteFile(Reference.ProjectDir, "Items/coin.png");

        AssertThat(ImportFingerprint()).IsNotEqual(fingerprint);
    }

    [TestCase("Characters/Player.cs")]
    [TestCase("Game.csproj")]
    [TestCase(".godot/uid_cache.bin")]
    public void ImportFingerprintIgnoresFilesTheImportDoesNotDependOn(string entry)
    {
        var fingerprint = ImportFingerprint();

        WriteFile(Reference.ProjectDir, entry, "changed content");

        AssertThat(ImportFingerprint()).IsEqual(fingerprint);
    }

    #endregion

    #region IsImportUpToDate

    [TestCase]
    public void IsImportUpToDateAfterWritingTheStamp()
    {
        GodotProjectPacker.WriteImportStamp(Reference, GodotProjectPacker.Scan(Reference.ProjectDir), GodotBinary);

        AssertThat(IsImportUpToDate()).IsTrue();
    }

    [TestCase]
    public void IsImportUpToDateIsSharedByTheTestProjects()
    {
        GodotProjectPacker.WriteImportStamp(Reference, GodotProjectPacker.Scan(Reference.ProjectDir), GodotBinary);

        AssertThat(File.Exists(Path.Combine(Reference.ProjectDir, ".godot", "gdunit4.import.stamp"))).IsTrue();
        AssertThat(GodotProjectPacker.Scan(Reference.ProjectDir).Select(file => file.Entry)).Contains(".godot/gdunit4.import.stamp");
        AssertThat(IsImportUpToDate()).IsTrue();
    }

    [TestCase]
    public void IsImportUpToDateWithoutStamp()
        => AssertThat(IsImportUpToDate()).IsFalse();

    [TestCase]
    public void IsImportUpToDateAfterAnAssetChanged()
    {
        GodotProjectPacker.WriteImportStamp(Reference, GodotProjectPacker.Scan(Reference.ProjectDir), GodotBinary);

        WriteFile(Reference.ProjectDir, "icon.svg", "<svg/>");

        AssertThat(IsImportUpToDate()).IsFalse();
    }

    [TestCase]
    public void IsImportUpToDateAfterImportedDataWentMissing()
    {
        GodotProjectPacker.WriteImportStamp(Reference, GodotProjectPacker.Scan(Reference.ProjectDir), GodotBinary);

        File.Delete(Path.Combine(Reference.ProjectDir, ".godot", "imported", "icon.svg-1234.ctex"));

        AssertThat(IsImportUpToDate()).IsFalse();
    }

    [TestCase]
    public void IsImportUpToDateWhenTheProjectIsNotImported()
    {
        GodotProjectPacker.WriteImportStamp(Reference, GodotProjectPacker.Scan(Reference.ProjectDir), GodotBinary);

        Directory.Delete(Path.Combine(Reference.ProjectDir, ".godot", "imported"), true);

        AssertThat(IsImportUpToDate()).IsFalse();
    }

    [TestCase]
    public void IsImportUpToDateAfterTheGodotBinaryChanged()
    {
        GodotProjectPacker.WriteImportStamp(Reference, GodotProjectPacker.Scan(Reference.ProjectDir), GodotBinary);

        File.WriteAllText(GodotBinary, "4.6");

        AssertThat(IsImportUpToDate()).IsFalse();
    }

    [TestCase]
    public void IsImportUpToDateForAProjectWithoutImportedData()
    {
        Directory.Delete(Path.Combine(Reference.ProjectDir, ".godot", "imported"), true);
        GodotProjectPacker.WriteImportStamp(Reference, GodotProjectPacker.Scan(Reference.ProjectDir), GodotBinary);

        AssertThat(IsImportUpToDate()).IsTrue();
    }

    [TestCase]
    public void IsImportUpToDateForAProjectWithoutAssets()
    {
        File.Delete(Path.Combine(Reference.ProjectDir, "icon.svg.import"));
        Directory.Delete(Path.Combine(Reference.ProjectDir, ".godot", "imported"), true);
        GodotProjectPacker.WriteImportStamp(Reference, GodotProjectPacker.Scan(Reference.ProjectDir), GodotBinary);

        AssertThat(IsImportUpToDate()).IsTrue();
    }

    #endregion

    #region GodotProjectPacks

    [TestCase]
    public void DisposingThePacksReleasesTheirMarkers()
    {
        var (packFile, inUseMarker, _) = CreatePack(Reference);
        var packs = new GodotProjectPacks();
        packs.Add(packFile, inUseMarker);

        AssertThat(packs.Files).ContainsExactly(packFile);

        packs.Dispose();

        AssertThat(File.Exists(inUseMarker)).IsFalse();
        AssertThat(File.Exists(packFile)).IsTrue();
    }

    [TestCase]
    public void DisposingThePacksIgnoresAReadOnlyMarkerOnWindows()
    {
        // On Unix, read-only files can still be unlinked, only Windows blocks this delete.
        if (!OperatingSystem.IsWindows())
            return;

        var (packFile, inUseMarker, _) = CreatePack(Reference);
        var (otherPack, otherMarker, _) = GodotProjectPacker.CreatePack(Reference, Path.Combine(TempDir, "Other.Test"), GodotProjectPacker.Scan(Reference.ProjectDir), TimeSpan.FromSeconds(5));
        var packs = new GodotProjectPacks();
        packs.Add(packFile, inUseMarker);
        packs.Add(otherPack, otherMarker);
        File.SetAttributes(inUseMarker, FileAttributes.ReadOnly);
        try
        {
            packs.Dispose();

            AssertThat(File.Exists(otherMarker)).IsFalse();
        }
        finally
        {
            File.SetAttributes(inUseMarker, FileAttributes.Normal);
        }
    }

    #endregion

    private static string[] PackEntries(string packFile)
    {
        using var pack = ZipFile.OpenRead(packFile);
        return [.. pack.Entries.Select(entry => entry.FullName)];
    }

    private static void WriteFile(string projectDir, string entry, string content = "")
    {
        var path = Path.Combine(projectDir, entry);
        _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    private (string PackFile, string InUseMarker, IReadOnlyList<string> Collisions) CreatePack(GodotProjectReference reference)
        => GodotProjectPacker.CreatePack(reference, TestProjectDir, GodotProjectPacker.Scan(reference.ProjectDir), TimeSpan.FromSeconds(5));

    private string ImportFingerprint() => GodotProjectPacker.ImportFingerprint(GodotProjectPacker.Scan(Reference.ProjectDir));

    private bool IsImportUpToDate() => GodotProjectPacker.IsImportUpToDate(Reference, GodotProjectPacker.Scan(Reference.ProjectDir), GodotBinary);
}

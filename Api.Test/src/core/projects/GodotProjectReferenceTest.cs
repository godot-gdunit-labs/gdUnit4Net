namespace GdUnit4.Tests.Core.Projects;

using System;
using System.IO;
using System.Runtime.Loader;

using GdUnit4.Core.Projects;

using static Assertions;

[TestSuite]
public class GodotProjectReferenceTest
{
    private string TempDir { get; set; } = string.Empty;

    private string TestProjectDir => Path.Combine(TempDir, "Game.Test");

    [BeforeTest]
    public void CreateTempDir()
    {
        TempDir = Path.Combine(Path.GetTempPath(), "gdunit4_" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(TestProjectDir);
    }

    [AfterTest]
    public void DeleteTempDir() => Directory.Delete(TempDir, true);

    [TestCase]
    public void FromAssemblyFile()
    {
        var assemblyPath = Path.Combine(TempDir, "Test.dll");
        ProjectReferenceAssembly.Write(assemblyPath, "../Game/", "..\\Library\\");

        AssertThat(GodotProjectReference.FromAssemblyFile(assemblyPath, TestProjectDir))
            .Extract("ProjectDir")
            .ContainsExactly(Path.Combine(TempDir, "Game"), Path.Combine(TempDir, "Library"));
    }

    [TestCase]
    public void FromAssemblyFileWithoutReferences()
    {
        var assemblyPath = Path.Combine(TempDir, "Test.dll");
        ProjectReferenceAssembly.Write(assemblyPath);

        AssertThat(GodotProjectReference.FromAssemblyFile(assemblyPath, TestProjectDir)).IsEmpty();
    }

    [TestCase]
    public void FromAssemblyFileIgnoresDuplicates()
    {
        var assemblyPath = Path.Combine(TempDir, "Test.dll");
        ProjectReferenceAssembly.Write(assemblyPath, "../Game/", "../Game");

        AssertThat(GodotProjectReference.FromAssemblyFile(assemblyPath, TestProjectDir))
            .Extract("ProjectDir")
            .ContainsExactly(Path.Combine(TempDir, "Game"));
    }

    [TestCase]
    public void FromAssemblyFileIgnoresTheCaseOfDuplicates()
    {
        var assemblyPath = Path.Combine(TempDir, "Test.dll");
        ProjectReferenceAssembly.Write(assemblyPath, "../Game/", "../game");

        AssertThat(GodotProjectReference.FromAssemblyFile(assemblyPath, TestProjectDir))
            .Extract("ProjectDir")
            .ContainsExactly(Path.Combine(TempDir, "Game"));
    }

    [TestCase]
    public void FromAssemblyFileSkipsTheTestProject()
    {
        var assemblyPath = Path.Combine(TempDir, "Test.dll");
        ProjectReferenceAssembly.Write(assemblyPath, string.Empty, "../Game.Test/", "../Game");

        AssertThat(GodotProjectReference.FromAssemblyFile(assemblyPath, TestProjectDir))
            .Extract("ProjectDir")
            .ContainsExactly(Path.Combine(TempDir, "Game"));
    }

    [TestCase]
    public void FromAssembly()
    {
        var assemblyPath = Path.Combine(TempDir, "Test.dll");
        ProjectReferenceAssembly.Write(assemblyPath, "../Game/");
        var loadContext = new AssemblyLoadContext(null, true);
        try
        {
            var assembly = loadContext.LoadFromStream(new MemoryStream(File.ReadAllBytes(assemblyPath)));

            AssertThat(GodotProjectReference.FromAssembly(assembly, TestProjectDir))
                .Extract("ProjectDir")
                .ContainsExactly(Path.Combine(TempDir, "Game"));
        }
        finally
        {
            loadContext.Unload();
        }
    }

    [TestCase]
    public void FromAssemblyWithoutReferences()
        => AssertThat(GodotProjectReference.FromAssembly(typeof(GodotProjectReferenceTest).Assembly, TestProjectDir)).IsEmpty();

    [TestCase]
    public void ResolveAssemblyNameFromSettings()
    {
        File.WriteAllText(Path.Combine(TestProjectDir, "project.godot"), "[application]\nconfig/name=\"Game\"\n\n[dotnet]\n\nproject/assembly_name=\"MyGame\"\n");
        File.WriteAllText(Path.Combine(TestProjectDir, "Game.csproj"), string.Empty);

        AssertThat(GodotProjectReference.ResolveAssemblyName(TestProjectDir)).IsEqual("MyGame");
    }

    [TestCase]
    public void ResolveAssemblyNameIgnoresTheSettingInOtherSections()
    {
        File.WriteAllText(Path.Combine(TestProjectDir, "project.godot"), "[other]\nproject/assembly_name=\"Other\"\n");
        File.WriteAllText(Path.Combine(TestProjectDir, "Game.csproj"), string.Empty);

        AssertThat(GodotProjectReference.ResolveAssemblyName(TestProjectDir)).IsEqual("Game");
    }

    [TestCase]
    public void ResolveAssemblyNameFromProjectFile()
    {
        File.WriteAllText(Path.Combine(TestProjectDir, "Game.csproj"), string.Empty);

        AssertThat(GodotProjectReference.ResolveAssemblyName(TestProjectDir)).IsEqual("Game");
    }

    [TestCase]
    public void ResolveAssemblyNameFailsWithoutSettingAndProjectFile()
        => AssertThrown(() => GodotProjectReference.ResolveAssemblyName(TestProjectDir))
            .IsInstanceOf<InvalidOperationException>()
            .HasMessage($"The assembly name of '{TestProjectDir}' can't be resolved, set 'dotnet/project/assembly_name' in its project settings.");

    [TestCase]
    public void WorkingFilesAreInsideTheTestProjectCache()
    {
        var reference = new GodotProjectReference(Path.Combine(TempDir, "Game"));
        var workingDir = Path.Combine(TestProjectDir, ".godot", "gdunit4");

        AssertThat(reference.Name).IsEqual("Game");
        AssertThat(reference.Id).StartsWith("Game-").HasLength(13);
        AssertThat(reference.PackFile(TestProjectDir, "0123456789ABCDEF0123")).IsEqual(Path.Combine(workingDir, $"{reference.Id}-0123456789ABCDEF.zip"));
        AssertThat(reference.ImportStampFile).IsEqual(Path.Combine(TempDir, "Game", ".godot", "gdunit4.import.stamp"));
        AssertThat(reference.LockFile).IsEqual(Path.Combine(Path.GetTempPath(), $"gdunit4-{Environment.UserName}", $"{reference.Id}.lock"));
    }

    [TestCase]
    public void IdDiffersForProjectsWithTheSameName()
        => AssertThat(new GodotProjectReference(Path.Combine(TempDir, "Game")).Id)
            .IsNotEqual(new GodotProjectReference(Path.Combine(TempDir, "Other", "Game")).Id);
}

// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Projects;

using System.Reflection;
using System.Security.Cryptography;
using System.Text;

using Mono.Cecil;

/// <summary>
///     A Godot project referenced by the test project, whose resources and settings are available at test runtime.
///     The build writes each <c>ProjectReference</c> to a Godot project as assembly metadata into the test assembly,
///     with the path relative to the test project.
/// </summary>
/// <param name="ProjectDir">The full path of the referenced Godot project directory.</param>
internal sealed record GodotProjectReference(string ProjectDir)
{
    internal const string METADATA_KEY = "GdUnit4.GodotProjectReference";

    private const string ASSEMBLY_NAME_SETTING = "project/assembly_name=";

    internal string Name => new DirectoryInfo(ProjectDir).Name;

    // Unique per project directory, two referenced projects can share their folder name.
    internal string Id => $"{Name}-{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ProjectDir.ToUpperInvariant())))[..8]}";

    // Describes the import state of the project cache, shared by all test projects that reference the project.
    internal string ImportStampFile => Path.Combine(ProjectDir, ".godot", "gdunit4.import.stamp");

    // Outside the project, creating it must not create the project cache before the import check.
    // Per user, another user can't write to a shared temp directory created by the first user, e.g. `/tmp` on Linux.
    internal string LockFile => Path.Combine(Path.GetTempPath(), $"gdunit4-{Environment.UserName}", $"{Id}.lock");

    internal static string WorkingDir(string testProjectDir) => Path.Combine(testProjectDir, ".godot", "gdunit4");

    // Named by its content, a pack mounted by a running test process is never rewritten.
    internal string PackFile(string testProjectDir, string contentFingerprint)
        => Path.Combine(WorkingDir(testProjectDir), $"{Id}-{contentFingerprint[..16]}.zip");

    internal static IReadOnlyList<GodotProjectReference> FromAssembly(Assembly assembly, string testProjectDir)
        => Resolve(
            assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .Where(attribute => attribute.Key == METADATA_KEY)
                .Select(attribute => attribute.Value),
            testProjectDir);

    /// <summary>
    ///     Resolves the assembly name of a Godot project: Godot names the assembly by the `dotnet/project/assembly_name` setting,
    ///     its C# project carries the same name.
    /// </summary>
    /// <param name="projectDir">The Godot project directory.</param>
    /// <returns>The assembly name.</returns>
    /// <exception cref="InvalidOperationException">Thrown when neither the setting nor a single C# project names the assembly.</exception>
    internal static string ResolveAssemblyName(string projectDir)
    {
        var projectFile = Path.Combine(projectDir, "project.godot");
        var section = string.Empty;
        foreach (var line in File.Exists(projectFile) ? File.ReadLines(projectFile).Select(line => line.Trim()) : [])
        {
            if (line.StartsWith('[') && line.EndsWith(']'))
                section = line[1..^1];
            else if (section == "dotnet" && line.StartsWith(ASSEMBLY_NAME_SETTING, StringComparison.Ordinal))
                return line[ASSEMBLY_NAME_SETTING.Length..].Trim('"');
        }

        var projectFiles = Directory.Exists(projectDir) ? Directory.GetFiles(projectDir, "*.csproj") : [];
        return projectFiles.Length == 1
            ? Path.GetFileNameWithoutExtension(projectFiles[0])
            : throw new InvalidOperationException($"The assembly name of '{projectDir}' can't be resolved, set 'dotnet/project/assembly_name' in its project settings.");
    }

    // Reads the metadata without loading the assembly, the test host must not lock or load the Godot test assembly.
    internal static IReadOnlyList<GodotProjectReference> FromAssemblyFile(string assemblyPath, string testProjectDir)
    {
        using var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
        return Resolve(
            assembly.CustomAttributes
                .Where(attribute => attribute.AttributeType.FullName == typeof(AssemblyMetadataAttribute).FullName
                                    && attribute.ConstructorArguments.Count == 2
                                    && (attribute.ConstructorArguments[0].Value as string) == METADATA_KEY)
                .Select(attribute => attribute.ConstructorArguments[1].Value as string),
            testProjectDir);
    }

    // A reference to the test project itself is skipped, the test project is never mounted into itself.
    private static List<GodotProjectReference> Resolve(IEnumerable<string?> relativePaths, string testProjectDir)
    {
        var testDir = FullPath(testProjectDir);
        return
        [
            .. relativePaths
                .OfType<string>()
                .Select(path => FullPath(Path.Combine(testProjectDir, path.Replace('\\', '/'))))
                .Where(projectDir => !string.Equals(projectDir, testDir, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(projectDir => new GodotProjectReference(projectDir))
        ];
    }

    private static string FullPath(string path) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
}

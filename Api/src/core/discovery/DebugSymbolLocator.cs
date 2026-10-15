// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Discovery;

using System.Reflection;
using System.Runtime.Loader;

using Godot;

using Mono.Cecil;
using Mono.Cecil.Cil;

/// <summary>
///     Resolves source locations from the debug symbols of an assembly.
///     Reads embedded, portable, and full PDBs; when the symbols are missing or unreadable, no location is returned.
/// </summary>
internal static class DebugSymbolLocator
{
    private const string GODOT_PLUGIN_LOAD_CONTEXT = "GodotPlugins.PluginLoadContext";

    /// <summary>
    ///     Finds the source location of the method body that a compiler-generated state machine type implements,
    ///     e.g. the type the compiler generates for an <c>async</c> iterator method.
    /// </summary>
    /// <param name="stateMachineType">The compiler-generated state machine type.</param>
    /// <returns>The source location of the method body, or <c>null</c> when the debug symbols do not provide it.</returns>
    internal static SourceLocation? FindStateMachineLocation(Type stateMachineType)
    {
        ArgumentNullException.ThrowIfNull(stateMachineType);

        var moduleVersionId = stateMachineType.Module.ModuleVersionId;
        return AssemblyFileCandidates(stateMachineType.Assembly)
            .Select(assemblyPath => FindStateMachineLocation(assemblyPath, stateMachineType.MetadataToken, moduleVersionId))
            .FirstOrDefault(location => location != null);
    }

    /// <summary>
    ///     Finds the source location of the method body that a compiler-generated state machine type implements.
    /// </summary>
    /// <param name="assemblyPath">The path of the assembly that defines the state machine type.</param>
    /// <param name="typeMetadataToken">The metadata token of the state machine type.</param>
    /// <param name="moduleVersionId">The expected module version id, to skip an assembly file that was rebuilt since it was loaded.</param>
    /// <returns>The source location of the method body, or <c>null</c> when the debug symbols do not provide it.</returns>
    internal static SourceLocation? FindStateMachineLocation(string assemblyPath, int typeMetadataToken, Guid? moduleVersionId = null)
    {
        if (string.IsNullOrEmpty(assemblyPath) || !File.Exists(assemblyPath))
            return null;

        try
        {
            var readerParameters = new ReaderParameters
            {
                ReadSymbols = true,
                SymbolReaderProvider = new DefaultSymbolReaderProvider(false),
                ReadingMode = ReadingMode.Deferred
            };
            using var assembly = AssemblyDefinition.ReadAssembly(assemblyPath, readerParameters);
            if (moduleVersionId != null && assembly.MainModule.Mvid != moduleVersionId)
                return null;
            if (assembly.MainModule.LookupToken(typeMetadataToken) is not TypeDefinition stateMachineType)
                return null;

            var sequencePoint = stateMachineType.Methods
                .Where(method => method.Name == "MoveNext" || method.Name.EndsWith(".MoveNext", StringComparison.Ordinal))
                .SelectMany(method => method.DebugInformation.SequencePoints)
                .FirstOrDefault(point => !point.IsHidden);
            return sequencePoint == null ? null : new SourceLocation(sequencePoint.Document.Url, sequencePoint.StartLine);
        }

        // Debug symbol file boundary: Mono.Cecil throws arbitrary exception types for a corrupt symbol file,
        // the debug symbols are optional, a missing, mismatching, or corrupt symbol file only means no location is available
#pragma warning disable CA1031
        catch (Exception)
#pragma warning restore CA1031
        {
            return null;
        }
    }

    private static IEnumerable<string> AssemblyFileCandidates(Assembly assembly)
    {
        if (!string.IsNullOrEmpty(assembly.Location))
            return [assembly.Location];

        // Godot loads the project assemblies from memory, so they have no location, look them up in the Godot build directories instead
        if (AssemblyLoadContext.GetLoadContext(assembly)?.GetType().FullName != GODOT_PLUGIN_LOAD_CONTEXT)
            return [];

        var assemblyFileName = $"{assembly.GetName().Name}.dll";
        var buildDir = Path.Combine(Path.GetFullPath(ProjectSettings.GlobalizePath("res://")), ".godot", "mono", "temp", "bin");
        return new[] { "Debug", "Release", "ExportDebug", "ExportRelease" }
            .Select(configuration => Path.Combine(buildDir, configuration, assemblyFileName));
    }

    /// <summary>
    ///     A location in a source file.
    /// </summary>
    /// <param name="FilePath">The path of the source file.</param>
    /// <param name="Line">The 1-based line number.</param>
    internal sealed record SourceLocation(string FilePath, int Line);
}

// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Discovery;

using System.Collections.Immutable;
using System.Runtime.CompilerServices;

using Api;

using Godot;

using Logging;

using Mono.Cecil;
using Mono.Cecil.Cil;

/// <summary>
///     Discovers test cases in assemblies by scanning for test attributes and analyzing debug information.
/// </summary>
internal static class TestCaseDiscoverer
{
#pragma warning disable CA1859
    private static readonly ITestEngineLogger Logger = new GodotLogger();
#pragma warning restore CA1859

    // Static cache for assembly locations and names
    private static readonly Dictionary<string, ISet<string>> AssemblyLocationCache = [];
    private static string? cachedAssemblyName;

    /// <summary>
    ///     Discovers all test cases in a test assembly.
    /// </summary>
    /// <param name="settings">Test engine settings.</param>
    /// <param name="logger">Logger for discovery process information.</param>
    /// <param name="testAssembly">Path to the test assembly to scan.</param>
    /// <returns>List of discovered test case descriptors.</returns>
#pragma warning disable IDE0060
    public static IReadOnlyList<TestCaseDescriptor> Discover(TestEngineSettings settings, ITestEngineLogger logger, string testAssembly)
#pragma warning restore IDE0060
    {
        logger.LogInfo($"Discover tests from assembly: {testAssembly}");

        AssemblyDefinition? assemblyDefinition = null;
        try
        {
            // Try reading with symbols first
            var readerParameters = new ReaderParameters
            {
                ReadSymbols = true,
                SymbolReaderProvider = new PortablePdbReaderProvider()
            };

            assemblyDefinition = AssemblyDefinition.ReadAssembly(testAssembly, readerParameters);
        }
        catch (Exception ex) when (ex is BadImageFormatException or FileNotFoundException or InvalidOperationException)
        {
            // Fallback: try reading without symbols if PDB reading fails
            try
            {
                var fallbackParameters = new ReaderParameters { ReadSymbols = false };
                assemblyDefinition = AssemblyDefinition.ReadAssembly(testAssembly, fallbackParameters);
            }
            catch (Exception fallbackEx) when (fallbackEx is BadImageFormatException or IOException or UnauthorizedAccessException)
            {
                logger.LogError($"Failed to read assembly {testAssembly}: {fallbackEx.Message}");
                return [];
            }
        }

        using (assemblyDefinition)
        {
            var testSuites = assemblyDefinition.MainModule.Types
                .Where(IsTestSuite)
                .ToImmutableList();

            var testCases = testSuites
                .SelectMany(type => DiscoverTests(logger, testAssembly, type))
                .ToImmutableList();

            logger.LogInfo(
                testCases.Count == 0
                    ? "Discover tests done, no tests found."
                    : $"Discover tests done, {testSuites.Count} TestSuites and total {testCases.Count} Tests found.");

            return testCases;
        }
    }

    public static IReadOnlyList<TestCaseDescriptor> DiscoverTestCasesFromScript(CSharpScript sourceScript)
    {
        ArgumentNullException.ThrowIfNull(sourceScript);
        Logger.LogInfo($"Discover tests for script: {sourceScript.ResourcePath}");

        try
        {
            var fullScriptPath = Path.GetFullPath(ProjectSettings.GlobalizePath(sourceScript.ResourcePath));
            var className = Path.GetFileNameWithoutExtension(fullScriptPath);

            foreach (var assemblyPath in TestAssemblyLocations())
            {
                // Create a custom resolver that can handle GodotSharp
                var resolver = new DefaultAssemblyResolver();
                resolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath));

                // Create reader parameters with the custom resolver
                var readerParameters = new ReaderParameters
                {
                    ReadSymbols = true,
                    SymbolReaderProvider = new PortablePdbReaderProvider(),
                    AssemblyResolver = resolver,
                    ReadingMode = ReadingMode.Deferred // Use deferred loading to avoid resolving references right away
                };

                // lookup for class in the assembly
                using var assembly = AssemblyDefinition.ReadAssembly(assemblyPath, readerParameters);
                var testSuite = assembly?.MainModule.Types
                    .FirstOrDefault(definition => definition.Name == className && HasSourceFilePath(definition, fullScriptPath));

                // No suite found, try the next assembly
                if (assembly == null || testSuite == null)
                    continue;

                return DiscoverTests(null, assembly.MainModule.FileName, testSuite);
            }

            Logger.LogWarning($"Could not find assembly or test suite for {className} at {fullScriptPath}");
            return [];
        }

        // Godot callback boundary: the GDScript editor plugin calls this discovery through the Godot bridge, an exception must not escape into the editor
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            Logger.LogError($"Error discovering tests: {e.Message}\n{e.StackTrace}");
            return [];
        }
    }

    internal static ImmutableList<TestCaseDescriptor> DiscoverTestCasesFromMethod(
        MethodDefinition mi,
        string assemblyPath,
        bool classRequireEngineMode,
        string className,
        ImmutableList<string> classCategories,
        ImmutableDictionary<string, List<string>> classTraits)
    {
        var attributes = mi.CustomAttributes
            .Where(IsAttribute<TestCaseAttribute>)
            .ToImmutableList();
        var hasMultipleAttributes = attributes.Count > 1;
        var requireRunningGodotEngine = classRequireEngineMode || mi.CustomAttributes.Any(IsAttribute<RequireGodotRuntimeAttribute>);
        var methodCategories = GetCategories(mi);
        var methodTraits = GetTraits(mi);
        var allCategories = classCategories.Concat(methodCategories).Distinct().ToImmutableList();
        var allTraits = CombineTraits(classTraits, methodTraits);

        return
        [
            .. attributes
                .Select((attr, index) =>
                {
                    var navData = GetNavigationDataOrDefault(mi);
                    var testCaseAttribute = CreateTestCaseAttribute(attr);
#pragma warning disable IDE0055
                    return new TestCaseDescriptor
                        {
                            Id = Guid.NewGuid(),
                            AssemblyPath = assemblyPath,
                            ManagedType = className,
                            ManagedMethod = mi.Name,
                            AttributeIndex = index,
                            LineNumber = navData.LineNumber,
                            CodeFilePath = navData.CodeFilePath,
                            RequireRunningGodotEngine = requireRunningGodotEngine,
                            Categories = allCategories,
                            Traits = allTraits
                        }
                        .Build(testCaseAttribute, hasMultipleAttributes);
#pragma warning restore IDE0055
                })
                .OrderBy(test => $"{test.ManagedMethod}:{test.AttributeIndex}")
        ];
    }

    private static ImmutableList<TestCaseDescriptor> DiscoverTests(ITestEngineLogger? logger, string testAssembly, TypeDefinition type)
    {
        var requireEngineMode = type.CustomAttributes.Any(IsAttribute<RequireGodotRuntimeAttribute>);
        var typeCategories = GetCategories(type);
        var typeTraits = GetTraits(type);

        var testCases = type.Methods
            .Where(m => m.CustomAttributes.Any(IsAttribute<TestCaseAttribute>))
            .ToList()
            .AsParallel()
            .SelectMany(mi => DiscoverTestCasesFromMethod(mi, testAssembly, requireEngineMode, type.FullName, typeCategories, typeTraits))
            .OrderBy(test => $"{test.ManagedMethod}:{test.AttributeIndex}")
            .ToImmutableList();

        logger?.LogInfo($"Discover:  TestSuite {type.FullName} with {testCases.Count} TestCases found.");
        return testCases;
    }

    private static CodeNavigation GetNavigationDataOrDefault(MethodDefinition definition)
    {
        try
        {
            if (definition.DebugInformation?.SequencePoints == null)
            {
                return new CodeNavigation
                {
                    LineNumber = -1,
                    CodeFilePath = "unknown",
                    MethodName = definition.Name
                };
            }

            var debugInfo = GetDebugInfo(definition);

            return new CodeNavigation
            {
                LineNumber = debugInfo?.StartLine ?? -1,
                CodeFilePath = debugInfo?.Document.Url ?? "unknown",
                MethodName = definition.Name
            };
        }

        // Debug symbol file boundary: Mono.Cecil reads the symbols of the user's test assembly on demand, a stale or corrupt symbol file
        // makes it throw arbitrary exception types, and the source location is optional for the discovered test
#pragma warning disable CA1031
        catch (Exception)
#pragma warning restore CA1031
        {
            return new CodeNavigation
            {
                LineNumber = -1,
                CodeFilePath = "unknown",
                MethodName = definition.Name
            };
        }
    }

    private static SequencePoint? GetDebugInfo(MethodDefinition definition)
    {
        // Check if this is an async method
        var stateMachineAttribute = definition.CustomAttributes.FirstOrDefault(IsAttribute<AsyncStateMachineAttribute>);
        if (stateMachineAttribute?.ConstructorArguments[0].Value is TypeDefinition stateMachineType)
        {
            // Get the MoveNext method which contains the actual implementation
            var moveNextMethod = stateMachineType.Methods.FirstOrDefault(m => m.Name == "MoveNext");
            if (moveNextMethod != null)
            {
                var debugInfo = moveNextMethod.DebugInformation;

                // Try to find the first sequence point that's not compiler-generated
                return debugInfo?.SequencePoints
                    .FirstOrDefault(sp =>

                        // Compiler-generated code often has special column values or hidden lines
                        !sp.IsHidden &&
                        sp.StartColumn > 0 &&
                        sp.Document.Url.EndsWith(".cs"));
            }
        }

        // Default handling for non-async methods
        return definition.DebugInformation?.SequencePoints.FirstOrDefault();
    }

    /// <summary>
    ///     Extracts category information from a type or method definition.
    /// </summary>
    /// <param name="definition">The type or method definition to check for category attributes.</param>
    /// <returns>A list of categories.</returns>
    private static ImmutableList<string> GetCategories(ICustomAttributeProvider definition)
        =>
        [
            .. definition.CustomAttributes
                .Where(IsAttribute<TestCategoryAttribute>)
                .Where(attr => attr.ConstructorArguments.Count > 0 && attr.ConstructorArguments[0].Value is string)
                .Select(attr => (string)attr.ConstructorArguments[0].Value)
        ];

    /// <summary>
    ///     Extracts trait information from a type or method definition.
    /// </summary>
    /// <param name="definition">The type or method definition to check for trait attributes.</param>
    /// <returns>A dictionary of trait names and their values.</returns>
    private static ImmutableDictionary<string, List<string>> GetTraits(ICustomAttributeProvider definition)
        => definition.CustomAttributes
            .Where(IsAttribute<TraitAttribute>)
            .Where(attr => attr.ConstructorArguments.Count >= 2
                           && attr.ConstructorArguments[0].Value is string
                           && attr.ConstructorArguments[1].Value is string)
            .GroupBy(
                attr => (string)attr.ConstructorArguments[0].Value,
                attr => (string)attr.ConstructorArguments[1].Value,
                StringComparer.OrdinalIgnoreCase)
            .ToImmutableDictionary(
                group => group.Key,
                group => group.Distinct().ToList(),
                StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Combines two trait dictionaries.
    /// </summary>
    /// <param name="first">The first dictionary of traits.</param>
    /// <param name="second">The second dictionary of traits.</param>
    /// <returns>A combined dictionary of traits.</returns>
    private static ImmutableDictionary<string, List<string>> CombineTraits(
        ImmutableDictionary<string, List<string>> first,
        ImmutableDictionary<string, List<string>> second)
        => first
            .Concat(second)
            .GroupBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToImmutableDictionary(
                group => group.Key,
                group => group
                    .SelectMany(pair => pair.Value)
                    .Distinct()
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);

    private static TestCaseAttribute CreateTestCaseAttribute(CustomAttribute attribute)
    {
        if (attribute.ConstructorArguments.Count == 0)
            return new TestCaseAttribute();

        var args = Array.Empty<object?>();
        var firstArg = attribute.ConstructorArguments[0];
        if (firstArg.Value is CustomAttributeArgument[] constructorArgs)
        {
            args =
            [
                .. constructorArgs
                    .Select(arg =>
                    {
                        if (arg.Value is CustomAttributeArgument customAttribute)
                            return customAttribute.Value;
                        return arg.Value;
                    })
            ];
        }

        // Create the TestCase attribute instance with constructor arguments
        var testCase = new TestCaseAttribute(args);

        // Handle properties
        foreach (var prop in attribute.Properties)
        {
            switch (prop.Name)
            {
                case "TestName":
                    testCase.TestName = prop.Argument.Value?.ToString();
                    break;
                case "Seed":
                    if (prop.Argument.Value is double seed)
                        testCase.Seed = seed;
                    break;
                case "Iterations":
                    if (prop.Argument.Value is int iterations)
                        testCase.Iterations = iterations;
                    break;
                case "Description":
                    if (prop.Argument.Value is string description)
                        testCase.Description = description;
                    break;

                // ReSharper disable once RedundantEmptySwitchSection
                default:
                    break;
            }
        }

        return testCase;
    }

    private static bool IsAttribute<TAttribute>(CustomAttribute attribute)
        where TAttribute : Attribute
        => string.Equals(attribute.AttributeType.FullName, typeof(TAttribute).FullName, StringComparison.Ordinal);

    private static bool IsTestSuite(TypeDefinition type) =>
        type is { IsClass: true, IsAbstract: false, HasCustomAttributes: true }
        && type.CustomAttributes.Any(attr => attr.AttributeType.Name == "TestSuiteAttribute")
        && type.FullName != null
        && !type.FullName.StartsWith("Microsoft.VisualStudio.TestTools");

    // Initializes and caches potential assembly locations
    private static ISet<string> TestAssemblyLocations()
    {
        lock (AssemblyLocationCache)
        {
            if (AssemblyLocationCache.Count != 0)
            {
                return AssemblyLocationCache.TryGetValue(TestAssemblyName(), out var locations)
                    ? locations
                    : new HashSet<string>();
            }

            var assemblyName = TestAssemblyName();
            var assemblyPaths = FindAllAssemblyPaths(Logger, assemblyName);
            AssemblyLocationCache[assemblyName] = assemblyPaths;

            Logger.LogInfo($"Initialized {assemblyPaths.Count} test assembly locations {string.Join(",", assemblyPaths.ToList())}");
            return assemblyPaths;
        }
    }

    private static HashSet<string> FindAllAssemblyPaths(ITestEngineLogger logger, string assemblyName)
    {
        // Normalize the path
        var projectDir = Path.GetFullPath(ProjectSettings.GlobalizePath("res://"));
        var assemblyDirs = new HashSet<string>
        {
            // Godot 4.x build directories
            Path.Combine(projectDir, ".godot", "mono", "temp", "bin", "Debug"),
            Path.Combine(projectDir, ".godot", "mono", "temp", "bin", "Release"),
            Path.Combine(projectDir, ".godot", "mono", "temp", "bin", "ExportDebug"),
            Path.Combine(projectDir, ".godot", "mono", "temp", "bin", "ExportRelease")
        };

        var assemblyLocations = new HashSet<string>();
        foreach (var dir in assemblyDirs.Where(Directory.Exists))
        {
            // Check for assembly with a project /specified name
            var specificAssembly = Path.Combine(dir, $"{assemblyName}.dll");
            if (!File.Exists(specificAssembly))
                continue;
            _ = assemblyLocations.Add(specificAssembly);
            logger.LogInfo($"Found named assembly: {specificAssembly}");
        }

        return assemblyLocations;
    }

    // Gets the assembly name from project settings with caching
    private static string TestAssemblyName()
    {
        if (cachedAssemblyName != null)
            return cachedAssemblyName;

        var projectName = ProjectSettings.GetSetting("application/config/name").AsString();
        var assemblyName = ProjectSettings.GetSetting("dotnet/project/assembly_name").AsString();

        cachedAssemblyName = string.IsNullOrEmpty(assemblyName) ? projectName : assemblyName;
        return cachedAssemblyName;
    }

    private static bool HasSourceFilePath(TypeDefinition type, string sourceScriptPath)
        => type.Methods.Any(method =>
        {
            var navData = GetNavigationDataOrDefault(method);
            return string.Equals(navData.CodeFilePath, sourceScriptPath, StringComparison.OrdinalIgnoreCase);
        });
}

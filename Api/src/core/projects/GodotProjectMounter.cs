// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Projects;

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;

using Api;

using Godot;
using Godot.Bridge;

/// <summary>
///     Makes the referenced Godot projects available inside the Godot runtime of the test project:
///     mounts their resource pack, registers their uids and scripts, and applies their project settings.
/// </summary>
internal static class GodotProjectMounter
{
    internal const string PACK_ARGUMENT = "--gdunit4-pack";

    private const string DEFAULT_BUS_LAYOUT = "res://default_bus_layout.tres";

    // Sections that identify the test project, configure the test run, or that are applied by a dedicated step.
    private static readonly string[] NotAppliedSections = [string.Empty, "application", "dotnet", "autoload", "input", "editor", "editor_plugins", "gdunit4"];

    // Settings the test run reads from the test project, e.g. the log file is opened at engine start.
    private static readonly string[] NotAppliedKeyPrefixes = ["debug/file_logging/"];

    // Runs for every test run, also without referenced projects, an unexpected error is reported and the tests still run.
    internal static void MountReferencedProjects(Window root, ITestEngineLogger logger)
    {
        try
        {
            MountReferencedProjects(ProjectSettings.GlobalizePath("res://"), root, logger);
        }
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            logger.LogError($"Mounting the referenced Godot projects fails: {e.Message}");
        }
    }

    // The test host prepares the projects the test assembly references, the runtime mounts the same ones.
    // Other assemblies, e.g. a helper library, carry paths relative to their own project.
    internal static Assembly? FindProjectAssembly(string testProjectDir)
    {
        try
        {
            var assemblyName = GodotProjectReference.ResolveAssemblyName(testProjectDir);
            return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == assemblyName);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    // Reading the attributes can fail on any assembly load error, the assembly then references no Godot project.
    internal static IReadOnlyList<GodotProjectReference> ReadReferences(Assembly assembly, string testProjectDir)
    {
        try
        {
            return GodotProjectReference.FromAssembly(assembly, testProjectDir);
        }
#pragma warning disable CA1031
        catch (Exception)
#pragma warning restore CA1031
        {
            return [];
        }
    }

    // The test host packs the referenced projects and passes the packs, a pack is named by its content and differs per run.
    internal static IReadOnlyList<string> PackFiles(string[] args)
    {
        var packFiles = new List<string>();
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (args[index] == PACK_ARGUMENT)
                packFiles.Add(args[++index]);
        }

        return packFiles;
    }

    // Without their files the referenced projects are not mounted at all, the tests then fail on the reported cause.
    internal static bool MountPacks(IReadOnlyList<string> packFiles, ITestEngineLogger logger)
    {
        if (packFiles.Count == 0)
        {
            logger.LogError($"The test project references Godot projects, but the Godot runtime was started without their resource packs ('{PACK_ARGUMENT}').");
            return false;
        }

        var notMounted = packFiles.Where(packFile => !File.Exists(packFile) || !ProjectSettings.LoadResourcePack(packFile, false)).ToList();
        foreach (var packFile in notMounted)
            logger.LogError($"The resource pack '{packFile}' can't be mounted.");
        return notMounted.Count == 0;
    }

    internal static void Mount(GodotProjectReference reference, Assembly projectAssembly, ConfigFile testSettings, Window root, ITestEngineLogger logger)
    {
        _ = RegisterUids(Path.Combine(reference.ProjectDir, ".godot", "uid_cache.bin"), logger);

        using var settings = LoadProjectSettings(reference.ProjectDir);
        _ = RegisterProjectScripts(reference, projectAssembly, logger);
        ApplySettings(settings, testSettings);
        ApplyInputMap(settings, testSettings);
        ApplyAudioBusLayout(settings, testSettings);
        ApplyTranslations(settings);
        ApplyAutoloads(settings, root, logger);

        var extensions = NotLoadedExtensions(reference.ProjectDir, ProjectSettings.GlobalizePath("res://"));
        if (extensions.Count > 0)
        {
            logger.LogWarning(
                $"The GDExtensions of the referenced Godot project '{reference.ProjectDir}' are not loaded, Godot loads only the GDExtensions of the test project at startup:\n"
                + string.Join('\n', extensions.Select(extension => $"  {extension}"))
                + "\nAdd them to the test project to use their classes in tests.");
        }
    }

    // Godot lists the GDExtensions of a project in `.godot/extension_list.cfg`, the test project may load the same ones.
    internal static IReadOnlyList<string> NotLoadedExtensions(string projectDir, string testProjectDir)
    {
        var extensionList = Path.Combine(projectDir, ".godot", "extension_list.cfg");
        if (!File.Exists(extensionList))
            return [];
        var testExtensionList = Path.Combine(testProjectDir, ".godot", "extension_list.cfg");
        var loaded = File.Exists(testExtensionList)
            ? File.ReadLines(testExtensionList).Select(line => line.Trim()).ToHashSet(StringComparer.Ordinal)
            : [];
        return [.. File.ReadLines(extensionList).Select(line => line.Trim()).Where(line => line.Length > 0 && !loaded.Contains(line))];
    }

    // Godot reads `.godot/uid_cache.bin` only at startup, so the uids of the referenced project are added from its cache file.
    // Format of ResourceUID::save_to_cache: uint32 count, then per entry int64 id, int32 length, utf8 path.
    // A cache that can't be read completely, e.g. rewritten by a parallel import, keeps the uids read so far.
    internal static int RegisterUids(string cacheFile, ITestEngineLogger logger)
    {
        if (!File.Exists(cacheFile))
            return 0;

        var registered = 0;
        try
        {
            using var reader = new BinaryReader(File.OpenRead(cacheFile), Encoding.UTF8);
            var count = reader.ReadUInt32();
            for (var i = 0; i < count; i++)
            {
                var id = reader.ReadInt64();
                var length = reader.ReadInt32();
                if (length < 0 || length > reader.BaseStream.Length - reader.BaseStream.Position)
                    throw new EndOfStreamException($"The path of uid {id} has the invalid length {length}.");
                var bytes = reader.ReadBytes(length);
                if (bytes.Length != length)
                    throw new EndOfStreamException($"The path of uid {id} ends after {bytes.Length} of {length} bytes.");
                var path = Encoding.UTF8.GetString(bytes);
                if (ResourceUid.HasId(id))
                    continue;
                ResourceUid.AddId(id, path);
                registered++;
            }
        }
        catch (Exception e) when (e is IOException or ArgumentOutOfRangeException)
        {
            logger.LogError($"The uid cache '{cacheFile}' can't be read completely, uid:// paths of the referenced project may not resolve: {e.Message}");
        }

        return registered;
    }

    internal static ConfigFile LoadProjectSettings(string projectDir)
    {
        var settings = new ConfigFile();
        var error = settings.Load(Path.Combine(projectDir, "project.godot"));
        return error == Error.Ok
            ? settings
            : throw new InvalidOperationException($"The project settings of '{projectDir}' can't be loaded: {error}.");
    }

    // Loaded into the load context of the test assembly, so the referenced types keep their identity in the tests.
    internal static Assembly LoadProjectAssembly(Assembly projectAssembly, string assemblyName)
    {
        var loadContext = AssemblyLoadContext.GetLoadContext(projectAssembly) ?? AssemblyLoadContext.Default;
        return loadContext.LoadFromAssemblyName(new AssemblyName(assemblyName));
    }

    // A referenced project whose assembly can't be loaded still gets its settings, input map and autoloads.
    internal static bool RegisterProjectScripts(GodotProjectReference reference, Assembly projectAssembly, ITestEngineLogger logger)
    {
        try
        {
            return RegisterScripts(LoadProjectAssembly(projectAssembly, GodotProjectReference.ResolveAssemblyName(reference.ProjectDir)), logger);
        }
        catch (Exception e) when (e is InvalidOperationException or IOException or BadImageFormatException)
        {
            logger.LogError(
                $"The assembly of the referenced Godot project '{reference.ProjectDir}' can't be loaded: {e.Message}\n"
                + "Scenes of the referenced Godot project load without their C# classes.");
            return false;
        }
    }

    // Godot registers only the scripts of the test assembly; scenes of the referenced project attach its scripts by path.
    // `LookupScriptsInAssembly` is public, but Godot plans to make it internal. On a Godot version without it, or when a script path
    // of the referenced project is already registered by the test project, the mount continues without these C# classes.
    // A duplicate path stops the lookup, the scripts looked up before it stay registered.
    internal static bool RegisterScripts(Assembly assembly, ITestEngineLogger logger, Action<Assembly>? lookupScripts = null)
    {
        try
        {
            (lookupScripts ?? LookupScriptsInAssembly)(assembly);
            return true;
        }
        catch (Exception e) when (e is MissingMethodException or MethodAccessException or TypeLoadException or ArgumentException)
        {
            logger.LogError(
                $"The scripts of the referenced assembly '{assembly.GetName().Name}' can't be registered completely on Godot {Engine.GetVersionInfo()["string"]}: {e.Message}\n"
                + "Scenes of the referenced Godot project may load without their C# classes.");
            return false;
        }
    }

    // A setting the test project sets itself keeps its value, like a file of the test project hides the file of the referenced project.
    internal static void ApplySettings(ConfigFile settings, ConfigFile testSettings)
    {
        var restartRequiredSettings = ProjectSettings.Singleton.GetPropertyList()
            .Where(property => (((PropertyUsageFlags)property["usage"].AsInt32()) & PropertyUsageFlags.RestartIfChanged) != 0)
            .Select(property => property["name"].AsString())
            .ToHashSet(StringComparer.Ordinal);

        foreach (var section in settings.GetSections().Except(NotAppliedSections, StringComparer.Ordinal))
        {
            foreach (var key in settings.GetSectionKeys(section).Where(key => !testSettings.HasSectionKey(section, key)))
            {
                var name = $"{section}/{key}";
                if (!restartRequiredSettings.Contains(name)
                    && !NotAppliedKeyPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
                    ProjectSettings.SetSetting(name, settings.GetValue(section, key));
            }
        }
    }

    // An action the test project defines itself keeps its events.
    internal static void ApplyInputMap(ConfigFile settings, ConfigFile testSettings)
    {
        if (!settings.HasSection("input"))
            return;

        foreach (var action in settings.GetSectionKeys("input").Where(action => !testSettings.HasSectionKey("input", action)))
        {
            var definition = settings.GetValue("input", action).AsGodotDictionary();
            if (InputMap.HasAction(action))
                InputMap.EraseAction(action);
            InputMap.AddAction(action, definition.TryGetValue("deadzone", out var deadzone) ? deadzone.AsSingle() : 0.2f);
            if (!definition.TryGetValue("events", out var events))
                continue;
            foreach (var inputEvent in events.AsGodotArray<InputEvent>())
                InputMap.ActionAddEvent(action, inputEvent);
        }
    }

    // The bus layout is loaded at engine start from the test project, so the layout of the referenced project is set afterwards,
    // unless the test project has a layout of its own, configured or at the default path.
    internal static void ApplyAudioBusLayout(ConfigFile settings, ConfigFile testSettings)
    {
        if (testSettings.HasSectionKey("audio", "buses/default_bus_layout") || File.Exists(ProjectSettings.GlobalizePath(DEFAULT_BUS_LAYOUT)))
            return;

        var layoutPath = settings.GetValue("audio", "buses/default_bus_layout", DEFAULT_BUS_LAYOUT).AsString();
        if (ResourceLoader.Exists(layoutPath))
        {
            ProjectSettings.SetSetting("audio/buses/default_bus_layout", layoutPath);
            AudioServer.SetBusLayout(GD.Load<AudioBusLayout>(layoutPath));
        }
    }

    // Godot loads the translations at engine start from the test project, the translations of the referenced project are added.
    internal static void ApplyTranslations(ConfigFile settings)
    {
        if (!settings.HasSectionKey("internationalization", "locale/translations"))
            return;

        foreach (var path in settings.GetValue("internationalization", "locale/translations").AsStringArray().Where(path => ResourceLoader.Exists(path)))
            TranslationServer.AddTranslation(GD.Load<Translation>(path));
    }

    // A C# script of the referenced project creates no instance when its scripts are not registered.
    internal static void ApplyAutoloads(ConfigFile settings, Window root, ITestEngineLogger logger)
    {
        if (!settings.HasSection("autoload"))
            return;

        foreach (var name in settings.GetSectionKeys("autoload"))
        {
            if (root.HasNode(name))
                continue;

            // a leading '*' marks the autoload as a global of GDScript
            var path = settings.GetValue("autoload", name).AsString().TrimStart('*');
            var node = (ResourceLoader.Exists(path) ? GD.Load(path) : null) switch
            {
                PackedScene scene => scene.Instantiate(),
                Script script => script.Call("new").AsGodotObject() as Node,
                _ => null
            };
            if (node is null)
            {
                logger.LogError($"The autoload '{name}' can't be created from '{path}'.");
                continue;
            }

            node.Name = name;
            root.AddChild(node);
        }
    }

    private static void MountReferencedProjects(string testProjectDir, Window root, ITestEngineLogger logger)
    {
        using var testSettings = new ConfigFile();
        _ = testSettings.Load("res://project.godot");
        var packFiles = PackFiles(OS.GetCmdlineArgs());
        var projectAssembly = FindProjectAssembly(testProjectDir);
        if (projectAssembly is null)
        {
            if (packFiles.Count > 0)
                logger.LogError($"The resource packs of the referenced Godot projects are not mounted, the assembly of the test project '{testProjectDir}' can't be found.");
            return;
        }

        var references = ReadReferences(projectAssembly, testProjectDir);
        if (references.Count == 0)
            return;

        if (!MountPacks(packFiles, logger))
            return;

        foreach (var reference in references)
        {
            try
            {
                Mount(reference, projectAssembly, testSettings, root, logger);
                logger.LogInfo($"Mounted the referenced Godot project '{reference.ProjectDir}'.");
            }

            // Mount boundary: a broken referenced project is reported, the test run continues with the next project
#pragma warning disable CA1031
            catch (Exception e)
#pragma warning restore CA1031
            {
                logger.LogError($"Mounting the referenced Godot project '{reference.ProjectDir}' fails: {e.Message}");
            }
        }
    }

    // The runtime resolves the call when it compiles the calling method, kept apart so that a missing method is caught by the caller.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LookupScriptsInAssembly(Assembly assembly)
        => ScriptManagerBridge.LookupScriptsInAssembly(assembly);
}

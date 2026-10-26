namespace GdUnit4.Tests.Core.Projects;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

using Api;

using GdUnit4.Core.Projects;
using GdUnit4.Tests.Core.Execution.Monitoring;

using Godot;
using Godot.Collections;

using Moq;
using Moq.Protected;

using static Assertions;

[TestSuite]
[RequireGodotRuntime]
public class GodotProjectMounterTest
{
    private string TempDir { get; set; } = string.Empty;

    [BeforeTest]
    public void CreateTempDir()
    {
        TempDir = Path.Combine(Path.GetTempPath(), "gdunit4_" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(TempDir);
    }

    [AfterTest]
    public void DeleteTempDir() => Directory.Delete(TempDir, true);

    #region ReadReferences

    [TestCase]
    public void ReadReferences()
    {
        var assemblyPath = Path.Combine(TempDir, "Test.dll");
        ProjectReferenceAssembly.Write(assemblyPath, "../Game/");

        AssertThat(ReadReferencesInIsolation(assemblyPath)).ContainsExactly(Path.Combine(TempDir, "Game"));
    }

    [TestCase]
    public void ReadReferencesOfAnAssemblyWithUnresolvableAttributes()
    {
        var assemblyPath = Path.Combine(TempDir, "Test.dll");
        ProjectReferenceAssembly.WriteWithUnresolvableAttribute(assemblyPath, "../Game/");

        AssertThat(ReadReferencesInIsolation(assemblyPath)).IsEmpty();
    }

    #endregion

    #region PackFiles

    [TestCase]
    public void PackFiles()
        => AssertThat(GodotProjectMounter.PackFiles(["--path", ".", "--gdunit4-pack", "C:/a b/Game.zip", "--pipe-name", "x", "--gdunit4-pack", "/tmp/Lib.zip"]))
            .ContainsExactly("C:/a b/Game.zip", "/tmp/Lib.zip");

    [TestCase]
    public void PackFilesWithoutValue()
        => AssertThat(GodotProjectMounter.PackFiles(["--path", ".", "--gdunit4-pack"])).IsEmpty();

    #endregion

    #region MountPacks

    [TestCase]
    public void MountPacksReportsMissingPacks()
    {
        var logger = new Mock<ITestEngineLogger>();

        AssertThat(GodotProjectMounter.MountPacks([], logger.Object)).IsFalse();

        VerifyError(logger, "The test project references Godot projects, but the Godot runtime was started without their resource packs ('--gdunit4-pack').");
    }

    [TestCase]
    public void MountPacksReportsAPackThatCanNotBeMounted()
    {
        var logger = new Mock<ITestEngineLogger>();
        var packFile = Path.Combine(TempDir, "Game.zip");

        AssertThat(GodotProjectMounter.MountPacks([packFile], logger.Object)).IsFalse();

        VerifyError(logger, $"The resource pack '{packFile}' can't be mounted.");
    }

    #endregion

    #region RegisterUids

    [TestCase]
    public void RegisterUids()
    {
        var first = ResourceUid.CreateId();
        var second = ResourceUid.CreateId();
        var cacheFile = WriteUidCache((first, "res://Characters/Player.tscn"), (second, "res://Items/Coin.tscn"));
        try
        {
            AssertThat(GodotProjectMounter.RegisterUids(cacheFile, new Mock<ITestEngineLogger>().Object)).IsEqual(2);
            AssertThat(ResourceUid.GetIdPath(first)).IsEqual("res://Characters/Player.tscn");
            AssertThat(ResourceUid.GetIdPath(second)).IsEqual("res://Items/Coin.tscn");
        }
        finally
        {
            ResourceUid.RemoveId(first);
            ResourceUid.RemoveId(second);
        }
    }

    [TestCase]
    public void RegisterUidsKeepsRegisteredUids()
    {
        var id = ResourceUid.CreateId();
        ResourceUid.AddId(id, "res://test_project_scene.tscn");
        var cacheFile = WriteUidCache((id, "res://Characters/Player.tscn"));
        try
        {
            AssertThat(GodotProjectMounter.RegisterUids(cacheFile, new Mock<ITestEngineLogger>().Object)).IsEqual(0);
            AssertThat(ResourceUid.GetIdPath(id)).IsEqual("res://test_project_scene.tscn");
        }
        finally
        {
            ResourceUid.RemoveId(id);
        }
    }

    [TestCase]
    public void RegisterUidsWithoutCacheFile()
        => AssertThat(GodotProjectMounter.RegisterUids(Path.Combine(TempDir, "uid_cache.bin"), new Mock<ITestEngineLogger>().Object)).IsEqual(0);

    // cut inside the path of the second entry, or inside its path length
    [TestCase(10)]
    [TestCase(25)]
    public void RegisterUidsOfATruncatedCache(int cutBytes)
    {
        var first = ResourceUid.CreateId();
        var cacheFile = WriteUidCache((first, "res://Characters/Player.tscn"), (ResourceUid.CreateId(), "res://Items/Coin.tscn"));
        var bytes = File.ReadAllBytes(cacheFile);
        File.WriteAllBytes(cacheFile, bytes[..^cutBytes]);
        var logger = new Mock<ITestEngineLogger>();
        try
        {
            AssertThat(GodotProjectMounter.RegisterUids(cacheFile, logger.Object)).IsEqual(1);
            AssertThat(ResourceUid.GetIdPath(first)).IsEqual("res://Characters/Player.tscn");
            logger.Protected().Verify(
                "SendMessage",
                Times.Once(),
                ItExpr.Is<LogLevel>(level => level == LogLevel.Error),
                ItExpr.Is<string>(message => message.StartsWith($"The uid cache '{cacheFile}' can't be read completely", StringComparison.Ordinal)));
        }
        finally
        {
            ResourceUid.RemoveId(first);
        }
    }

    [TestCase]
    public void RegisterUidsOfACacheWithAnInvalidPathLength()
    {
        var cacheFile = Path.Combine(TempDir, "uid_cache.bin");
        using (var writer = new BinaryWriter(File.Create(cacheFile)))
        {
            writer.Write(1u);
            writer.Write(ResourceUid.CreateId());
            writer.Write(0x7FFFFFF0);
        }

        var logger = new Mock<ITestEngineLogger>();

        AssertThat(GodotProjectMounter.RegisterUids(cacheFile, logger.Object)).IsEqual(0);
        logger.Protected().Verify(
            "SendMessage",
            Times.Once(),
            ItExpr.Is<LogLevel>(level => level == LogLevel.Error),
            ItExpr.Is<string>(message => message.Contains("has the invalid length 2147483632", StringComparison.Ordinal)));
    }

    #endregion

    #region FindProjectAssembly

    [TestCase]
    public void FindProjectAssembly()
    {
        File.WriteAllText(Path.Combine(TempDir, "project.godot"), "[dotnet]\nproject/assembly_name=\"GdUnit4ApiTest\"\n");

        AssertObject(GodotProjectMounter.FindProjectAssembly(TempDir)).IsSame(typeof(GodotProjectMounterTest).Assembly);
    }

    [TestCase]
    public void FindProjectAssemblyThatIsNotLoaded()
    {
        File.WriteAllText(Path.Combine(TempDir, "project.godot"), "[dotnet]\nproject/assembly_name=\"NotLoaded\"\n");

        AssertObject(GodotProjectMounter.FindProjectAssembly(TempDir)).IsNull();
    }

    [TestCase]
    public void FindProjectAssemblyWithoutAssemblyName()
        => AssertObject(GodotProjectMounter.FindProjectAssembly(TempDir)).IsNull();

    #endregion

    #region LoadProjectAssembly

    [TestCase]
    public void LoadProjectAssemblyKeepsTheTypeIdentity()
    {
        var testAssembly = typeof(GodotProjectMounterTest).Assembly;

        AssertThat(GodotProjectMounter.LoadProjectAssembly(testAssembly, "GdUnit4Api")).IsSame(typeof(GodotProjectMounter).Assembly);
    }

    #endregion

    #region RegisterScripts

    [TestCase]
    public void RegisterScripts()
    {
        var logger = new Mock<ITestEngineLogger>();
        var assembly = typeof(GodotProjectMounterTest).Assembly;
        Assembly? registered = null;

        AssertThat(GodotProjectMounter.RegisterScripts(assembly, logger.Object, lookup => registered = lookup)).IsTrue();
        AssertObject(registered).IsSame(assembly);
        logger.Protected().Verify("SendMessage", Times.Never(), ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<string>());
    }

    [TestCase]
    public void RegisterScriptsReportsAGodotVersionWithoutTheLookup()
    {
        var logger = new Mock<ITestEngineLogger>();

        var registered = GodotProjectMounter.RegisterScripts(
            typeof(GodotProjectMounterTest).Assembly,
            logger.Object,
            _ => throw new MissingMethodException("Godot.Bridge.ScriptManagerBridge", "LookupScriptsInAssembly"));

        AssertThat(registered).IsFalse();
        logger.Protected().Verify(
            "SendMessage",
            Times.Once(),
            ItExpr.Is<LogLevel>(level => level == LogLevel.Error),
            ItExpr.Is<string>(message => message.StartsWith("The scripts of the referenced assembly 'GdUnit4ApiTest' can't be registered completely on Godot ", StringComparison.Ordinal)
                                         && message.EndsWith("Scenes of the referenced Godot project may load without their C# classes.", StringComparison.Ordinal)));
    }

    [TestCase]
    public void RegisterScriptsReportsAScriptPathTheTestProjectRegistered()
    {
        var logger = new Mock<ITestEngineLogger>();

        var registered = GodotProjectMounter.RegisterScripts(
            typeof(GodotProjectMounterTest).Assembly,
            logger.Object,
            _ => throw new ArgumentException("An item with the same key has already been added. Key: res://Autoload/GameState.cs"));

        AssertThat(registered).IsFalse();
        logger.Protected().Verify(
            "SendMessage",
            Times.Once(),
            ItExpr.Is<LogLevel>(level => level == LogLevel.Error),
            ItExpr.Is<string>(message => message.Contains("Key: res://Autoload/GameState.cs", StringComparison.Ordinal)));
    }

    [TestCase]
    public void RegisterProjectScriptsOfAProjectWithoutAssemblyName()
    {
        var logger = new Mock<ITestEngineLogger>();

        AssertThat(GodotProjectMounter.RegisterProjectScripts(new GodotProjectReference(TempDir), typeof(GodotProjectMounterTest).Assembly, logger.Object)).IsFalse();
        logger.Protected().Verify(
            "SendMessage",
            Times.Once(),
            ItExpr.Is<LogLevel>(level => level == LogLevel.Error),
            ItExpr.Is<string>(message => message.StartsWith($"The assembly of the referenced Godot project '{TempDir}' can't be loaded: ", StringComparison.Ordinal)));
    }

    [TestCase]
    public void RegisterProjectScriptsOfAProjectThatIsNotBuilt()
    {
        File.WriteAllText(Path.Combine(TempDir, "project.godot"), "[dotnet]\nproject/assembly_name=\"NotBuilt\"\n");
        var logger = new Mock<ITestEngineLogger>();

        AssertThat(GodotProjectMounter.RegisterProjectScripts(new GodotProjectReference(TempDir), typeof(GodotProjectMounterTest).Assembly, logger.Object)).IsFalse();
        logger.Protected().Verify(
            "SendMessage",
            Times.Once(),
            ItExpr.Is<LogLevel>(level => level == LogLevel.Error),
            ItExpr.Is<string>(message => message.Contains("NotBuilt", StringComparison.Ordinal)
                                         && message.EndsWith("Scenes of the referenced Godot project load without their C# classes.", StringComparison.Ordinal)));
    }

    #endregion

    #region LoadProjectSettings

    [TestCase]
    public void LoadProjectSettingsFailsWithoutProjectFile()
        => AssertThrown(() => GodotProjectMounter.LoadProjectSettings(TempDir))
            .IsInstanceOf<InvalidOperationException>()
            .HasMessage($"The project settings of '{TempDir}' can't be loaded: FileNotFound.");

    #endregion

    #region ApplySettings

    [TestCase]
    public void ApplySettings()
    {
        using var settings = new ConfigFile();
        using var testSettings = new ConfigFile();
        settings.SetValue("gdunit4_test", "rules/start_lives", 3);
        settings.SetValue("layer_names", "2d_physics/layer_31", "gdunit4_test_layer");
        try
        {
            GodotProjectMounter.ApplySettings(settings, testSettings);

            AssertThat(ProjectSettings.GetSetting("gdunit4_test/rules/start_lives").AsInt32()).IsEqual(3);
            AssertThat(ProjectSettings.GetSetting("layer_names/2d_physics/layer_31").AsString()).IsEqual("gdunit4_test_layer");
        }
        finally
        {
            ProjectSettings.Clear("gdunit4_test/rules/start_lives");
            ProjectSettings.SetSetting("layer_names/2d_physics/layer_31", string.Empty);
        }
    }

    [TestCase]
    public void ApplySettingsKeepsTheTestProjectIdentity()
    {
        var name = ProjectSettings.GetSetting("application/config/name").AsString();
        var assemblyName = ProjectSettings.GetSetting("dotnet/project/assembly_name").AsString();
        using var settings = new ConfigFile();
        settings.SetValue("application", "config/name", "Game");
        settings.SetValue("dotnet", "project/assembly_name", "Game");

        GodotProjectMounter.ApplySettings(settings, new ConfigFile());

        AssertThat(ProjectSettings.GetSetting("application/config/name").AsString()).IsEqual(name);
        AssertThat(ProjectSettings.GetSetting("dotnet/project/assembly_name").AsString()).IsEqual(assemblyName);
    }

    [TestCase]
    public void ApplySettingsKeepsTheSettingsOfTheTestProject()
    {
        using var settings = new ConfigFile();
        using var testSettings = new ConfigFile();
        settings.SetValue("gdunit4_test", "rules/start_lives", 3);
        testSettings.SetValue("gdunit4_test", "rules/start_lives", 5);
        ProjectSettings.SetSetting("gdunit4_test/rules/start_lives", 5);
        try
        {
            GodotProjectMounter.ApplySettings(settings, testSettings);

            AssertThat(ProjectSettings.GetSetting("gdunit4_test/rules/start_lives").AsInt32()).IsEqual(5);
        }
        finally
        {
            ProjectSettings.Clear("gdunit4_test/rules/start_lives");
        }
    }

    [TestCase]
    public void ApplySettingsKeepsTheTestRunSettings()
    {
        var logPath = ProjectSettings.GetSetting("debug/file_logging/log_path").AsString();
        var reportCount = ProjectSettings.GetSetting("gdunit4/report/report_count");
        using var settings = new ConfigFile();
        settings.SetValue("debug", "file_logging/log_path", "user://logs/game.log");
        settings.SetValue("gdunit4", "report/report_count", 1);

        GodotProjectMounter.ApplySettings(settings, new ConfigFile());

        AssertThat(ProjectSettings.GetSetting("debug/file_logging/log_path").AsString()).IsEqual(logPath);
        AssertThat(ProjectSettings.GetSetting("gdunit4/report/report_count").VariantType).IsEqual(reportCount.VariantType);
    }

    [TestCase]
    public void ApplySettingsKeepsSettingsThatRequireRestart()
    {
        const string settingName = "gdunit4_test/restart_required";
        ProjectSettings.SetSetting(settingName, 5);
        ProjectSettings.SetRestartIfChanged(settingName, true);
        using var settings = new ConfigFile();
        using var testSettings = new ConfigFile();
        settings.SetValue("gdunit4_test", "restart_required", 3);
        try
        {
            GodotProjectMounter.ApplySettings(settings, testSettings);

            AssertThat(ProjectSettings.GetSetting(settingName).AsInt32()).IsEqual(5);
        }
        finally
        {
            ProjectSettings.SetRestartIfChanged(settingName, false);
            ProjectSettings.Clear(settingName);
        }
    }

    #endregion

    #region ApplyAudioBusLayout

    [TestCase]
    public void ApplyAudioBusLayoutOfTheReferencedProject()
    {
        var originalLayout = AudioServer.GenerateBusLayout();
        var hadLayoutSetting = ProjectSettings.HasSetting("audio/buses/default_bus_layout");
        var originalLayoutSetting = ProjectSettings.GetSetting("audio/buses/default_bus_layout");
        var layoutPath = Path.Combine(TempDir, "game_bus_layout.tres").Replace('\\', '/');
        AudioServer.AddBus();
        AudioServer.SetBusName(AudioServer.BusCount - 1, "GdUnit4Music");
        _ = ResourceSaver.Save(AudioServer.GenerateBusLayout(), layoutPath);
        AudioServer.SetBusLayout(originalLayout);
        using var settings = new ConfigFile();
        settings.SetValue("audio", "buses/default_bus_layout", layoutPath);
        try
        {
            GodotProjectMounter.ApplyAudioBusLayout(settings, new ConfigFile());

            AssertThat(AudioServer.GetBusIndex("GdUnit4Music")).IsNotEqual(-1);
            AssertThat(ProjectSettings.GetSetting("audio/buses/default_bus_layout").AsString()).IsEqual(layoutPath);
        }
        finally
        {
            AudioServer.SetBusLayout(originalLayout);
            if (hadLayoutSetting)
                ProjectSettings.SetSetting("audio/buses/default_bus_layout", originalLayoutSetting);
            else
                ProjectSettings.Clear("audio/buses/default_bus_layout");
        }
    }

    [TestCase]
    public void ApplyAudioBusLayoutKeepsTheLayoutOfTheTestProject()
    {
        var originalLayout = AudioServer.GenerateBusLayout();
        var busCount = AudioServer.BusCount;
        var layoutPath = Path.Combine(TempDir, "game_bus_layout.tres").Replace('\\', '/');
        AudioServer.AddBus();
        _ = ResourceSaver.Save(AudioServer.GenerateBusLayout(), layoutPath);
        AudioServer.SetBusLayout(originalLayout);
        using var settings = new ConfigFile();
        using var testSettings = new ConfigFile();
        settings.SetValue("audio", "buses/default_bus_layout", layoutPath);
        testSettings.SetValue("audio", "buses/default_bus_layout", "res://test_bus_layout.tres");
        try
        {
            GodotProjectMounter.ApplyAudioBusLayout(settings, testSettings);

            AssertThat(AudioServer.BusCount).IsEqual(busCount);
        }
        finally
        {
            AudioServer.SetBusLayout(originalLayout);
        }
    }

    [TestCase]
    public void ApplyAudioBusLayoutKeepsTheLayoutAtTheDefaultPathOfTheTestProject()
    {
        var originalLayout = AudioServer.GenerateBusLayout();
        var busCount = AudioServer.BusCount;
        var testLayout = ProjectSettings.GlobalizePath("res://default_bus_layout.tres");
        var layoutPath = Path.Combine(TempDir, "game_bus_layout.tres").Replace('\\', '/');
        AudioServer.AddBus();
        _ = ResourceSaver.Save(AudioServer.GenerateBusLayout(), layoutPath);
        AudioServer.SetBusLayout(originalLayout);
        File.WriteAllText(testLayout, string.Empty);
        using var settings = new ConfigFile();
        settings.SetValue("audio", "buses/default_bus_layout", layoutPath);
        try
        {
            GodotProjectMounter.ApplyAudioBusLayout(settings, new ConfigFile());

            AssertThat(AudioServer.BusCount).IsEqual(busCount);
        }
        finally
        {
            File.Delete(testLayout);
            AudioServer.SetBusLayout(originalLayout);
        }
    }

    #endregion

    #region ApplyTranslations

    [TestCase]
    public void ApplyTranslations()
    {
        var translationPath = Path.Combine(TempDir, "game.en.translation.tres").Replace('\\', '/');
        var translation = new Translation { Locale = TranslationServer.GetLocale() };
        translation.AddMessage("GDUNIT4_GAME_TITLE", "My Game");
        _ = ResourceSaver.Save(translation, translationPath);
        using var settings = new ConfigFile();
        settings.SetValue("internationalization", "locale/translations", new[] { translationPath });
        try
        {
            GodotProjectMounter.ApplyTranslations(settings);

            AssertThat(TranslationServer.Translate("GDUNIT4_GAME_TITLE").ToString()).IsEqual("My Game");
        }
        finally
        {
            TranslationServer.RemoveTranslation(GD.Load<Translation>(translationPath));
        }
    }

    #endregion

    #region ApplyInputMap

    [TestCase]
    public void ApplyInputMap()
    {
        using var settings = new ConfigFile();
        settings.SetValue("input", "gdunit4_jump", new Dictionary
        {
            { "deadzone", 0.3f },
            { "events", new Array<InputEvent> { new InputEventKey { PhysicalKeycode = Key.Space } } }
        });
        try
        {
            GodotProjectMounter.ApplyInputMap(settings, new ConfigFile());

            AssertThat(InputMap.HasAction("gdunit4_jump")).IsTrue();
            AssertThat(InputMap.ActionGetDeadzone("gdunit4_jump")).IsEqual(0.3f);
            AssertThat(InputMap.ActionGetEvents("gdunit4_jump").OfType<InputEventKey>().Select(key => key.PhysicalKeycode)).ContainsExactly(Key.Space);
        }
        finally
        {
            InputMap.EraseAction("gdunit4_jump");
        }
    }

    [TestCase]
    public void ApplyInputMapReplacesAnExistingAction()
    {
        InputMap.AddAction("gdunit4_jump");
        InputMap.ActionAddEvent("gdunit4_jump", new InputEventKey { PhysicalKeycode = Key.Enter });
        using var settings = new ConfigFile();
        settings.SetValue("input", "gdunit4_jump", new Dictionary
        {
            { "deadzone", 0.2f },
            { "events", new Array<InputEvent> { new InputEventKey { PhysicalKeycode = Key.Space } } }
        });
        try
        {
            GodotProjectMounter.ApplyInputMap(settings, new ConfigFile());

            AssertThat(InputMap.ActionGetEvents("gdunit4_jump").OfType<InputEventKey>().Select(key => key.PhysicalKeycode)).ContainsExactly(Key.Space);
        }
        finally
        {
            InputMap.EraseAction("gdunit4_jump");
        }
    }

    [TestCase]
    public void ApplyInputMapKeepsTheActionsOfTheTestProject()
    {
        InputMap.AddAction("gdunit4_jump");
        InputMap.ActionAddEvent("gdunit4_jump", new InputEventKey { PhysicalKeycode = Key.Enter });
        using var settings = new ConfigFile();
        using var testSettings = new ConfigFile();
        var gameAction = new Dictionary
        {
            { "deadzone", 0.2f },
            { "events", new Array<InputEvent> { new InputEventKey { PhysicalKeycode = Key.Space } } }
        };
        settings.SetValue("input", "gdunit4_jump", gameAction);
        testSettings.SetValue("input", "gdunit4_jump", new Dictionary());
        try
        {
            GodotProjectMounter.ApplyInputMap(settings, testSettings);

            AssertThat(InputMap.ActionGetEvents("gdunit4_jump").OfType<InputEventKey>().Select(key => key.PhysicalKeycode)).ContainsExactly(Key.Enter);
        }
        finally
        {
            InputMap.EraseAction("gdunit4_jump");
        }
    }

    #endregion

    #region NotLoadedExtensions

    [TestCase]
    public void NotLoadedExtensionsWithoutExtensionList()
        => AssertThat(GodotProjectMounter.NotLoadedExtensions(Path.Combine(TempDir, "Game"), Path.Combine(TempDir, "Game.Test"))).IsEmpty();

    [TestCase]
    public void NotLoadedExtensionsSkipsTheExtensionsOfTheTestProject()
    {
        var gameCache = Path.Combine(TempDir, "Game", ".godot");
        var testCache = Path.Combine(TempDir, "Game.Test", ".godot");
        _ = Directory.CreateDirectory(gameCache);
        _ = Directory.CreateDirectory(testCache);
        File.WriteAllText(Path.Combine(gameCache, "extension_list.cfg"), "res://addons/terrain/terrain.gdextension\nres://addons/audio/audio.gdextension\n\n");
        File.WriteAllText(Path.Combine(testCache, "extension_list.cfg"), "res://addons/audio/audio.gdextension\n");

        AssertThat(GodotProjectMounter.NotLoadedExtensions(Path.Combine(TempDir, "Game"), Path.Combine(TempDir, "Game.Test")))
            .ContainsExactly("res://addons/terrain/terrain.gdextension");
    }

    #endregion

    #region ApplyAutoloads

    [TestCase]
    public void ApplyAutoloads()
    {
        var root = ((SceneTree)Engine.GetMainLoop()).Root;
        using var settings = new ConfigFile();
        settings.SetValue("autoload", "GdUnit4EventBus", "*res://src/core/execution/monitoring/ExampleEventBus.cs");
        settings.SetValue("autoload", "GdUnit4Scene", "res://src/core/resources/scenes/SimpleScene.tscn");
        try
        {
            GodotProjectMounter.ApplyAutoloads(settings, root, new Mock<ITestEngineLogger>().Object);

            AssertObject(root.GetNodeOrNull("GdUnit4EventBus")).IsInstanceOf<ExampleEventBus>();
            AssertObject(root.GetNodeOrNull("GdUnit4Scene")).IsNotNull();
        }
        finally
        {
            root.GetNodeOrNull("GdUnit4EventBus")?.Free();
            root.GetNodeOrNull("GdUnit4Scene")?.Free();
        }
    }

    [TestCase]
    public void ApplyAutoloadsKeepsExistingNodes()
    {
        var root = ((SceneTree)Engine.GetMainLoop()).Root;
        var existing = new Node { Name = "GdUnit4EventBus" };
        root.AddChild(existing);
        using var settings = new ConfigFile();
        settings.SetValue("autoload", "GdUnit4EventBus", "*res://src/core/execution/monitoring/ExampleEventBus.cs");
        try
        {
            GodotProjectMounter.ApplyAutoloads(settings, root, new Mock<ITestEngineLogger>().Object);

            AssertObject(root.GetNodeOrNull("GdUnit4EventBus")).IsSame(existing);
        }
        finally
        {
            existing.Free();
        }
    }

    [TestCase]
    public void ApplyAutoloadsReportsAnAutoloadThatCreatesNoNode()
    {
        var root = ((SceneTree)Engine.GetMainLoop()).Root;
        var scriptPath = Path.Combine(TempDir, "not_a_node.gd").Replace('\\', '/');
        File.WriteAllText(scriptPath, "extends RefCounted\n");
        var logger = new Mock<ITestEngineLogger>();
        using var settings = new ConfigFile();
        settings.SetValue("autoload", "GdUnit4NotANode", $"*{scriptPath}");
        settings.SetValue("autoload", "GdUnit4Scene", "res://src/core/resources/scenes/SimpleScene.tscn");
        try
        {
            GodotProjectMounter.ApplyAutoloads(settings, root, logger.Object);

            AssertObject(root.GetNodeOrNull("GdUnit4NotANode")).IsNull();
            AssertObject(root.GetNodeOrNull("GdUnit4Scene")).IsNotNull();
            VerifyError(logger, $"The autoload 'GdUnit4NotANode' can't be created from '{scriptPath}'.");
        }
        finally
        {
            root.GetNodeOrNull("GdUnit4Scene")?.Free();
        }
    }

    [TestCase]
    public void ApplyAutoloadsReportsAnUnsupportedResource()
    {
        var root = ((SceneTree)Engine.GetMainLoop()).Root;
        var logger = new Mock<ITestEngineLogger>();
        using var settings = new ConfigFile();
        settings.SetValue("autoload", "GdUnit4Missing", "res://not_existing.tscn");

        GodotProjectMounter.ApplyAutoloads(settings, root, logger.Object);

        AssertObject(root.GetNodeOrNull("GdUnit4Missing")).IsNull();
        VerifyError(logger, "The autoload 'GdUnit4Missing' can't be created from 'res://not_existing.tscn'.");
    }

    #endregion

    private static void VerifyError(Mock<ITestEngineLogger> logger, string message)
        => logger.Protected().Verify("SendMessage", Times.Once(), ItExpr.Is<LogLevel>(level => level == LogLevel.Error), ItExpr.Is<string>(text => text == message));

    private string[] ReadReferencesInIsolation(string assemblyPath)
    {
        var loadContext = new AssemblyLoadContext(null, true);
        try
        {
            var assembly = loadContext.LoadFromStream(new MemoryStream(File.ReadAllBytes(assemblyPath)));
            return [.. GodotProjectMounter.ReadReferences(assembly, Path.Combine(TempDir, "Game.Test")).Select(reference => reference.ProjectDir)];
        }
        finally
        {
            loadContext.Unload();
        }
    }

    private string WriteUidCache(params (long Id, string Path)[] entries)
    {
        var cacheFile = Path.Combine(TempDir, "uid_cache.bin");
        using var writer = new BinaryWriter(File.Create(cacheFile), Encoding.UTF8);
        writer.Write((uint)entries.Length);
        foreach (var (id, path) in entries)
        {
            var bytes = Encoding.UTF8.GetBytes(path);
            writer.Write(id);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        return cacheFile;
    }
}

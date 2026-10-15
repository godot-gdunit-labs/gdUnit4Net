namespace GdUnit4.TestAdapter.Test.Settings;

using System;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using TestAdapter.Settings;

using static TestAdapter.Utilities.Utils;
using static TestUtils;

[TestClass]
public class GodotProjectSettingsTest
{
    [TestMethod]
    public void GlobalizeGodotPath()
    {
        // user specific path
        Assert.AreEqual(
            Path.Combine(GetUserDataDirectory, "app_userdata", "gdUnit4Test", "logs", "godot.log"),
            GodotProjectSettings.GlobalizeGodotPath("user://logs/godot.log", "gdUnit4Test"));

        // project specific path
        Assert.AreEqual(
            Path.Combine(GetProjectDirectory, "logs", "godot.log"),
            GodotProjectSettings.GlobalizeGodotPath("res://logs/godot.log", "gdUnit4Test"));
    }

    [TestMethod]
    public void LoadGodotProjectSettingsWithoutDebugSection()
    {
        var projectFile = GetResourcePath("project.godot");
        var settings = GodotProjectSettings.LoadFromFile(projectFile);
        Assert.IsNotNull(settings);

        Assert.AreEqual("gdUnit4Test", settings.Application.Config.Name);
        CollectionAssert.Contains(settings.Application.Config.Features, "4.3");
        CollectionAssert.Contains(settings.Application.Config.Features, "C#");
        Assert.AreEqual("res://icon.svg", settings.Application.Config.Icon);

        // validate the log file path points to the default user dir
        Assert.IsNotNull(settings.Debug);
        var expectedLogFilePath = GodotProjectSettings.GlobalizeGodotPath("user://logs/godot.log", "gdUnit4Test");
        Assert.AreEqual(expectedLogFilePath, settings.Debug.FileLogging.LogPath);
    }

    [TestMethod]
    public void LoadGodotProjectSettingsWithCustomLogPath()
    {
        var projectFile = GetResourcePath("project.godot2");
        var settings = GodotProjectSettings.LoadFromFile(projectFile);
        Assert.IsNotNull(settings);

        Assert.AreEqual("gdUnit4", settings.Application.Config.Name);
        CollectionAssert.Contains(settings.Application.Config.Features, "4.3");
        CollectionAssert.Contains(settings.Application.Config.Features, "C#");
        Assert.AreEqual("res://icon.png", settings.Application.Config.Icon);

        // validate the log file path points to project root
        Assert.IsNotNull(settings.Debug);
        var expectedLogFilePath = Path.Combine(GetProjectDirectory, "godot_session.log");
        Assert.AreEqual(expectedLogFilePath, settings.Debug.FileLogging.LogPath);
    }

    [TestMethod]
    public void LoadGodotProjectSettingsSkipsValuesNotMatchingThePropertyType()
    {
        var projectFile = Path.Combine(Path.GetTempPath(), $"project_{Guid.NewGuid():N}.godot");
        File.WriteAllText(
            projectFile,
            """
            [application]
            config/name=PackedStringArray("not", "a name")
            config/icon="res://icon.svg"

            [debug]
            file_logging/enable_file_logging="maybe"
            """);
        try
        {
            var settings = GodotProjectSettings.LoadFromFile(projectFile);

            // an array does not convert to the string of the name (InvalidCastException), the default is kept
            Assert.AreEqual(string.Empty, settings.Application.Config.Name);
            Assert.AreEqual("res://icon.svg", settings.Application.Config.Icon);

            // 'maybe' does not convert to a bool (FormatException), the default is kept
            Assert.IsFalse(settings.Debug.FileLogging.EnableFileLogging);
        }
        finally
        {
            File.Delete(projectFile);
        }
    }
}

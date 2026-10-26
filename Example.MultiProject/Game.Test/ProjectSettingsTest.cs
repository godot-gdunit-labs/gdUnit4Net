namespace Game.Test;

using Autoload;

using GdUnit4;

using Godot;

using static GdUnit4.Assertions;

/// <summary>
///     Game project settings from the game project.godot, applied at test runtime.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class ProjectSettingsTest
{
    [TestCase]
    public void InputActionsAreAvailable()
    {
        AssertThat(InputMap.HasAction("move_left")).IsTrue();
        AssertThat(InputMap.HasAction("move_right")).IsTrue();
        AssertThat(InputMap.HasAction("jump")).IsTrue();
    }

    [TestCase]
    public void InputActionEventsAreMapped()
    {
        var keys = InputMap.ActionGetEvents("jump").OfType<InputEventKey>().Select(e => e.PhysicalKeycode).ToList();
        AssertThat(keys).ContainsExactly(Key.Space);
        AssertThat(InputMap.ActionGetDeadzone("jump")).IsEqual(0.2f);
    }

    [TestCase]
    public void CustomSettingsAreAvailable()
    {
        AssertThat(ProjectSettings.GetSetting("game/rules/start_lives").AsInt32()).IsEqual(3);
        AssertThat(ProjectSettings.GetSetting("game/rules/difficulty").AsString()).IsEqual("hard");
    }

    [TestCase]
    public void LayerNamesAreAvailable()
    {
        AssertThat(ProjectSettings.GetSetting("layer_names/2d_physics/layer_1").AsString()).IsEqual("player");
        AssertThat(ProjectSettings.GetSetting("layer_names/2d_physics/layer_2").AsString()).IsEqual("coins");
    }

    [TestCase]
    public void TestProjectIdentityIsKept()
    {
        AssertThat(ProjectSettings.GetSetting("application/config/name").AsString()).IsEqual("Game.Test");
        AssertThat(ProjectSettings.GetSetting("dotnet/project/assembly_name").AsString()).IsEqual("Game.Test");
    }

    [TestCase]
    public void AudioBusLayoutIsApplied()
    {
        var musicBus = AudioServer.GetBusIndex("Music");
        AssertThat(musicBus).IsGreater(0);
        AssertThat(AudioServer.GetBusVolumeDb(musicBus)).IsEqual(-6f);
    }

    [TestCase]
    public void AutoloadIsAvailable()
    {
        var root = ((SceneTree)Engine.GetMainLoop()).Root;
        AssertObject(root.GetNodeOrNull("GameState")).IsInstanceOf<GameState>();
    }
}

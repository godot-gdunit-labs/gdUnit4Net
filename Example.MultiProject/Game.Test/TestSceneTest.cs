namespace Game.Test;

using Autoload;
using Characters;
using GdUnit4;
using Godot;
using Levels;
using Scenes;
using static GdUnit4.Assertions;

/// <summary>
///     Running test-owned scenes that use game scenes as sub-scenes.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class TestSceneTest
{
    [BeforeTest]
    public void ResetGameState()
        => ((SceneTree)Engine.GetMainLoop()).Root.GetNode<GameState>("GameState").Reset();

    [TestCase]
    public void TestSceneWithGameSubScene()
    {
        var harness = ISceneRunner.Load("res://Scenes/PlayerTestScene.tscn", true).Scene() as PlayerTestHarness;
        AssertObject(harness).IsNotNull();
        AssertObject(harness!.Player).IsInstanceOf<Player>();
    }

    [TestCase]
    public void TestSceneOverridesGameResourceWithTestResource()
    {
        var harness = (PlayerTestHarness)ISceneRunner.Load("res://Scenes/PlayerTestScene.tscn", true).Scene()!;
        AssertThat(harness.Player.Stats.Speed).IsEqual(1000f);
    }

    [TestCase]
    public async Task TestScriptReceivesGameSignals()
    {
        var runner = ISceneRunner.Load("res://Scenes/PlayerTestScene.tscn", true);
        runner.SimulateActionPressed("jump");
        await runner.AwaitInputProcessed();
        AssertThat(((PlayerTestHarness)runner.Scene()!).JumpsSeen).IsEqual(1);
    }

    [TestCase]
    public void TestSceneExtendsNestedGameScenes()
    {
        var scene = ISceneRunner.Load("res://Scenes/LevelTestScene.tscn", true).Scene()!;
        var level = scene.GetNode<Level>("Level");
        AssertThat(level.Coins.Select(coin => coin.Value)).ContainsExactly(10, 25, 50);

        level.CollectAll();
        AssertThat(scene.GetNode<GameState>("/root/GameState").Score).IsEqual(85);
    }

    [TestCase]
    public void TestCodeInstantiatesGameScene()
    {
        var player = AutoFree(GD.Load<PackedScene>("res://Characters/Player.tscn").Instantiate<Player>());
        AssertThat(player.Stats.Speed).IsEqual(200f);
    }
}

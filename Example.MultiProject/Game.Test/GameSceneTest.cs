namespace Game.Test;

using Autoload;
using Characters;
using GdUnit4;
using Godot;
using Items;
using Levels;
using static GdUnit4.Assertions;

/// <summary>
///     Running game scenes: plain, with game resources, with game sub-scenes, with settings, input and autoloads.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class GameSceneTest
{
    [BeforeTest]
    public void ResetGameState()
        => ((SceneTree)Engine.GetMainLoop()).Root.GetNode<GameState>("GameState").Reset();

    [TestCase]
    public void LoadGameSceneByPath()
    {
        var coin = ISceneRunner.Load<Coin>("res://Items/Coin.tscn", true).Scene();
        AssertThat(coin.Value).IsEqual(10);
    }

    [TestCase]
    public void LoadGameSceneByUid()
        => AssertObject(ISceneRunner.Load("uid://bgplayer01", true).Scene()).IsInstanceOf<Player>();

    [TestCase]
    public void GameSceneUsesGameResources()
    {
        var player = ISceneRunner.Load<Player>("res://Characters/Player.tscn", true).Scene();
        AssertThat(player.Stats.Speed).IsEqual(200f);
        AssertObject(player.GetNode<Sprite2D>("Sprite").Texture).IsNotNull();
    }

    [TestCase]
    public void GameSceneReadsProjectSetting()
    {
        var player = ISceneRunner.Load<Player>("res://Characters/Player.tscn", true).Scene();
        AssertThat(player.Lives).IsEqual(3);
    }

    #region documented examples
    // The following test verifies Api/doc/multi-project.md.
    [TestCase]
    public async Task GameSceneReactsOnInputAction()
    {
        var runner = ISceneRunner.Load<Player>("res://Characters/Player.tscn", true);
        runner.SimulateActionPressed("jump");
        await runner.AwaitInputProcessed();
        AssertThat(runner.Scene().JumpCount).IsEqual(1);
    }
    #endregion

    [TestCase]
    public async Task GameSceneReactsOnKeyMappedByGameInputMap()
    {
        var runner = ISceneRunner.Load<Player>("res://Characters/Player.tscn", true);
        runner.SimulateKeyPressed(Key.Space);
        await runner.AwaitInputProcessed();
        AssertThat(runner.Scene().JumpCount).IsEqual(1);
    }

    [TestCase]
    public async Task GameSceneMovesOnHeldAction()
    {
        var runner = ISceneRunner.Load<Player>("res://Characters/Player.tscn", true);
        var player = runner.Scene();
        var start = player.Position.X;
        runner.SimulateActionPress("move_right");
        await runner.SimulateFrames(10);
        runner.SimulateActionRelease("move_right");
        AssertThat(player.Position.X).IsGreater(start);
    }

    [TestCase]
    public void GameSceneWithGameSubScenes()
    {
        var level = ISceneRunner.Load<Level>("res://Levels/Level.tscn", true).Scene();
        AssertThat(level.Player.Stats.Speed).IsEqual(200f);
        AssertThat(level.Coins.Select(coin => coin.Value)).ContainsExactly(10, 25);
    }

    [TestCase]
    public void GameSceneUsesAutoload()
    {
        var level = ISceneRunner.Load<Level>("res://Levels/Level.tscn", true).Scene();
        level.CollectAll();
        AssertThat(level.GetNode<GameState>("/root/GameState").Score).IsEqual(35);
    }
}

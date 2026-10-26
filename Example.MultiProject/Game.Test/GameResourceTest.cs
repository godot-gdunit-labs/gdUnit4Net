namespace Game.Test;

using Characters;
using GdUnit4;
using Godot;
using Items;
using static GdUnit4.Assertions;

/// <summary>
///     Loading game resources, with and without game scripts, by path and by uid.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class GameResourceTest
{
    [TestCase]
    public void UseGameTypeInCode()
        => AssertThat(new PlayerStats().Speed).IsEqual(100f);

    [TestCase]
    public void LoadGameResourceWithGameScriptByPath()
    {
        var stats = GD.Load<PlayerStats>("res://Characters/player_stats.tres");
        AssertThat(stats.Speed).IsEqual(200f);
        AssertThat(stats.JumpVelocity).IsEqual(450f);
    }

    [TestCase]
    public void LoadGameResourceWithGameScriptByUid()
        => AssertThat(GD.Load<PlayerStats>("uid://bgstats001").Speed).IsEqual(200f);

    [TestCase]
    public void LoadGameResourceWithoutScript()
        => AssertObject(GD.Load("res://default_bus_layout.tres")).IsInstanceOf<AudioBusLayout>();

    [TestCase]
    public void LoadImportedGameAsset()
    {
        var texture = GD.Load<Texture2D>("res://icon.svg");
        AssertThat(texture.GetSize()).IsEqual(new Vector2(64, 64));
    }

    [TestCase]
    public void LoadGameScript()
    {
        var script = GD.Load<CSharpScript>("res://Items/Coin.cs");
        var coin = AutoFree(script.New().AsGodotObject());
        AssertObject(coin).IsInstanceOf<Coin>();
    }

    [TestCase]
    public void LoadTestResourceWithGameScript()
    {
        var stats = GD.Load<PlayerStats>("res://Resources/test_player_stats.tres");
        AssertThat(stats.Speed).IsEqual(1000f);
    }
}

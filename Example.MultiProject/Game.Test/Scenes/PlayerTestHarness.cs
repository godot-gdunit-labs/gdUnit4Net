namespace Game.Test.Scenes;

using Characters;

using Godot;

/// <summary>
///     Test-owned script on a test-owned scene, wrapping a game scene.
/// </summary>
public partial class PlayerTestHarness : Node2D
{
    public Player Player => GetNode<Player>("Player");

    public int JumpsSeen { get; private set; }

    public override void _Ready()
        => Player.Jumped += () => JumpsSeen++;
}

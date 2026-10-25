namespace Game.Characters;

using Godot;

[GlobalClass]
public partial class PlayerStats : Resource
{
    [Export]
    public float Speed { get; set; } = 100f;

    [Export]
    public float JumpVelocity { get; set; } = 300f;
}

namespace Game.Characters;

using Godot;

public partial class Player : Node2D
{
    [Signal]
    public delegate void JumpedEventHandler();

    [Export]
    public PlayerStats Stats { get; set; } = new();

    public int JumpCount { get; private set; }

    public int Lives { get; private set; }

    public override void _Ready()
        => Lives = ProjectSettings.GetSetting("game/rules/start_lives", 1).AsInt32();

    public override void _Process(double delta)
    {
        var direction = Input.GetAxis("move_left", "move_right");
        Position += new Vector2(direction * Stats.Speed * (float)delta, 0);
    }

    public override void _Input(InputEvent @event)
    {
        if (!@event.IsActionPressed("jump"))
            return;
        JumpCount++;
        EmitSignal(SignalName.Jumped);
    }
}

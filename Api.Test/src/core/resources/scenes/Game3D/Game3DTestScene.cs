namespace GdUnit4.Tests.Core.Resources.Scenes;

using Godot;

public partial class Game3DTestScene : Node3D
{
    private const float Speed = 5.0f;
    private const float JumpVelocity = 4.5f;

#nullable disable
    private CharacterBody3D player;
#nullable enable

    [Signal]
    public delegate void TargetClickedEventHandler();

    [Signal]
    public delegate void TargetHoveredEventHandler();

    [Signal]
    public delegate void StartPressedEventHandler();

    [Signal]
    public delegate void JumpedEventHandler();

    public Vector3? TargetClickPosition { get; private set; }

    public override void _Ready()
    {
        player = GetNode<CharacterBody3D>("Player");
        var target = GetNode<Area3D>("Target");
        target.InputEvent += OnTargetInputEvent;
        target.MouseEntered += EmitSignalTargetHovered;
        GetNode<Button>("HUD/StartButton").Pressed += EmitSignalStartPressed;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!player.IsOnFloor())
            player.Velocity += player.GetGravity() * (float)delta;
        if (Input.IsActionJustPressed("ui_accept") && player.IsOnFloor())
        {
            player.Velocity = player.Velocity with { Y = JumpVelocity };
            EmitSignalJumped();
        }

        player.Velocity = player.Velocity with { X = Input.GetAxis("ui_left", "ui_right") * Speed };
        player.MoveAndSlide();
    }

    private void OnTargetInputEvent(Node camera, InputEvent inputEvent, Vector3 eventPosition, Vector3 normal, long shapeIdx)
    {
        if (inputEvent is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            TargetClickPosition = eventPosition;
            EmitSignalTargetClicked();
        }
    }
}

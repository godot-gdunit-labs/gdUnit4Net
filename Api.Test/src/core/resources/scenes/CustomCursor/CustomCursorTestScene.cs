namespace GdUnit4.Tests.Core.Resources.Scenes;

using Godot;

public partial class CustomCursorTestScene : Node2D
{
#nullable disable
    private Sprite2D cursor;
#nullable enable

    public override void _Ready()
    {
        cursor = GetNode<Sprite2D>("Cursor");
        Input.MouseMode = Input.MouseModeEnum.Hidden;
    }

    public override void _ExitTree()
        => Input.MouseMode = Input.MouseModeEnum.Visible;

    public override void _Process(double delta)
        => cursor.Position = GetViewport().GetMousePosition();
}

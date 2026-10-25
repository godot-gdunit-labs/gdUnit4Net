namespace Game.Items;

using Autoload;

using Godot;

public partial class Coin : Node2D
{
    [Export]
    public int Value { get; set; } = 10;

    public void Collect()
    {
        GetNode<GameState>("/root/GameState").AddScore(Value);
        QueueFree();
    }
}

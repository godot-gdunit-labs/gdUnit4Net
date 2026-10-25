namespace Game.Autoload;

using Godot;

public partial class GameState : Node
{
    public int Score { get; private set; }

    public void AddScore(int value) => Score += value;

    public void Reset() => Score = 0;
}

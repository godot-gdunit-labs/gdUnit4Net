namespace Game.Levels;

using System.Linq;
using Characters;
using Godot;
using Items;

public partial class Level : Node2D
{
    public Player Player => GetNode<Player>("Player");

    public IReadOnlyList<Coin> Coins => [.. GetNode("Coins").GetChildren().OfType<Coin>()];

    public void CollectAll()
    {
        foreach (var coin in Coins)
            coin.Collect();
    }
}

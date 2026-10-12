namespace Examples.Test.Api.CSharpTypes;

// Original Player class (without IEquatable for comparison)
public class Player
{
    public Player(string name, int level, float health, bool isAlive)
    {
        Name = name;
        Level = level;
        Health = health;
        IsAlive = isAlive;
    }

    public string Name { get; }

    public int Level { get; }
    public float Health { get; }
    public bool IsAlive { get; }

    public override string ToString()
        => $"Player(Name: {Name}, Level: {Level}, Health: {Health}, IsAlive: {IsAlive})";
}

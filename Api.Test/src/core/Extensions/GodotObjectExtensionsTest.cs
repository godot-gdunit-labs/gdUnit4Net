namespace GdUnit4.Tests.Core.Extensions;

using GdUnit4.Core.Extensions;

using Godot;
using Godot.Collections;

using static Assertions;

[TestSuite]
public class GodotObjectExtensionsTest
{
    [TestCase]
    [RequireGodotRuntime]
    public static void DeepEqualsOnObject()
    {
        var items = new Array<Item>
        {
            new("ItemA", 10),
            new("ItemB", 20),
            new("ItemC", 30)
        };
        var player1 = new Player("Mage", 15, 75.0f, true, items);
        var player2 = new Player("Mage", 15, 75.0f, true, items);
        var player3 = new Player("Warrior", 20, 100.0f, true, items);
        var player4 = new Player("Mage", 15, 75.0f, true, new Array<Item>
        {
            new("ItemA", 10),
            new("ItemB", 20),
            new("ItemC", 31)
        });
        var player5 = new Player("Mage", 15, 75.0f, true, new Array<Item>
        {
            new("ItemA", 10),
            new("ItemB", 20),
            new("ItemC", 30)
        });

        AssertThat(GodotObjectExtensions.DeepEquals(player1, player2)).IsTrue();
        AssertThat(GodotObjectExtensions.DeepEquals(player1, player5)).IsTrue();

        // difference at name and health
        AssertThat(GodotObjectExtensions.DeepEquals(player1, player3)).IsFalse();

        // difference at items `ItemC`
        AssertThat(GodotObjectExtensions.DeepEquals(player1, player4)).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public static void DeepEqualsOnArray()
    {
        var items1 = new Array<Item>
        {
            new("ItemA", 10),
            new("ItemB", 20),
            new("ItemC", 30)
        };
        var items2 = new Array<Item>
        {
            new("ItemA", 10),
            new("ItemB", 20),
            new("ItemC", 30)
        };
        var items3 = new Array<Item>
        {
            new("ItemA", 10),
            new("ItemB", 20),
            new("ItemC", 33)
        };

        AssertThat(GodotObjectExtensions.DeepEquals(items1, items2)).IsTrue();
        AssertThat(GodotObjectExtensions.DeepEquals(items1, items3)).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public static void DeepEqualsOnDictionary()
    {
        var items1 = new Dictionary
        {
            { "ItemA", 10 },
            { "ItemB", 20 },
            { "ItemC", 30 },
            { "player", new Player("Mage", 15, 75.0f, true, []) }
        };
        var items2 = new Dictionary
        {
            { "ItemA", 10 },
            { "ItemB", 20 },
            { "ItemC", 30 },
            { "player", new Player("Mage", 15, 75.0f, true, []) }
        };
        var items3 = new Dictionary
        {
            { "ItemA", 10 },
            { "ItemB", 20 },
            { "ItemC", 30 },
            { "player", new Player("Mage", 15, 66.0f, true, []) }
        };
        var items4 = new Dictionary
        {
            { "ItemA", 10 },
            { "ItemB", 20 },
            { "ItemC", 33 },
            { "player", new Player("Mage", 15, 75.0f, true, []) }
        };

        AssertThat(GodotObjectExtensions.DeepEquals(items1, items2)).IsTrue();

        // difference on player
        AssertThat(GodotObjectExtensions.DeepEquals(items1, items3)).IsFalse();

        // difference on itemC
        AssertThat(GodotObjectExtensions.DeepEquals(items1, items4)).IsFalse();
    }
}

public partial class Item : RefCounted
{
    public Item(string name, int count)
    {
        Name = name;
        Count = count;
    }

    public int Count { get; set; }

    public string Name { get; set; }
}

public partial class Player : Node
{
    private readonly Array<Item> items;
    private int myField;

    public Player(string name, int level, float health, bool isAlive, Array<Item> items)
    {
        this.items = items;
        Name = name;
        Level = level;
        Health = health;
        IsAlive = isAlive;
        myField = 42;
    }

    public int Level { get; set; }

    public float Health { get; set; }

    public bool IsAlive { get; set; }
}

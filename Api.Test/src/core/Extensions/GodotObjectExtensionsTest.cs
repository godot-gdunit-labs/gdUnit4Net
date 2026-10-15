namespace GdUnit4.Tests.Core.Extensions;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

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

    #region DeepEquals on properties that cannot be read

    [TestCase]
    public static void DeepEqualsSkipsUnreadableProperties()
    {
        AssertThat(GodotObjectExtensions.DeepEquals(new UnreadableProperties(1), new UnreadableProperties(1))).IsTrue();
        AssertThat(GodotObjectExtensions.DeepEquals(new UnreadableProperties(1), new UnreadableProperties(2))).IsFalse();
    }

    [TestCase]
    public static void DeepEqualsPropagatesExceptionOfComparedValue()
        => AssertThrown(() => GodotObjectExtensions.DeepEquals(new WithFailingEquatable(), new WithFailingEquatable()))
            .IsInstanceOf<InvalidOperationException>()
            .HasMessage("Equals failed");

    #endregion

    #region ToGodotArray on values without a Variant representation

    [TestCase]
    [RequireGodotRuntime]
    public static void ToGodotArrayConvertsUnsupportedValuesToNotAvailable()
    {
        using var converted = new object[] { 1, new(), 2.5m, "text" }.ToGodotArray();

        // a type without a Variant conversion and a decimal, which the conversion unboxes as ulong, both end up as 'n.a'
        AssertThat(converted.Select(value => value.ToString())).ContainsExactly("1", "n.a", "n.a", "text");
    }

    #endregion

    private sealed class UnreadableProperties
    {
        public UnreadableProperties(int value)
            => Value = value;

        public int Value { get; }

        public int Failing => throw new InvalidOperationException("getter failed");

        public Span<int> NotBoxable => [];

        public int this[int index] => index;
    }

    [SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "The test verifies that an exception of the compared value propagates")]
    private sealed class FailingEquatable : IEquatable<FailingEquatable>
    {
        public bool Equals(FailingEquatable? other) => throw new InvalidOperationException("Equals failed");

        public override bool Equals(object? obj) => throw new InvalidOperationException("Equals failed");

        public override int GetHashCode() => 0;
    }

    private sealed class WithFailingEquatable
    {
        public FailingEquatable Value { get; } = new();
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

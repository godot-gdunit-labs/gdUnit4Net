namespace Examples.Test.Api.CSharpTypes;

using GdUnit4;

using static GdUnit4.Assertions;

[TestSuite]
public class AssertObjectTest
{
    [TestCase]
    public void TestObjectEqualityByReflection()
    {
        // Create objects with same values
        var player1 = new Player("Mage", 15, 75.0f, true);
        var player2 = new Player("Mage", 15, 75.0f, true);
        var player3 = new Player("Mage", 15, 75.0f, false);
        var player4 = player1; // Same reference

        AssertObject(player1).IsNotEqual(player3);
        // Test reference equality
        AssertObject(player1)
            .IsNotSame(player2) // Different instances
            .IsSame(player4) // Same reference
            .IsEqual(player2) // equal by properties
            .IsNotEqual(player3); // Different properties
    }

    [TestCase]
    public void TestIEquatableImplementationIsCalled()
    {
        // Create spy objects that track method calls
        var spyPlayer1 = new SpyEquatablePlayer("Warrior", 20, 100.0f, true);
        var spyPlayer2 = new SpyEquatablePlayer("Warrior", 20, 100.0f, true);
        var spyPlayer3 = new SpyEquatablePlayer("Mage", 15, 75.0f, false);

        // Test equal objects - should call IEquatable.Equals
        AssertObject(spyPlayer1).IsEqual(spyPlayer2);

        // Verify that the IEquatable.Equals method was called
        AssertThat(spyPlayer1.EqualsCallCount).IsEqual(1);
        AssertThat(spyPlayer1.LastEqualsArgument).IsEqual(spyPlayer2);

        // Reset and test unequal objects
        spyPlayer1.ResetSpy();
        AssertObject(spyPlayer1).IsNotEqual(spyPlayer3);

        // Verify that the IEquatable.Equals method was called for the unequal comparison too
        AssertThat(spyPlayer1.EqualsCallCount).IsEqual(1);
        AssertThat(spyPlayer1.LastEqualsArgument).IsEqual(spyPlayer3);
        AssertThat(spyPlayer1.LastEqualsResult).IsFalse();
    }

    [TestCase]
    public void TestEqualsMethodCallFrequency()
    {
        var spyPlayer1 = new SpyEquatablePlayer("Paladin", 25, 120.0f, true);
        var spyPlayer2 = new SpyEquatablePlayer("Paladin", 25, 120.0f, true);

        spyPlayer1.ResetSpy();

        // Multiple equality checks
        AssertObject(spyPlayer1).IsEqual(spyPlayer2);
        AssertObject(spyPlayer1).IsEqual(spyPlayer2);

        // Verify call frequency
        AssertThat(spyPlayer1.EqualsCallCount).IsEqual(2);

        // Verify call history
        AssertThat(spyPlayer1.EqualsCallHistory).HasSize(2);
        AssertThat(spyPlayer1.EqualsCallHistory[0]).IsEqual(spyPlayer2);
        AssertThat(spyPlayer1.EqualsCallHistory[1]).IsEqual(spyPlayer2);
    }

    [TestCase]
    public void TestIEquatableWithNullComparison()
    {
        var spyPlayer = new SpyEquatablePlayer("Rogue", 18, 80.0f, true);

        // Test comparison with null
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        AssertObject(spyPlayer).IsNotEqual(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Should not call IEquatable.Equals for null comparison (handled by reference check)
        AssertThat(spyPlayer.EqualsCallCount).IsEqual(0);
    }

    [TestCase]
    public void TestIEquatableWithSelfComparison()
    {
        var spyPlayer = new SpyEquatablePlayer("Monk", 22, 90.0f, true);

        // Test self comparison (should use reference equality shortcut)
        AssertObject(spyPlayer).IsEqual(spyPlayer);

        // Should not call IEquatable.Equals for self comparison (handled by ReferenceEquals)
        AssertThat(spyPlayer.EqualsCallCount).IsEqual(0);
    }

    [TestCase]
    public void TestIEquatableCallDetails()
    {
        var spyPlayer1 = new SpyEquatablePlayer("Cleric", 30, 150.0f, true);
        var spyPlayer2 = new SpyEquatablePlayer("Cleric", 30, 150.0f, true);
        var spyPlayer3 = new SpyEquatablePlayer("Thief", 12, 60.0f, false);

        // Test multiple comparisons
        AssertObject(spyPlayer1).IsEqual(spyPlayer2);
        AssertObject(spyPlayer1).IsNotEqual(spyPlayer3);

        // Verify call details
        AssertThat(spyPlayer1.EqualsCallCount).IsEqual(2);
        AssertThat(spyPlayer1.EqualsCallHistory).Contains(spyPlayer2);
        AssertThat(spyPlayer1.EqualsCallHistory).Contains(spyPlayer3);
        AssertThat(spyPlayer1.LastEqualsResult).IsFalse(); // Last call was with spyPlayer3 (not equal)

        // Verify that both calls used the real equality logic
        AssertThat(spyPlayer1.EqualsCallHistory[0]).IsEqual(spyPlayer2); // First call: equal
        AssertThat(spyPlayer1.EqualsCallHistory[1]).IsEqual(spyPlayer3); // Second call: not equal
    }

    [TestCase]
    public void TestIEqualityComparerImplementationIsCalled()
    {
        // Create spy objects that track method calls
        var spyPlayer1 = new SpyEqualityComparerPlayer("Warrior", 20, 100.0f, true);
        var spyPlayer2 = new SpyEqualityComparerPlayer("Warrior", 20, 100.0f, true);
        var spyPlayer3 = new SpyEqualityComparerPlayer("Mage", 15, 75.0f, false);

        // Test equal objects - should call IEquatable.Equals
        AssertObject(spyPlayer1).IsEqual(spyPlayer2);

        // Verify that the IEquatable.Equals method was called
        AssertThat(spyPlayer1.EqualsCallCount).IsEqual(1);
        AssertThat(spyPlayer1.LastEqualsArgument).IsEqual(spyPlayer2);

        // Reset and test unequal objects
        spyPlayer1.ResetSpy();
        AssertObject(spyPlayer1).IsNotEqual(spyPlayer3);

        // Verify that the IEquatable.Equals method was called for the unequal comparison too
        AssertThat(spyPlayer1.EqualsCallCount).IsEqual(1);
        AssertThat(spyPlayer1.LastEqualsArgument).IsEqual(spyPlayer3);
        AssertThat(spyPlayer1.LastEqualsResult).IsFalse();
    }
}

public class SpyEqualityComparerPlayer(string name, int level, float health, bool isAlive)
    : Player(name, level, health, isAlive), IEqualityComparer<Player>
{
    public int EqualsCallCount { get; private set; }
    public Player? LastEqualsArgument { get; private set; }

    public bool LastEqualsResult { get; private set; }

    public bool Equals(Player? p1, Player? p2)
    {
        // Track the call
        EqualsCallCount++;
        LastEqualsArgument = p2;

        if (ReferenceEquals(p1, p2))
        {
            LastEqualsResult = true;
            return true;
        }

        if (p2 is null || p1 is null)
        {
            LastEqualsResult = false;
            return false;
        }

        var result = p1.Level == p2.Level
                     && Math.Abs(p1.Health - p2.Health) <= 0.0
                     && p1.IsAlive == p2.IsAlive
                     && p1.Name == p2.Name;
        LastEqualsResult = result;
        return result;
    }

    public int GetHashCode(Player player)
        => HashCode.Combine(player.Level, player.Health, player.IsAlive, player.Name);

    public void ResetSpy()
    {
        EqualsCallCount = 0;
        LastEqualsArgument = null;
        LastEqualsResult = false;
    }
}

// Spy wrapper that tracks method calls on real objects
public class SpyEquatablePlayer(string name, int level, float health, bool isAlive)
    : EquatablePlayer(name, level, health, isAlive)
{
    private readonly List<EquatablePlayer?> equalsCallHistory = [];

    // Tracking properties
    public int EqualsCallCount { get; private set; }

    public EquatablePlayer? LastEqualsArgument { get; private set; }

    public bool LastEqualsResult { get; private set; }

    public IReadOnlyList<EquatablePlayer?> EqualsCallHistory => equalsCallHistory.AsReadOnly();

    public void ResetSpy()
    {
        EqualsCallCount = 0;
        equalsCallHistory.Clear();
        LastEqualsArgument = null;
        LastEqualsResult = false;
    }

    public override bool Equals(EquatablePlayer? other)
    {
        // Track the call
        EqualsCallCount++;
        LastEqualsArgument = other;
        equalsCallHistory.Add(other);

        // Delegate to real implementation
        var result = base.Equals(other);

        LastEqualsResult = result;
        return result;
    }
}

// Enhanced Player class that implements IEquatable
public class EquatablePlayer(string name, int level, float health, bool isAlive)
    : Player(name, level, health, isAlive), IEquatable<EquatablePlayer>
{
    public virtual bool Equals(EquatablePlayer? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return Level == other.Level &&
               Math.Abs(Health - other.Health) < 0.001f &&
               IsAlive == other.IsAlive &&
               Name == other.Name;
    }

    public override bool Equals(object? obj)
        => Equals(obj as EquatablePlayer);

    public override int GetHashCode()
        => HashCode.Combine(Level, Health, IsAlive, Name);
}

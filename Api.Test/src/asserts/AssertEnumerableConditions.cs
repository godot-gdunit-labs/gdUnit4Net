namespace GdUnit4.Tests.Asserts;

using GdUnit4.Asserts;
using GdUnit4.Core.Execution.Exceptions;
using GdUnit4.Core.Extensions;

using static Assertions;

public class AssertEnumerableConditions
{
    protected void DoIsEmpty(dynamic current, dynamic filled)
    {
        AssertArray(current).IsEmpty();

        // should fail because the array is not empty it has a size of one
        AssertThrown(() => AssertArray(filled).IsEmpty())
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting be empty:
                         but has size '4'
                        """);
    }

    protected void DoIsNotEmpty(dynamic current, dynamic empty)
    {
        AssertArray(null).IsNotEmpty();
        AssertArray(current).IsNotEmpty();

        // should fail because the array is empty
        AssertThrown(() => AssertArray(empty).IsNotEmpty())
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting being NOT empty:
                         but is empty
                        """);
    }

    protected void DoIsNull(dynamic current, dynamic obj5)
    {
        AssertArray(null).IsNull();

        // should fail because the current is not null
        string isNull = AssertFailures.IsNull(current);
        AssertThrown(() => AssertArray(current).IsNull())
            .IsInstanceOf<TestFailedException>()
            .StartsWithMessage("Expecting be <Null>:")
            .HasMessage(isNull.RichTextNormalize());
    }

    protected void DoIsNotNull(dynamic current, dynamic obj5)
    {
        AssertArray(current).IsNotNull();

        // should fail because the current is null
        AssertThrown(() => AssertArray(null).IsNotNull())
            .IsInstanceOf<TestFailedException>()
            .HasMessage(AssertFailures.IsNotNull());
    }

    protected void DoHasSize(dynamic current)
    {
        AssertArray(current).HasSize(4);

        // should fail because the array has a size of 4
        AssertThrown(() => AssertArray(current).HasSize(5))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting size:
                            '5' but is '4'
                        """);
        AssertThrown(() => AssertArray(null).HasSize(4))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting size:
                            '4' but is unknown
                        """);
    }

    protected void DoContains(dynamic current, dynamic obj5)
    {
        var obj1 = current[0];
        var obj2 = current[1];
        var obj3 = current[2];
        var obj4 = current[3];

        // test against only one element
        AssertArray(current).Contains(obj1);
        AssertArray(current).Contains(obj2);
        AssertArray(current).Contains(obj2, obj3);
        AssertArray(current).Contains(obj1, obj2, obj3, obj4);
        AssertArray(current).Contains(obj4, obj1, obj3, obj2);
        AssertArray(current).Contains(current);
        AssertThrown(() => AssertArray(current).Contains(obj5))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting contains elements:
                    [$obj1, $obj2, $obj3, $obj4]
                 do contains (in any order)
                    [$obj5]
                 but could not find elements:
                    [$obj5]
                """
                .Replace("$obj1", ValueFormatter.AsString(obj1))
                .Replace("$obj2", ValueFormatter.AsString(obj2))
                .Replace("$obj3", ValueFormatter.AsString(obj3))
                .Replace("$obj4", ValueFormatter.AsString(obj4))
                .Replace("$obj5", ValueFormatter.AsString(obj5)));

        AssertThrown(() => AssertArray(current).Contains(obj1, obj2, obj5))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting contains elements:
                    [$obj1, $obj2, $obj3, $obj4]
                 do contains (in any order)
                    [$obj1, $obj2, $obj5]
                 but could not find elements:
                    [$obj5]
                """
                .Replace("$obj1", ValueFormatter.AsString(obj1))
                .Replace("$obj2", ValueFormatter.AsString(obj2))
                .Replace("$obj3", ValueFormatter.AsString(obj3))
                .Replace("$obj4", ValueFormatter.AsString(obj4))
                .Replace("$obj5", ValueFormatter.AsString(obj5)));
    }

    protected void ContainsUsingDictionaryWithDifferentOrder(dynamic current, dynamic expected)
        => AssertArray(current).HasSize(1).Contains(expected);

    protected void DoContainsSame(dynamic current, dynamic obj5)
    {
        var obj1 = current[0];
        var obj2 = current[1];
        var obj3 = current[2];
        var obj4 = current[3];

        AssertArray(current).ContainsSame(obj1);
        AssertArray(current).ContainsSame(obj2);
        AssertArray(current).ContainsSame(obj2, obj3);
        AssertArray(current).ContainsSame(obj1, obj2, obj3, obj4);
        AssertArray(current).ContainsSame(obj4, obj1, obj3, obj2);
        AssertArray(current).ContainsSame(current);

        // should fail because the array not contains 'obj5'
        AssertThrown(() => AssertArray(current).ContainsSame(obj1, obj5))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting contains elements:
                    [$obj1, $obj2, $obj3, $obj4]
                 do contains (in any order)
                    [$obj1, $obj5]
                 but could not find elements:
                    [$obj5]
                """
                .Replace("$obj1", ValueFormatter.AsString(obj1))
                .Replace("$obj2", ValueFormatter.AsString(obj2))
                .Replace("$obj3", ValueFormatter.AsString(obj3))
                .Replace("$obj4", ValueFormatter.AsString(obj4))
                .Replace("$obj5", ValueFormatter.AsString(obj5)));
    }

    protected void DoContainsExactly(dynamic current, dynamic obj5)
    {
        var obj1 = current[0];
        var obj2 = current[1];
        var obj3 = current[2];
        var obj4 = current[3];

        // test against only one element
        AssertArray(current).ContainsExactly(obj1, obj2, obj3, obj4);
        AssertArray(current).ContainsExactly(current);
        AssertThrown(() => AssertArray(current).ContainsExactly(obj1))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting contains exactly elements:
                    [$obj1, $obj2, $obj3, $obj4]
                 do contains (in same order)
                    [$obj1]
                 but others where not expected:
                    [$obj2, $obj3, $obj4]
                """
                .Replace("$obj1", ValueFormatter.AsString(obj1))
                .Replace("$obj2", ValueFormatter.AsString(obj2))
                .Replace("$obj3", ValueFormatter.AsString(obj3))
                .Replace("$obj4", ValueFormatter.AsString(obj4)));
        AssertThrown(() => AssertArray(current).ContainsExactly(obj1, obj2))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting contains exactly elements:
                    [$obj1, $obj2, $obj3, $obj4]
                 do contains (in same order)
                    [$obj1, $obj2]
                 but others where not expected:
                    [$obj3, $obj4]
                """
                .Replace("$obj1", ValueFormatter.AsString(obj1))
                .Replace("$obj2", ValueFormatter.AsString(obj2))
                .Replace("$obj3", ValueFormatter.AsString(obj3))
                .Replace("$obj4", ValueFormatter.AsString(obj4)));
        AssertThrown(() => AssertArray(current).ContainsExactly(obj1, obj5))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting contains exactly elements:
                    [$obj1, $obj2, $obj3, $obj4]
                 do contains (in same order)
                    [$obj1, $obj5]
                 but others where not expected:
                    [$obj2, $obj3, $obj4]
                 and some elements not found:
                    [$obj5]
                """
                .Replace("$obj1", ValueFormatter.AsString(obj1))
                .Replace("$obj2", ValueFormatter.AsString(obj2))
                .Replace("$obj3", ValueFormatter.AsString(obj3))
                .Replace("$obj4", ValueFormatter.AsString(obj4))
                .Replace("$obj5", ValueFormatter.AsString(obj5)));
        AssertThrown(() => AssertArray(current).ContainsExactly(obj1, obj3, obj2, obj4))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting contains exactly elements:
                    [$obj1, $obj2, $obj3, $obj4]
                 do contains (in same order)
                    [$obj1, $obj3, $obj2, $obj4]
                 but there has differences in order:
                    - element at index 0 expect $obj3 but is $obj2
                    - element at index 1 expect $obj2 but is $obj3
                """
                .Replace("$obj1", ValueFormatter.AsString(obj1))
                .Replace("$obj2", ValueFormatter.AsString(obj2))
                .Replace("$obj3", ValueFormatter.AsString(obj3))
                .Replace("$obj4", ValueFormatter.AsString(obj4))
                .Replace("$obj5", ValueFormatter.AsString(obj5)));
    }

    protected void DoContainsSameExactly(dynamic current)
    {
        var obj1 = current[0];
        var obj2 = current[1];
        var obj3 = current[2];
        var obj4 = current[3];

        // test against only one element
        AssertArray(current).ContainsSameExactly(obj1, obj2, obj3, obj4);
        AssertArray(current).ContainsSameExactly(current);

        // when compare by reference equal obj1 != obj4
        AssertThrown(() => AssertArray(current).ContainsSameExactly(obj1, obj2, obj3, obj1))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting contains exactly elements:
                    [$obj1, $obj2, $obj3, $obj4]
                 do contains (in same order)
                    [$obj1, $obj2, $obj3, $obj1]
                 but others where not expected:
                    [$obj4]
                 and some elements not found:
                    [$obj1]
                """
                .Replace("$obj1", ValueFormatter.AsString(obj1))
                .Replace("$obj2", ValueFormatter.AsString(obj2))
                .Replace("$obj3", ValueFormatter.AsString(obj3))
                .Replace("$obj4", ValueFormatter.AsString(obj4)));
        AssertThrown(() => AssertArray(current).ContainsSameExactly(obj1))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting contains exactly elements:
                    [$obj1, $obj2, $obj3, $obj4]
                 do contains (in same order)
                    [$obj1]
                 but others where not expected:
                    [$obj2, $obj3, $obj4]
                """
                .Replace("$obj1", ValueFormatter.AsString(obj1))
                .Replace("$obj2", ValueFormatter.AsString(obj2))
                .Replace("$obj3", ValueFormatter.AsString(obj3))
                .Replace("$obj4", ValueFormatter.AsString(obj4)));
        AssertThrown(() => AssertArray(current).ContainsSameExactly(obj1, obj2))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting contains exactly elements:
                    [$obj1, $obj2, $obj3, $obj4]
                 do contains (in same order)
                    [$obj1, $obj2]
                 but others where not expected:
                    [$obj3, $obj4]
                """
                .Replace("$obj1", ValueFormatter.AsString(obj1))
                .Replace("$obj2", ValueFormatter.AsString(obj2))
                .Replace("$obj3", ValueFormatter.AsString(obj3))
                .Replace("$obj4", ValueFormatter.AsString(obj4)));
    }

    protected void DoContainsExactlyInAnyOrder(dynamic current, dynamic obj5)
    {
        var obj1 = current[0];
        var obj2 = current[1];
        var obj3 = current[2];
        var obj4 = current[3];

        AssertArray(current).ContainsExactlyInAnyOrder(current);
        AssertArray(current).ContainsExactlyInAnyOrder(obj1, obj2, obj3, obj4);
        AssertArray(current).ContainsExactlyInAnyOrder(obj4, obj2, obj3, obj1);
        AssertArray(current).ContainsExactlyInAnyOrder(obj2, obj4, obj3, obj1);
        AssertArray(current).ContainsExactlyInAnyOrder(obj1, obj2, obj3, obj4);
        AssertArray(current).ContainsExactlyInAnyOrder(obj4, obj2, obj3, obj1);

        // should fail because it contains not exactly the same elements in any order
        AssertThrown(() => AssertArray(current)
                .ContainsExactlyInAnyOrder(obj1, obj2, obj5, obj3, obj4))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting contains exactly elements:
                    [$obj1, $obj2, $obj3, $obj4]
                 do contains (in any order)
                    [$obj1, $obj2, $obj5, $obj3, $obj4]
                 but could not find elements:
                    [$obj5]
                """
                .Replace("$obj1", ValueFormatter.AsString(obj1))
                .Replace("$obj2", ValueFormatter.AsString(obj2))
                .Replace("$obj3", ValueFormatter.AsString(obj3))
                .Replace("$obj4", ValueFormatter.AsString(obj4))
                .Replace("$obj5", ValueFormatter.AsString(obj5)));

        // should fail because it contains not all elements
        AssertThrown(() => AssertArray(current)
                .ContainsExactlyInAnyOrder(obj1, obj2, obj4))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting contains exactly elements:
                    [$obj1, $obj2, $obj3, $obj4]
                 do contains (in any order)
                    [$obj1, $obj2, $obj4]
                 but some elements where not expected:
                    [$obj3]
                """
                .Replace("$obj1", ValueFormatter.AsString(obj1))
                .Replace("$obj2", ValueFormatter.AsString(obj2))
                .Replace("$obj3", ValueFormatter.AsString(obj3))
                .Replace("$obj4", ValueFormatter.AsString(obj4))
                .Replace("$obj5", ValueFormatter.AsString(obj5)));
    }

    protected void DoContainsSameExactlyInAnyOrder(dynamic current, dynamic obj5)
    {
        var obj1 = current[0];
        var obj2 = current[1];
        var obj3 = current[2];
        var obj4 = current[3];

        AssertArray(current).ContainsSameExactlyInAnyOrder(current);
        AssertArray(current).ContainsSameExactlyInAnyOrder(obj1, obj2, obj3, obj4);
        AssertArray(current).ContainsSameExactlyInAnyOrder(obj4, obj2, obj3, obj1);
        AssertArray(current).ContainsSameExactlyInAnyOrder(obj2, obj4, obj3, obj1);

        // should fail because it contains not exactly the same elements in any order
        AssertThrown(() => AssertArray(current)
                .ContainsSameExactlyInAnyOrder(obj1, obj2, obj5, obj3, obj4))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting contains exactly elements:
                    [$obj1, $obj2, $obj3, $obj4]
                 do contains (in any order)
                    [$obj1, $obj2, $obj5, $obj3, $obj4]
                 but could not find elements:
                    [$obj5]
                """
                .Replace("$obj1", ValueFormatter.AsString(obj1))
                .Replace("$obj2", ValueFormatter.AsString(obj2))
                .Replace("$obj3", ValueFormatter.AsString(obj3))
                .Replace("$obj4", ValueFormatter.AsString(obj4))
                .Replace("$obj5", ValueFormatter.AsString(obj5)));

        // should fail because it contains not all elements
        AssertThrown(() => AssertArray(current)
                .ContainsSameExactlyInAnyOrder(obj1, obj2, obj4))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting contains exactly elements:
                    [$obj1, $obj2, $obj3, $obj4]
                 do contains (in any order)
                    [$obj1, $obj2, $obj4]
                 but some elements where not expected:
                    [$obj3]
                """
                .Replace("$obj1", ValueFormatter.AsString(obj1))
                .Replace("$obj2", ValueFormatter.AsString(obj2))
                .Replace("$obj3", ValueFormatter.AsString(obj3))
                .Replace("$obj4", ValueFormatter.AsString(obj4))
                .Replace("$obj5", ValueFormatter.AsString(obj5)));
    }

    protected void DoNotContainsOnObject(dynamic current, dynamic obj5)
    {
        var obj1 = current[0];
        var obj2 = current[1];
        var obj3 = current[2];
        var obj4 = current[3];

        AssertArray(current).NotContains(obj5);
        AssertArray(current).NotContains(obj5, obj5);
        AssertThrown(() => AssertArray(current).NotContains(obj1))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting:
                    [$obj1, $obj2, $obj3, $obj4]
                 do NOT contains (in any order)
                    [$obj1]
                 but found elements:
                    [$obj1]
                """
                .Replace("$obj1", ValueFormatter.AsString(obj1))
                .Replace("$obj2", ValueFormatter.AsString(obj2))
                .Replace("$obj3", ValueFormatter.AsString(obj3))
                .Replace("$obj4", ValueFormatter.AsString(obj4))
                .Replace("$obj5", ValueFormatter.AsString(obj5)));
        AssertThrown(() => AssertArray(current).NotContains(obj1, obj2, obj5))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting:
                    [$obj1, $obj2, $obj3, $obj4]
                 do NOT contains (in any order)
                    [$obj1, $obj2, $obj5]
                 but found elements:
                    [$obj1, $obj2]
                """
                .Replace("$obj1", ValueFormatter.AsString(obj1))
                .Replace("$obj2", ValueFormatter.AsString(obj2))
                .Replace("$obj3", ValueFormatter.AsString(obj3))
                .Replace("$obj4", ValueFormatter.AsString(obj4))
                .Replace("$obj5", ValueFormatter.AsString(obj5)));
    }

    protected void DoNotContainsSameOnObject(dynamic current, dynamic obj5)
    {
        var obj1 = current[0];
        var obj2 = current[1];
        var obj3 = current[2];
        var obj4 = current[3];

        AssertArray(current).NotContainsSame(obj5);
        AssertArray(current).NotContainsSame(obj5, obj5);
        AssertThrown(() => AssertArray(current).NotContainsSame(obj1))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting:
                    [$obj1, $obj2, $obj3, $obj4]
                 do NOT contains (in any order)
                    [$obj1]
                 but found elements:
                    [$obj1]
                """
                .Replace("$obj1", ValueFormatter.AsString(obj1))
                .Replace("$obj2", ValueFormatter.AsString(obj2))
                .Replace("$obj3", ValueFormatter.AsString(obj3))
                .Replace("$obj4", ValueFormatter.AsString(obj4))
                .Replace("$obj5", ValueFormatter.AsString(obj5)));
        AssertThrown(() => AssertArray(current).NotContainsSame(obj1, obj2, obj5))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting:
                    [$obj1, $obj2, $obj3, $obj4]
                 do NOT contains (in any order)
                    [$obj1, $obj2, $obj5]
                 but found elements:
                    [$obj1, $obj2]
                """
                .Replace("$obj1", ValueFormatter.AsString(obj1))
                .Replace("$obj2", ValueFormatter.AsString(obj2))
                .Replace("$obj3", ValueFormatter.AsString(obj3))
                .Replace("$obj4", ValueFormatter.AsString(obj4))
                .Replace("$obj5", ValueFormatter.AsString(obj5)));
    }
}

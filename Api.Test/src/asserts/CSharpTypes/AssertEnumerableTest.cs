namespace GdUnit4.Tests.Asserts.CSharpTypes;

using System;
using System.Collections.Generic;

using GdUnit4.Core.Execution.Exceptions;

using static Assertions;

[TestSuite]
public class AssertEnumerableTest : AssertEnumerableConditions
{
    public static IEnumerable<object[]> TestValues =>
    [
        [
            "Array",
            new object[] { new SimpleObject("A1"), new SimpleObject("A2"), new SimpleObject("A3"), new SimpleObject("A4") },
            new object[] { new SimpleObject("A1"), new SimpleObject("A2"), new SimpleObject("A3"), new SimpleObject("A4") },
            Array.Empty<object>(),
            new SimpleObject("X")
        ],
        [
            "List",
            new List<SimpleObject> { new("A1"), new("A2"), new("A3"), new("A4") },
            new List<SimpleObject> { new("A1"), new("A2"), new("A3"), new("A4") },
            new List<SimpleObject>(),
            new SimpleObject("X")
        ],
        [
            "Array<string>",
            new[] { "A1", "A2", "A3", "A4" },
            new[] { "A1", "A2", "A3", "A4" },
            Array.Empty<string>(),
            "X"
        ]
    ];

    [TestCase]
    public void IsEqual()
    {
        AssertArray([]).IsEqual([]);
        AssertArray(Array.Empty<int>()).IsEqual([]);
        AssertArray(new[] { 1, 2, 4, 5 }).IsEqual(new[] { 1, 2, 4, 5 });

        AssertThrown(() => AssertArray(new[] { 1, 2, 4, 5 }).IsEqual([1, 2, 3, 4, 2, 5]))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting be equal:
                            [1, 2, 3, 4, 2, 5]
                         but is
                            [1, 2, 4, 5]
                        """);
        AssertThrown(() => AssertArray<object>(null).IsEqual([]))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting be equal:
                            <Empty>
                         but is
                            <Null>
                        """);
    }

    [TestCase]
    public void IsEqualIgnoringCase()
    {
        AssertArray(["this", "is", "a", "message"])
            .IsEqualIgnoringCase(["This", "is", "a", "Message"]);
        AssertArray(new List<string> { "this", "is", "a", "message" })
            .IsEqualIgnoringCase(new List<string> { "This", "is", "a", "Message" });

        // should fail because the array not contains same elements
        AssertThrown(() => AssertArray(["this", "is", "a", "message"])
                .IsEqualIgnoringCase(["This", "is", "an", "Message"]))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting be equal (ignoring case):
                            ["This", "is", "an", "Message"]
                         but is
                            ["this", "is", "a", "message"]
                        """);
        AssertThrown(() => AssertArray<object>(null)
                .IsEqualIgnoringCase(["This", "is"]))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting be equal (ignoring case):
                            ["This", "is"]
                         but is
                            <Null>
                        """);
    }

    [TestCase]
    public void IsNotEqual()
    {
        AssertArray<int>(null)
            .IsNotEqual([1, 2, 3, 4, 5]);
        AssertArray(new[] { 1, 2, 3, 4, 5 })
            .IsNotEqual([1, 2, 3, 4, 5, 6]);
        AssertArray(new List<int> { 1, 2, 3, 4, 5 })
            .IsNotEqual(new List<int> { 1, 2, 3, 4, 5, 6 });

        // should fail because the array  contains same elements
        AssertThrown(() => AssertArray(new[] { 1, 2, 3, 4, 5 }).IsNotEqual(new[] { 1, 2, 3, 4, 5 }))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting be NOT equal:
                            [1, 2, 3, 4, 5]
                         but is
                            [1, 2, 3, 4, 5]
                        """);
    }

    [TestCase]
    public void IsNotEqualIgnoringCase()
    {
        AssertArray(null)
            .IsNotEqualIgnoringCase(["This", "is", "an", "Message"]);
        AssertArray(["this", "is", "a", "message"])
            .IsNotEqualIgnoringCase(["This", "is", "an", "Message"]);
        AssertArray(new List<string> { "this", "is", "a", "message" })
            .IsNotEqualIgnoringCase(new List<string> { "This", "is", "an", "Message" });

        // should fail because the array contains same elements ignoring case-sensitive
        AssertThrown(() => AssertArray(["this", "is", "a", "message"])
                .IsNotEqualIgnoringCase(["This", "is", "a", "Message"]))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting be NOT equal (ignoring case):
                            ["This", "is", "a", "Message"]
                         but is
                            ["this", "is", "a", "message"]
                        """);
    }

    [TestCase]
    public void IsSame()
    {
        var current = new[] { "this", "is", "a", "message" };
        var other = new[] { "this", "is", "a", "message" };
        AssertArray(current).IsSame(current);

        AssertThrown(() => AssertArray(current).IsSame(other))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting be same:
                            ["this", "is", "a", "message"]
                         to refer to the same object
                            ["this", "is", "a", "message"]
                        """);
    }

    [TestCase]
    public void IsNotSame()
    {
        var current = new[] { "this", "is", "a", "message" };
        var other = new[] { "this", "is", "a", "message" };
        AssertArray(current).IsNotSame(other);

        AssertThrown(() => AssertArray(current).IsNotSame(current))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting be NOT same: ["this", "is", "a", "message"]
                        """);
    }

    [TestCase]
    [DataPoint(nameof(TestValues))]
    public void IsNull(string name, dynamic current, dynamic copy, dynamic empty, dynamic obj5)
        => DoIsNull(current, obj5);

    [TestCase]
    [DataPoint(nameof(TestValues))]
    public void IsNotNull(string name, dynamic current, dynamic copy, dynamic empty, dynamic obj5)
        => DoIsNotNull(current, obj5);

    [TestCase]
    [DataPoint(nameof(TestValues))]
    public void HasSize(string name, dynamic current, dynamic copy, dynamic empty, dynamic obj5)
        => DoHasSize(current);

    [TestCase]
    [DataPoint(nameof(TestValues))]
    public void IsEmpty(string name, dynamic current, dynamic copy, dynamic empty, dynamic obj5)
        => DoIsEmpty(empty, current);

    [TestCase]
    [DataPoint(nameof(TestValues))]
    public void IsNotEmpty(string name, dynamic current, dynamic copy, dynamic empty, dynamic obj5)
        => DoIsNotEmpty(current, empty);

    [TestCase]
    [DataPoint(nameof(TestValues))]
    public void Contains(string name, dynamic current, dynamic copy, dynamic empty, dynamic obj5)
        => DoContains(current, obj5);

    [TestCase]
    public void ContainsUsingDictionaryWithDifferentOrder()
        => ContainsUsingDictionaryWithDifferentOrder(
            new object[]
        {
            new Dictionary<string, object>
            {
                ["managed_type"] = "gdUnit4.addons.gdUnit4.test.dotnet.ExampleTestSuite",
                ["test_name"] = "IsFoo",
                ["source_file"] = "res://addons/gdUnit4/test/dotnet/ExampleTestSuite.cs",
                ["line_number"] = 16,
                ["attribute_index"] = 0,
                ["require_godot_runtime"] = true,
                ["code_file_path"] = "res://addons/gdUnit4/test/dotnet/ExampleTestSuite.cs",
                ["fully_qualified_name"] = "gdUnit4.addons.gdUnit4.test.dotnet.ExampleTestSuite.IsFoo",
                ["simple_name"] = "IsFoo"
            }
        }, new Dictionary<string, object>
        {
            ["test_name"] = "IsFoo",
            ["source_file"] = "res://addons/gdUnit4/test/dotnet/ExampleTestSuite.cs",
            ["line_number"] = 16,
            ["attribute_index"] = 0,
            ["require_godot_runtime"] = true,
            ["code_file_path"] = "res://addons/gdUnit4/test/dotnet/ExampleTestSuite.cs",
            ["fully_qualified_name"] = "gdUnit4.addons.gdUnit4.test.dotnet.ExampleTestSuite.IsFoo",
            ["simple_name"] = "IsFoo",
            ["managed_type"] = "gdUnit4.addons.gdUnit4.test.dotnet.ExampleTestSuite"
        });

    [TestCase]
    public void ContainsSame()
    {
        var current = new object[] { new SimpleObject("A1"), new SimpleObject("A2"), new SimpleObject("A3"), new SimpleObject("A4") };
        DoContainsSame(current, new SimpleObject("A1"));
    }

    [TestCase]
    [DataPoint(nameof(TestValues))]
    public void ContainsExactly(string name, dynamic current, dynamic copy, dynamic empty, dynamic obj5)
        => DoContainsExactly(current, obj5);

    [TestCase]
    [DataPoint(nameof(TestValues))]
    public void ContainsSameExactly(string name, dynamic current, dynamic copy, dynamic empty, dynamic obj5)
        => DoContainsSameExactly(current);

    [TestCase]
    [DataPoint(nameof(TestValues))]
    public void ContainsExactlyInAnyOrder(string name, dynamic current, dynamic copy, dynamic empty, dynamic obj5)
        => DoContainsExactlyInAnyOrder(current, obj5);

    [TestCase]
    [DataPoint(nameof(TestValues))]
    public void ContainsSameExactlyInAnyOrder(string name, dynamic current, dynamic copy, dynamic empty, dynamic obj5)
        => DoContainsSameExactlyInAnyOrder(current, obj5);

    [TestCase]
    [DataPoint(nameof(TestValues))]
    public void NotContainsOnObject(string name, dynamic current, dynamic copy, dynamic empty, dynamic obj5)
        => DoNotContainsOnObject(current, obj5);

    [TestCase]
    [DataPoint(nameof(TestValues))]
    public void NotContainsSameOnObject(string name, dynamic current, dynamic copy, dynamic empty, dynamic obj5)
        => DoNotContainsSameOnObject(current, obj5);

    [TestCase]
    public void OverrideFailureMessage()
        => AssertThrown(() => AssertArray([])
                .OverrideFailureMessage("Custom failure message")
                .IsNull())
            .IsInstanceOf<TestFailedException>()
            .HasMessage("Custom failure message");

    [TestCase]
    public void AppendFailureMessage()
        => AssertThrown(() => AssertArray([])
                .AppendFailureMessage("custom data")
                .IsNull())
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting be <Null>:
                         but is
                            <Empty>

                        Additional info:
                        custom data
                        """);

    [TestCase]
    public void MethodChainingBaseAssert()
    {
        var array = new object[] { 1, "hello", 3.14, true };
        AssertThat(array).IsNotNull().Contains("hello").HasSize(4);
        AssertThat(array).Contains("hello").IsNotNull().HasSize(4);
    }

    [TestCase]
    public void ExtractInvalidMethod()
        => AssertArray([new TestObj("root_a", null)])
            .Extract("NotExistMethod")
            .ContainsExactly("n.a.");

    [TestCase]
    public void Extract()
    {
        // extracting by a func without arguments
        AssertArray([new object(), 2, default(DateTime)])
            .Extract("GetType")
            .ContainsExactly(typeof(object), typeof(int), typeof(DateTime));

        // extracting by a func with arguments
        AssertArray([new SimpleObject("sig1"), 2, new SimpleObject("sig2")])
            .Extract("HasSignal", "sig2")
            .ContainsExactly(false, "n.a.", true);

        // try extract on object via a func that not exists
        AssertArray([new SimpleObject("sig1"), 2])
            .Extract("InvalidMethod")
            .ContainsExactly("n.a.", "n.a.");

        // try extract on object via a func that has no return value
        AssertArray([new SimpleObject("sig1"), 2])
            .Extract("RemoveMeta", string.Empty)
            .ContainsExactly(null, "n.a.");

        // must fail we can't extract from a null instance
        AssertThrown(() => AssertArray(null)
                .Extract("GetClass").ContainsExactly("AStar", "Node"))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting contains exactly elements:
                            <Null>
                         do contains (in same order)
                            ["AStar", "Node"]
                         but some elements not found:
                            ["AStar", "Node"]
                        """);
    }

    [TestCase]
    public void ExtractChained()
    {
        var rootA = new TestObj("root_a", null);
        var objA = new TestObj("A", rootA);
        var objB = new TestObj("B", rootA);
        var objC = new TestObj("C", rootA);
        var rootB = new TestObj("root_b", rootA);
        var objX = new TestObj("X", rootB);
        var objY = new TestObj("Y", rootB);

        AssertArray(new object[] { objA, objB, objC, objX, objY })
            .Extract("GetValue.GetName")
            .ContainsExactly(
                "root_a",
                "root_a",
                "root_a",
                "root_b",
                "root_b");
    }

    [TestCase]
    public void ExtractV()
    {
        // tuple of two
        AssertArray([new TestObj("A", 10), new TestObj("B", "foo"), new object(), new TestObj("C", 11)])
            .ExtractV(Extr("GetName"), Extr("GetValue"))
            .ContainsExactly(Tuple("A", 10), Tuple("B", "foo"), Tuple("n.a.", "n.a."), Tuple("C", 11));

        // tuple of three
        AssertArray(new object[] { new TestObj("A", 10), new TestObj("B", "foo", "bar"), new TestObj("C", 11, 42) })
            .ExtractV(Extr("GetName"), Extr("GetValue"), Extr("GetX"))
            .ContainsExactly(Tuple("A", 10, null), Tuple("B", "foo", "bar"), Tuple("C", 11, 42));

        AssertThrown(() => AssertArray(null)
                .ExtractV(Extr("GetName"), Extr("GetValue"), Extr("GetX"))
                .ContainsExactly(Tuple("A", 10, null)))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting contains exactly elements:
                            <Null>
                         do contains (in same order)
                            [tuple("A", 10, <Null>)]
                         but some elements not found:
                            [tuple("A", 10, <Null>)]
                        """);
    }

    [TestCase]
    public void ExtractVChained()
    {
        var rootA = new TestObj("root_a", null);
        var objA = new TestObj("A", rootA);
        var objB = new TestObj("B", rootA);
        var objC = new TestObj("C", rootA);
        var rootB = new TestObj("root_b", rootA);
        var objX = new TestObj("X", rootB);
        var objY = new TestObj("Y", rootB);

        AssertArray(new object[] { objA, objB, objC, objX, objY })
            .ExtractV(Extr("GetName"), Extr("GetValue.GetName"))
            .ContainsExactly(
                Tuple("A", "root_a"),
                Tuple("B", "root_a"),
                Tuple("C", "root_a"),
                Tuple("X", "root_b"),
                Tuple("Y", "root_b"));
    }

    [TestCase]
    public void ExtractVManyArgs()
        => AssertArray(new object[]
            {
                new TestObj("A", 10),
                new TestObj("B", "foo", "bar"),
                new TestObj("C", 11, 42)
            })
            .ExtractV(
                Extr("GetName"),
                Extr("GetX1"),
                Extr("GetX2"),
                Extr("GetX3"),
                Extr("GetX4"),
                Extr("GetX5"),
                Extr("GetX6"),
                Extr("GetX7"),
                Extr("GetX8"),
                Extr("GetX9"))
            .ContainsExactly(
                Tuple("A", "x1", "x2", "x3", "x4", "x5", "x6", "x7", "x8", "x9"),
                Tuple("B", "x1", "x2", "x3", "x4", "x5", "x6", "x7", "x8", "x9"),
                Tuple("C", "x1", "x2", "x3", "x4", "x5", "x6", "x7", "x8", "x9"));
}

// ReSharper disable once PartialTypeWithSinglePart
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable UnusedMember.Local
internal sealed class TestObj
{
    private readonly string name;
    private readonly object? value;
    private readonly object? x;

    public TestObj(string name, object? value, object? x = null)
    {
        this.name = name;
        this.value = value;
        this.x = x;
    }

    public string GetName() => name;

    public object? GetValue() => value;

    public object? GetX() => x;

    public string GetX1() => "x1";

    public string GetX2() => "x2";

    public string GetX3() => "x3";

    public string GetX4() => "x4";

    public string GetX5() => "x5";

    public string GetX6() => "x6";

    public string GetX7() => "x7";

    public string GetX8() => "x8";

    public string GetX9() => "x9";
}

internal class SimpleObject(string name)
{
    internal string Name { get; } = name;

    public bool HasSignal(string name)
        => name == Name;

    public void RemoveMeta(string value)
    {
    }
}

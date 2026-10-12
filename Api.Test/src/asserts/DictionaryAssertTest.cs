// GdUnit generated TestSuite

namespace GdUnit4.Tests.Asserts;

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;

using GdUnit4.Asserts;
using GdUnit4.Core.Execution.Exceptions;
using GdUnit4.Core.Extensions;

using Godot;
using Godot.Collections;

using static Assertions;

[TestSuite]
public class DictionaryAssertTest
{
    // TODO: replace it by https://github.com/MikeSchulze/gdUnit4Net/issues/46
    private static readonly object[] TestDataPointA =
    [

        // system dictionary types
        new object[] { new Hashtable { { "a1", new object() } } }, new object[] { new System.Collections.Generic.Dictionary<string, object> { { "a1", new object() } } },
        new object[] { new SortedDictionary<string, object> { { "a1", new object() } } },

        // Godot dictionary types
        new object[] { new Godot.Collections.Dictionary<string, Variant> { { "a1", new RefCounted() } } }, new object[] { new Dictionary { { "a1", new RefCounted() } } }
    ];

    // TODO: replace it by https://github.com/MikeSchulze/gdUnit4Net/issues/46
    public static readonly object[] TestDataPointB =
    [

        // system dictionary types
        new object[]
        {
            new Hashtable
            {
                { "a1", 100 },
                { "a2", 200 }
            },
            new Hashtable
            {
                { "a1", 100 },
                { "a2", 200 }
            }
        },
        new object[]
        {
            new System.Collections.Generic.Dictionary<string, long>
            {
                { "a1", 100 },
                { "a2", 200 }
            },
            new System.Collections.Generic.Dictionary<string, long>
            {
                { "a1", 100 },
                { "a2", 200 }
            }
        },
        new object[]
        {
            new SortedDictionary<string, long>
            {
                { "a1", 100 },
                { "a2", 200 }
            },
            new SortedDictionary<string, long>
            {
                { "a1", 100 },
                { "a2", 200 }
            }
        },

        // Godot dictionary types
        new object[]
        {
            new Dictionary
            {
                { "a1", 100 },
                { "a2", 200 }
            },
            new Dictionary
            {
                { "a1", 100 },
                { "a2", 200 }
            }
        },
        new object[]
        {
            new Godot.Collections.Dictionary<string, long>
            {
                { "a1", 100 },
                { "a2", 200 }
            },
            new Godot.Collections.Dictionary<string, long>
            {
                { "a1", 100 },
                { "a2", 200 }
            }
        }
    ];

    private static readonly string[] TestKeys = [new("a1"), new("aa2"), new("aaa3"), new("aaaa4")];

    private static readonly object[] TestDataPointKeys =
    [

        // system dictionary types
        new ListDictionary
        {
            { TestKeys[0], 100 },
            { TestKeys[1], 200 }
        },
        new System.Collections.Generic.Dictionary<string, int>
        {
            { TestKeys[0], 100 },
            { TestKeys[1], 200 }
        },

        // Godot dictionary types
        new Dictionary
        {
            { TestKeys[0], 100 },
            { TestKeys[1], 200 }
        },
        new Godot.Collections.Dictionary<Variant, int>
        {
            { TestKeys[0], 100 },
            { TestKeys[1], 200 }
        }
    ];

    [TestCase]
    [RequireGodotRuntime]
    public void VerifyDictionaryTypes()
    {
        AssertThat(new Hashtable()).IsEmpty();
        AssertThat(new System.Collections.Generic.Dictionary<string, long>()).IsEmpty();
        AssertThat(new SortedDictionary<string, object>()).IsEmpty();
        AssertThat(ImmutableDictionary.Create<string, long>()).IsEmpty();
        AssertThat(new Dictionary()).IsEmpty();
        AssertThat(new Godot.Collections.Dictionary<string, Variant>()).IsEmpty();
    }

    [TestCase]
    public void OverrideFailureMessage() =>
        AssertThrown(() => AssertThat((IDictionary?)null)
                .OverrideFailureMessage("Custom failure message")
                .IsNotNull())
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(152)
            .HasMessage("Custom failure message");

    [TestCase]
    public void AppendFailureMessage()
        => AssertThrown(() => AssertThat((IDictionary?)null)
                .AppendFailureMessage("custom data")
                .IsNotNull())
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(161)
            .HasMessage("""
                        Expecting be NOT <Null>:

                        Additional info:
                        custom data
                        """);

    [TestCase]
    public void IsEqualHashtable()
    {
        var current = new SortedDictionary<string, object>();
        var expected = new SortedDictionary<string, object>();
        AssertThat(current).IsEqual(expected);

        current = new SortedDictionary<string, object>
        {
            { "a1", "100" },
            { "a2", "200" }
        };
        expected = new SortedDictionary<string, object>
        {
            { "a1", "100" },
            { "a2", "200" }
        };
        AssertThat(current).IsEqual(expected);

        current.Add("a3", 300);
        AssertThrown(() => AssertThat(current).IsEqual(expected))
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(193)
            .HasMessage("""
                        Expecting be equal:
                            {"a1", "100"}; {"a2", "200"}
                         but is
                            {"a1", "100"}; {"a2", "200"}; {"a3", 300}
                        """);
        AssertThrown(() => AssertThat(new Hashtable()).IsEqual(expected))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting be equal:
                            {"a1", "100"}; {"a2", "200"}
                         but is
                            <Empty>
                        """);
    }

    [TestCase]
    public void IsEqualDictionary()
    {
        var current = new System.Collections.Generic.Dictionary<string, long>();
        var expected = new System.Collections.Generic.Dictionary<string, long>();
        AssertThat(current).IsEqual(expected);

        current = new System.Collections.Generic.Dictionary<string, long>
        {
            { "a1", 100 },
            { "a2", 200 }
        };
        expected = new System.Collections.Generic.Dictionary<string, long>(current);
        AssertThat(current).IsEqual(expected);

        current.Add("a3", 300);
        AssertThrown(() => AssertThat(current).IsEqual(expected))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting be equal:
                            {"a1", 100}; {"a2", 200}
                         but is
                            {"a1", 100}; {"a2", 200}; {"a3", 300}
                        """);
        AssertThrown(() => AssertThat((System.Collections.Generic.Dictionary<string, long>?)null).IsEqual(expected))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting be equal:
                            {"a1", 100}; {"a2", 200}
                         but is
                            <Null>
                        """);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void IsEqualGodotDictionary()
    {
        var current = new Dictionary();
        var expected = new Dictionary();
        AssertThat(current).IsEqual(expected);

        current = new Dictionary
        {
            { "a1", "100" },
            { "a2", "200" }
        };
        expected = current.Duplicate();
        AssertThat(current).IsEqual(expected);

        current.Add("a3", 300);
        AssertThrown(() => AssertThat(current).IsEqual(expected))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting be equal:
                            {"a1", "100"}; {"a2", "200"}
                         but is
                            {"a1", "100"}; {"a2", "200"}; {"a3", 300}
                        """);
        AssertThrown(() => AssertThat((Dictionary?)null).IsEqual(expected))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting be equal:
                            {"a1", "100"}; {"a2", "200"}
                         but is
                            <Null>
                        """);
    }

    [TestCase]
    public void IsNotEqualHashtable()
    {
        var current = new SortedDictionary<string, object>
        {
            { "a1", "100" },
            { "a2", "200" }
        };
        var expected = new SortedDictionary<string, object>
        {
            { "a1", "101" },
            { "a2", "200" }
        };
        AssertThat(current).IsNotEqual(expected);

        expected = new SortedDictionary<string, object>(current);
        AssertThrown(() => AssertThat(current).IsNotEqual(expected))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting be NOT equal:
                            {"a1", "100"}; {"a2", "200"}
                         but is
                            {"a1", "100"}; {"a2", "200"}
                        """);
    }

    [TestCase]
    public void IsNotEqualDictionary()
    {
        var current = new System.Collections.Generic.Dictionary<string, long>
        {
            { "a1", 100 },
            { "a2", 200 }
        };
        var expected = new System.Collections.Generic.Dictionary<string, long>
        {
            { "a1", 101 },
            { "a2", 200 }
        };
        AssertThat(current).IsNotEqual(expected);

        expected = new System.Collections.Generic.Dictionary<string, long>(current);
        AssertThrown(() => AssertThat(current).IsNotEqual(expected))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting be NOT equal:
                            {"a1", 100}; {"a2", 200}
                         but is
                            {"a1", 100}; {"a2", 200}
                        """);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void IsNotEqualGodotDictionary()
    {
        var current = new Dictionary
        {
            { "a1", "100" },
            { "a2", "200" }
        };
        var expected = new Dictionary
        {
            { "a1", "101" },
            { "a2", "200" }
        };
        AssertThat(current).IsNotEqual(expected);

        expected = current.Duplicate();
        AssertThrown(() => AssertThat(current).IsNotEqual(expected))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting be NOT equal:
                            {"a1", "100"}; {"a2", "200"}
                         but is
                            {"a1", "100"}; {"a2", "200"}
                        """);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void IsNull()
    {
        AssertThat((Hashtable?)null).IsNull();
        AssertThat((System.Collections.Generic.Dictionary<string, long>?)null).IsNull();
        AssertThat((Dictionary?)null).IsNull();

        AssertThrown(() => AssertThat(new Hashtable()).IsNull())
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting be <Null>:
                         but is
                            <Empty>
                        """);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void IsNotNull()
    {
        AssertThat(new Hashtable()).IsNotNull();
        AssertThat(new System.Collections.Generic.Dictionary<string, long>()).IsNotNull();
        AssertThat(new Dictionary()).IsNotNull();

        AssertThrown(() => AssertThat((IDictionary?)null).IsNotNull())
            .IsInstanceOf<TestFailedException>()
            .HasMessage("Expecting be NOT <Null>:");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void IsEmpty()
    {
        AssertThat(new Hashtable()).IsEmpty();
        AssertThat(new System.Collections.Generic.Dictionary<string, long>()).IsEmpty();
        AssertThat(new Dictionary()).IsEmpty();

        var expected = new System.Collections.Generic.Dictionary<string, long>
        {
            { "a1", 100 },
            { "a2", 200 }
        };
        AssertThrown(() => AssertThat(expected).IsEmpty())
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting be empty:
                         but has size '2'
                        """);
        AssertThrown(() => AssertThat((IDictionary?)null).IsEmpty())
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting be empty:
                         but is <Null>
                        """);
    }

    [TestCase]
    public void IsNotEmpty()
    {
        var current = new System.Collections.Generic.Dictionary<string, long>
        {
            { "a1", 100 },
            { "a2", 200 }
        };
        AssertThat(current).IsNotEmpty();

        AssertThrown(() => AssertThat(new Hashtable()).IsNotEmpty())
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting being NOT empty:
                         but is empty
                        """);
        AssertThrown(() => AssertThat((IDictionary?)null).IsNotEmpty())
            .IsInstanceOf<TestFailedException>()
            .HasMessage("Expecting be NOT <Null>:");
    }

    [TestCase]
    public void HasSize()
    {
        var current = new System.Collections.Generic.Dictionary<string, long>
        {
            { "a1", 100 },
            { "a2", 200 }
        };
        AssertThat(current).HasSize(2);
        AssertThat(new Hashtable()).HasSize(0);

        AssertThrown(() => AssertThat(current).HasSize(10))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting size:
                            '10' but is {"a1", 100}; {"a2", 200}
                        """);
        AssertThrown(() => AssertThat((IDictionary?)null).HasSize(10))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("Expecting be NOT <Null>:");
    }

    [TestCase]
    public void ContainsKeys()
    {
        var current = new System.Collections.Generic.Dictionary<string, long>
        {
            { "a1", 100 },
            { "a2", 200 }
        };
        AssertThat(current).ContainsKeys("a1", "a2");
        AssertThat(current).ContainsKeys(new List<string>
        {
            "a1",
            "a2"
        });

        AssertThrown(() => AssertThat(current).ContainsKeys("a4", "a2", "a3", "a1"))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting contains elements:
                            ["a1", "a2"]
                         do contains (in any order)
                            ["a4", "a2", "a3", "a1"]
                         but could not find elements:
                            ["a4", "a3"]
                        """);
        AssertThrown(() => AssertThat(current).ContainsKeys(new List<string>
            {
                "a4",
                "a2",
                "a3",
                "a1"
            }))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting contains elements:
                            ["a1", "a2"]
                         do contains (in any order)
                            ["a4", "a2", "a3", "a1"]
                         but could not find elements:
                            ["a4", "a3"]
                        """);
        AssertThrown(() => AssertThat((IDictionary?)null).ContainsKeys("a1"))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("Expecting be NOT <Null>:");
    }

    [TestCase(0, TestName = "IDictionary")]
    [TestCase(1, TestName = "IDictionary<string, int>")]
    [TestCase(2, TestName = "GodotDictionary")]
    [TestCase(3, TestName = "GodotDictionary<string, int>")]
    [RequireGodotRuntime]
    public void ContainsSameKeys(int dataPointIndex)
    {
        dynamic current = TestDataPointKeys[dataPointIndex];
        var key1 = TestKeys[0];
        var key2 = TestKeys[1];
        var key3 = TestKeys[2];
        var key4 = TestKeys[3];
        AssertThat(current).ContainsSameKeys(key1, key2);

        // we handle strings by equal regardless of object reference
        var key_1 = new string(key1.ToCharArray());
        AssertThat(current).ContainsSameKeys(key_1, key2);

        AssertThrown(() => AssertThat(current).ContainsSameKeys(key4, key2, key3, key1))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting contains elements:
                    [$key1, $key2]
                 do contains (in any order)
                    [$key4, $key2, $key3, $key1]
                 but could not find elements:
                    [$key4, $key3]
                """
                .Replace("$key1", key1.Formatted())
                .Replace("$key2", key2.Formatted())
                .Replace("$key3", key3.Formatted())
                .Replace("$key4", key4.Formatted()));
        AssertThrown(() => AssertThat((IDictionary?)null).ContainsSameKeys(key1))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("Expecting be NOT <Null>:");
    }

    [TestCase]
    public void NotContainsKeys()
    {
        var current = new System.Collections.Generic.Dictionary<string, long>
        {
            { "a1", 100 },
            { "a2", 200 }
        };
        AssertThat(current).NotContainsKeys("b1", "a3");
        AssertThat(current).NotContainsKeys(new List<string>
        {
            "b1",
            "a3"
        });

        AssertThrown(() => AssertThat(current).NotContainsKeys("a4", "a2", "a3"))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting:
                            ["a1", "a2"]
                         do NOT contains (in any order)
                            ["a4", "a2", "a3"]
                         but found elements:
                            ["a2"]
                        """);
        AssertThrown(() => AssertThat(current).NotContainsKeys(new List<string>
            {
                "a4",
                "a2",
                "a3"
            }))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting:
                            ["a1", "a2"]
                         do NOT contains (in any order)
                            ["a4", "a2", "a3"]
                         but found elements:
                            ["a2"]
                        """);
        AssertThrown(() => AssertThat((IDictionary?)null).NotContainsKeys("a1"))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("Expecting be NOT <Null>:");
    }

    [TestCase(0, TestName = "IDictionary")]
    [TestCase(1, TestName = "IDictionary<string, int>")]
    [TestCase(2, TestName = "GodotDictionary")]
    [TestCase(3, TestName = "GodotDictionary<string, int>")]
    [RequireGodotRuntime]
    public void NotContainsSameKeys(int dataPointIndex)
    {
        dynamic current = TestDataPointKeys[dataPointIndex];
        var key1 = TestKeys[0];
        var key2 = TestKeys[1];
        var key3 = TestKeys[2];
        var key4 = TestKeys[3];
        AssertThat(current).NotContainsSameKeys(key3, key4);

        // we handle strings by equal regardless of object reference
        var key_3 = new string(key3.ToCharArray());
        AssertThat(current).NotContainsSameKeys(key_3, key4);

        AssertThrown(() => AssertThat(current).NotContainsSameKeys(key4, key2, key3, key1))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting:
                    [$key1, $key2]
                 do NOT contains (in any order)
                    [$key4, $key2, $key3, $key1]
                 but found elements:
                    [$key2, $key1]
                """
                .Replace("$key1", key1.Formatted())
                .Replace("$key2", key2.Formatted())
                .Replace("$key3", key3.Formatted())
                .Replace("$key4", key4.Formatted()));
        AssertThrown(() => AssertThat((IDictionary?)null).NotContainsSameKeys(key1))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("Expecting be NOT <Null>:");
    }

    [TestCase]
    public void ContainsKeyValue()
    {
        var current = new System.Collections.Generic.Dictionary<string, long>
        {
            { "a1", 100 },
            { "a2", 200 }
        };
        AssertThat(current).ContainsKeyValue("a1", 100L);

        AssertThrown(() => AssertThat(current).ContainsKeyValue("a1", 200L))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting do contain entry:
                            {"a1", 200}
                         found key but value is
                            '100'
                        """);
        AssertThrown(() => AssertThat(current).ContainsKeyValue("a3", 300L))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting do contain entry:
                            {"a3", 300}
                        """);
        AssertThrown(() => AssertThat((IDictionary?)null).ContainsKeyValue("a1", 200L))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("Expecting be NOT <Null>:");
    }

    [TestCase(0, TestName = "Hashtable")]
    [TestCase(1, TestName = "Dictionary<string, object>")]
    [TestCase(2, TestName = "SortedDictionary<string, object>")]
    [TestCase(3, TestName = "GodotDictionary<string, Godot.Variant>")]
    [TestCase(4, TestName = "GodotDictionary")]
    [RequireGodotRuntime]
    public void ContainsSameKeyValue(int dataPointIndex)
    {
        var dataPoint = TestDataPointA[dataPointIndex] as object[];
        dynamic current = dataPoint![0];
        var same = current["a1"];
        var notSame = new RefCounted();

        AssertThat(current).ContainsSameKeyValue("a1", same);

        AssertThrown(() => AssertThat(current).ContainsSameKeyValue("a1", notSame))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting do contain entry:
                    {"a1", $obj1}
                 found key but value is
                    $obj2
                """
                .Replace("$obj1", AssertFailures.AsObjectId(notSame))
                .Replace("$obj2", AssertFailures.AsObjectId(same)));
        AssertThrown(() => AssertThat(current).ContainsSameKeyValue("a3", notSame))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting do contain entry:
                            {"a3", $obj1}
                        """.Replace("$obj1", AssertFailures.AsObjectId(notSame)));
    }

    [TestCase(0, TestName = "Hashtable")]
    [TestCase(1, TestName = "Dictionary<string, long>")]
    [TestCase(2, TestName = "SortedDictionary<string, long>")]
    [TestCase(3, TestName = "GodotCollectionsDictionary")]
    [TestCase(4, TestName = "GodotCollectionsDictionary<string, long>")]
    [RequireGodotRuntime]
    public void IsSame(int dataPointIndex)
    {
        var dataPoint = TestDataPointB[dataPointIndex] as object[];
        dynamic current = dataPoint![0];
        dynamic other = dataPoint[1];

        AssertThat(current).IsSame(current);
        AssertThrown(() => AssertThat(current).IsSame(other))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting be same:
                            {"a1", 100}; {"a2", 200}
                         to refer to the same object
                            {"a1", 100}; {"a2", 200}
                        """);
    }

    [TestCase(0, TestName = "Hashtable")]
    [TestCase(1, TestName = "Dictionary<string, long>")]
    [TestCase(2, TestName = "SortedDictionary<string, long>")]
    [TestCase(3, TestName = "GodotCollectionsDictionary")]
    [TestCase(4, TestName = "GodotCollectionsDictionary<string, long>")]
    [RequireGodotRuntime]
    public void IsNotSame(int dataPointIndex)
    {
        var dataPoint = TestDataPointB[dataPointIndex] as object[];
        dynamic current = dataPoint![0];
        dynamic other = dataPoint[1];

        AssertThat(current).IsNotSame(other);
        AssertThrown(() => AssertThat(current).IsNotSame(current))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting be same:
                            {"a1", 100}; {"a2", 200}
                         to refer to the same object
                            {"a1", 100}; {"a2", 200}
                        """);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void MethodChainingBaseAssert()
    {
        var dictionary = new Godot.Collections.Dictionary<string, string>
        {
            { "name", "Player" }
        };
        AssertThat(dictionary).IsNotNull().NotContainsKeys("A").ContainsKeys("name");
        AssertThat(dictionary).NotContainsKeys("A").IsNotNull().ContainsKeys("name");
    }
}

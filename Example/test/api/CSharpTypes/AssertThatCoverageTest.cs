namespace Examples.Test.Api.CSharpTypes;

using System.Collections;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Numerics;

using GdUnit4;
using GdUnit4.Asserts;

using static GdUnit4.Assertions;

[TestSuite]
public class AssertThatCoverageTest
{
    [TestCase]
    public void Primitives()
    {
        AssertObject(AssertThat(true)).IsInstanceOf<IBoolAssert>();
        AssertObject(AssertThat("foo")).IsInstanceOf<IStringAssert>();
        AssertObject(AssertThat(sbyte.MaxValue)).IsInstanceOf<INumberAssert<sbyte>>();
        AssertObject(AssertThat(byte.MaxValue)).IsInstanceOf<INumberAssert<byte>>();
        AssertObject(AssertThat(short.MaxValue)).IsInstanceOf<INumberAssert<short>>();
        AssertObject(AssertThat(ushort.MaxValue)).IsInstanceOf<INumberAssert<ushort>>();
        AssertObject(AssertThat(int.MaxValue)).IsInstanceOf<INumberAssert<int>>();
        AssertObject(AssertThat(uint.MaxValue)).IsInstanceOf<INumberAssert<uint>>();
        AssertObject(AssertThat(long.MaxValue)).IsInstanceOf<INumberAssert<long>>();
        AssertObject(AssertThat(ulong.MaxValue)).IsInstanceOf<INumberAssert<ulong>>();
        AssertObject(AssertThat(float.MaxValue)).IsInstanceOf<INumberAssert<float>>();
        AssertObject(AssertThat(double.MaxValue)).IsInstanceOf<INumberAssert<double>>();
        AssertObject(AssertThat(decimal.MaxValue)).IsInstanceOf<INumberAssert<decimal>>();

        // Using object type for assert dynamic primitives
        AssertThat(true as object).IsTrue();
        AssertObject(AssertThat(true as object)).IsInstanceOf<IBoolAssert>();
        AssertObject(AssertThat(sbyte.MaxValue as object)).IsInstanceOf<INumberAssert<sbyte>>();
        AssertObject(AssertThat(byte.MaxValue as object)).IsInstanceOf<INumberAssert<byte>>();
        AssertObject(AssertThat(short.MaxValue as object)).IsInstanceOf<INumberAssert<short>>();
        AssertObject(AssertThat(ushort.MaxValue as object)).IsInstanceOf<INumberAssert<ushort>>();
        AssertObject(AssertThat(int.MaxValue as object)).IsInstanceOf<INumberAssert<int>>();
        AssertObject(AssertThat(uint.MaxValue as object)).IsInstanceOf<INumberAssert<uint>>();
        AssertObject(AssertThat(long.MaxValue as object)).IsInstanceOf<INumberAssert<long>>();
        AssertObject(AssertThat(ulong.MaxValue as object)).IsInstanceOf<INumberAssert<ulong>>();
        AssertObject(AssertThat(float.MaxValue as object)).IsInstanceOf<INumberAssert<float>>();
        AssertObject(AssertThat(double.MaxValue as object)).IsInstanceOf<INumberAssert<double>>();
        AssertObject(AssertThat(decimal.MaxValue as object)).IsInstanceOf<INumberAssert<decimal>>();
    }

    [TestCase]
    public void Structs()
    {
        AssertObject(AssertThat(TimeSpan.FromMilliseconds(124))).IsInstanceOf<IObjectAssert<TimeSpan>>();
        AssertObject(AssertThat(new KeyValuePair<string, object>())).IsInstanceOf<IObjectAssert<KeyValuePair<string, object>>>();
    }

    [TestCase]
    public void Classes()
    {
        AssertObject(AssertThat(new object())).IsInstanceOf<IObjectAssert<object>>();
        AssertObject(AssertThat(new Player("Mage", 10, 100.0f, true))).IsInstanceOf<IObjectAssert<Player>>();
    }

    [TestCase]
    public void Vectors()
    {
        AssertObject(AssertThat(Vector2.One)).IsInstanceOf<IVectorAssert<Vector2>>();
        AssertObject(AssertThat(Vector3.One)).IsInstanceOf<IVectorAssert<Vector3>>();
        AssertObject(AssertThat(Vector4.One)).IsInstanceOf<IVectorAssert<Vector4>>();
    }

    [TestCase]
    public void Arrays()
    {
        AssertObject(AssertThat(Array.Empty<object>())).IsInstanceOf<IEnumerableAssert<object>>();
        AssertObject(AssertThat(Array.Empty<byte>())).IsInstanceOf<IEnumerableAssert<byte>>();
        AssertObject(AssertThat(Array.Empty<string>())).IsInstanceOf<IEnumerableAssert<string>>();
        AssertObject(AssertThat(new object[] { 1, "hello", 3.14, true })).IsInstanceOf<IEnumerableAssert<object>>();
        AssertObject(AssertThat(new List<object>())).IsInstanceOf<IEnumerableAssert<object>>();
        AssertObject(AssertThat(new List<int>())).IsInstanceOf<IEnumerableAssert<int>>();
        AssertObject(AssertThat(new ArrayList())).IsInstanceOf<IEnumerableAssert<object>>();
        AssertObject(AssertThat(ImmutableArray.Create<object>())).IsInstanceOf<IEnumerableAssert<object>>();
        AssertObject(AssertThat(ImmutableArray.Create<int>())).IsInstanceOf<IEnumerableAssert<int>>();
        AssertObject(AssertThat(new BitArray(0, false))).IsInstanceOf<IEnumerableAssert<object>>();
        AssertObject(AssertThat(new BitArray([true, false]))).IsInstanceOf<IEnumerableAssert<object>>();
        AssertObject(AssertThat(new HashSet<object>())).IsInstanceOf<IEnumerableAssert<object>>();
        AssertObject(AssertThat(new HashSet<int>())).IsInstanceOf<IEnumerableAssert<int>>();
    }

    [TestCase]
    public void Lists()
    {
        AssertObject(AssertThat(new List<string>())).IsInstanceOf<IEnumerableAssert<string>>();
        AssertObject(AssertThat(new List<int>())).IsInstanceOf<IEnumerableAssert<int>>();
        AssertObject(AssertThat(new List<object>())).IsInstanceOf<IEnumerableAssert<object>>();
        AssertObject(AssertThat(ImmutableList.Create<string>())).IsInstanceOf<IEnumerableAssert<string>>();
        AssertObject(AssertThat(ImmutableList.Create<object>())).IsInstanceOf<IEnumerableAssert<object>>();
    }

    [TestCase]
    public void Dictionary()
    {
        AssertObject(AssertThat(new ListDictionary())).IsInstanceOf<IDictionaryAssert<object, object>>();
        AssertObject(AssertThat(new Hashtable())).IsInstanceOf<IDictionaryAssert<object, object>>();
        AssertObject(AssertThat(new HybridDictionary())).IsInstanceOf<IDictionaryAssert<object, object>>();
        AssertObject(AssertThat(new Dictionary<string, object>())).IsInstanceOf<IDictionaryAssert<string, object>>();
        AssertObject(AssertThat(ImmutableDictionary.Create<string, object>())).IsInstanceOf<IDictionaryAssert<string, object>>();
    }
}

namespace Examples.Test.Api.GodotTypes;

using GdUnit4;
using GdUnit4.Asserts;

using Godot;
using Godot.Collections;

using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class AssertThatCoverageTest
{
    [TestCase]
    public void Variants()
    {
        // Godot Primitive Variants
        AssertObject(AssertThat(Variant.From(sbyte.MaxValue))).IsInstanceOf<INumberAssert<int>>();
        AssertObject(AssertThat(Variant.From(byte.MaxValue))).IsInstanceOf<INumberAssert<int>>();
        AssertObject(AssertThat(Variant.From(short.MaxValue))).IsInstanceOf<INumberAssert<int>>();
        AssertObject(AssertThat(Variant.From(ushort.MaxValue))).IsInstanceOf<INumberAssert<int>>();
        AssertObject(AssertThat(Variant.From(int.MaxValue))).IsInstanceOf<INumberAssert<int>>();
        AssertObject(AssertThat(Variant.From(uint.MaxValue))).IsInstanceOf<INumberAssert<int>>();
        AssertObject(AssertThat(Variant.From(long.MaxValue))).IsInstanceOf<INumberAssert<int>>();
        AssertObject(AssertThat(Variant.From(ulong.MaxValue))).IsInstanceOf<INumberAssert<int>>();
        AssertObject(AssertThat(Variant.From(float.MaxValue))).IsInstanceOf<INumberAssert<float>>();
        AssertObject(AssertThat(Variant.From(double.MaxValue))).IsInstanceOf<INumberAssert<float>>();
        // decimal is not supported, it has no Variant type

        AssertObject(AssertThat(Variant.From("foo"))).IsInstanceOf<IStringAssert>();
        AssertObject(AssertThat(Variant.From(new StringName("foo")))).IsInstanceOf<IStringAssert>();

        AssertObject(AssertThat(Variant.From(true))).IsInstanceOf<IBoolAssert>();

        // Godot Object Variants
        AssertObject(AssertThat(Variant.From(new RefCounted()))).IsInstanceOf<IObjectAssert<RefCounted>>();

        // Godot Struct Variants
        AssertObject(AssertThat(Variant.From(Vector3.One))).IsInstanceOf<IVectorAssert<Vector3>>();
    }

    [TestCase]
    public void Structs()
    {
        AssertObject(AssertThat(new Rect2())).IsInstanceOf<IObjectAssert<Rect2>>();
        AssertObject(AssertThat(new Color())).IsInstanceOf<IObjectAssert<Color>>();
    }

    [TestCase]
    public void Classes()
    {
        AssertObject(AssertThat(new GodotObject())).IsInstanceOf<IObjectAssert<GodotObject>>();
        AssertObject(AssertThat(AutoFree(new Node()))).IsInstanceOf<IObjectAssert<Node>>();
    }

    [TestCase]
    public void Vectors()
    {
        AssertObject(AssertThat(Vector2.One)).IsInstanceOf<IVectorAssert<Vector2>>();
        AssertObject(AssertThat(Vector2I.One)).IsInstanceOf<IVectorAssert<Vector2I>>();
        AssertObject(AssertThat(Vector3.One)).IsInstanceOf<IVectorAssert<Vector3>>();
        AssertObject(AssertThat(Vector3I.One)).IsInstanceOf<IVectorAssert<Vector3I>>();
        AssertObject(AssertThat(Vector4.One)).IsInstanceOf<IVectorAssert<Vector4>>();
        AssertObject(AssertThat(Vector4I.One)).IsInstanceOf<IVectorAssert<Vector4I>>();
    }

    [TestCase]
    public void Arrays()
    {
        AssertObject(AssertThat(new Array())).IsInstanceOf<IEnumerableAssert<Variant>>();
        AssertObject(AssertThat(new Array<GodotObject>())).IsInstanceOf<IEnumerableAssert<GodotObject>>();
        AssertObject(AssertThat(new Array<Variant>())).IsInstanceOf<IEnumerableAssert<Variant>>();
        AssertObject(AssertThat(new Array<int>())).IsInstanceOf<IEnumerableAssert<int>>();
    }

    [TestCase]
    public void Dictionaries()
    {
        AssertObject(AssertThat(new Dictionary())).IsInstanceOf<IDictionaryAssert<Variant, Variant>>();
        AssertObject(AssertThat(new Dictionary<string, GodotObject>())).IsInstanceOf<IDictionaryAssert<string, GodotObject>>();
        AssertObject(AssertThat(new Dictionary<string, Variant>())).IsInstanceOf<IDictionaryAssert<string, Variant>>();
        AssertObject(AssertThat(new Dictionary<string, string>())).IsInstanceOf<IDictionaryAssert<string, string>>();
    }
}

namespace GdUnit4.Tests.Asserts;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

using GdUnit4.Asserts;
using GdUnit4.Core.Execution;
using GdUnit4.Core.Execution.Exceptions;
using GdUnit4.Core.Extensions;

using Godot;
using Godot.Collections;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using static Assertions;

using Array = Godot.Collections.Array;

[TestSuite]
public class AssertionsTest
{
    [TestCase]
    public void DoAssertNotYetImplemented()
        => AssertThrown(AssertNotYetImplemented)
            .HasFileLineNumber(28)
            .HasMessage("Test not yet implemented!");

    [TestCase]
    [RequireGodotRuntime]
    public void AssertThatVariants()
    {
        // Godot number Variants
        AssertObject(AssertThat(Variant.From((sbyte)-1))).IsInstanceOf<INumberAssert<int>>();
        AssertObject(AssertThat(Variant.From(11))).IsInstanceOf<INumberAssert<int>>();
        AssertObject(AssertThat(Variant.From(12L))).IsInstanceOf<INumberAssert<int>>();
        AssertObject(AssertThat(Variant.From(1.4f))).IsInstanceOf<INumberAssert<float>>();
        AssertObject(AssertThat(Variant.From(1.5d))).IsInstanceOf<INumberAssert<float>>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void AssertThatPackedVector4ArrayVariant()
    {
        Vector4[] values = [new(1, 2, 3, 4), new(5, 6, 7, 8)];

        AssertThat(Variant.From(values)).IsEqual(values);
        AssertThrown(() => AssertThat(Variant.From(values)).IsEqual(new Vector4[] { new(1, 2, 3, 4) }))
            .IsInstanceOf<TestFailedException>();
    }

    [TestCase]
    public void AssertThatString()
    {
        AssertObject(AssertThat("abc")).IsInstanceOf<IStringAssert>();
        AssertObject(AssertThat(new string("abc"))).IsInstanceOf<IStringAssert>();
    }

    [TestCase]
    public void AssertThatBool()
    {
        AssertObject(AssertThat(true)).IsInstanceOf<IBoolAssert>();
        AssertObject(AssertThat(false)).IsInstanceOf<IBoolAssert>();
        AssertObject(AssertThat(default(bool))).IsInstanceOf<IBoolAssert>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void AssertThatEnumerable()
    {
        AssertObject(AssertThat(System.Array.Empty<byte>())).IsInstanceOf<IEnumerableAssert<byte>>();
        AssertObject(AssertThat(new ArrayList())).IsInstanceOf<IEnumerableAssert<object>>();
        AssertObject(AssertThat(new BitArray([true, false]))).IsInstanceOf<IEnumerableAssert<object>>();
        AssertObject(AssertThat(new HashSet<byte>())).IsInstanceOf<IEnumerableAssert<byte>>();
        AssertObject(AssertThat(new List<byte>())).IsInstanceOf<IEnumerableAssert<byte>>();
        AssertObject(AssertThat(new Array())).IsInstanceOf<IEnumerableAssert<Variant>>();
        AssertObject(AssertThat(new Array<int>())).IsInstanceOf<IEnumerableAssert<int>>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void AssertThatDictionary()
    {
        AssertObject(AssertThat(new Dictionary())).IsInstanceOf<IDictionaryAssert<Variant, Variant>>();
        AssertObject(AssertThat(new Godot.Collections.Dictionary<string, Variant>())).IsInstanceOf<IDictionaryAssert<string, Variant>>();
        AssertObject(AssertThat(new Godot.Collections.Dictionary<string, string>())).IsInstanceOf<IDictionaryAssert<string, string>>();
        AssertObject(AssertThat(new Hashtable())).IsInstanceOf<IDictionaryAssert<object, object>>();
        AssertObject(AssertThat(new System.Collections.Generic.Dictionary<string, object>())).IsInstanceOf<IDictionaryAssert<string, object>>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void AsObjectId()
    {
        var obj1 = new object();
        var obj2 = new object();

        AssertThat(AssertFailures.AsObjectId(null)).IsEqual("<Null>");
        AssertThat(AssertFailures.AsObjectId(obj1)).IsEqual($"<System.Object> (objId: {obj1.GetHashCode()})");
        AssertThat(AssertFailures.AsObjectId(obj1)).IsNotEqual(AssertFailures.AsObjectId(obj2));

        // on Godot Objects
        var obj3 = new RefCounted();
        AssertThat(AssertFailures.AsObjectId(obj3)).IsEqual($"<Godot.RefCounted> (objId: {obj3.GetInstanceId()})");
        var obj4 = new QuadMesh();
        AssertThat(AssertFailures.AsObjectId(obj4)).IsEqual($"<Godot.QuadMesh> (objId: {obj4.GetInstanceId()})");

        // on Godot Variants
        var obj5 = new RefCounted();
        AssertThat(AssertFailures.AsObjectId(obj5.ToVariant())).IsEqual($"<Godot.RefCounted> (objId: {obj5.GetInstanceId()})");

        object? obj6 = null;
        AssertThat(AssertFailures.AsObjectId(obj6.ToVariant())).IsEqual("<Godot.Variant> (Null)");

        // without overrides ToString
        var obj7 = new ClassWithoutToString("Custom");
        AssertThat(AssertFailures.AsObjectId(obj7)).IsEqual($"<GdUnit4.Tests.Asserts.AssertionsTest+ClassWithoutToString> (objId: {obj7.GetHashCode()})");

        // with overrides ToString
        var obj8 = new ClassWithToString("Custom");
        AssertThat(AssertFailures.AsObjectId(obj8)).IsEqual($"Custom (objId: {obj8.GetHashCode()})");
    }

    [TestCase]
    public void UsingMsTestAssertions()
    {
        var currentCulture = CultureInfo.DefaultThreadCurrentCulture;
        var currentUICulture = CultureInfo.DefaultThreadCurrentUICulture;

        try
        {
            // we want to test against english culture
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
            var assertion = AssertThrown(() => Assert.AreEqual(1, 2))
                .IsInstanceOf<AssertFailedException>()
                .StartsWithMessage("Assert.AreEqual failed. Expected:<1>. Actual:<2>.");
            AssertTrimmedExceptionStackTrace(assertion)
                .NotContains("Microsoft.VisualStudio.TestTools");
        }
        finally
        {
            CultureInfo.DefaultThreadCurrentCulture = currentCulture;
            CultureInfo.DefaultThreadCurrentUICulture = currentUICulture;
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public async Task TestCatchStdOutFromTestRun()
    {
        Console.WriteLine("Console.WriteLine: first message");
        GD.PrintS("Godot:PrintS: first message");

        Console.Write("Console.Write: message");
        Console.WriteLine("Console.Write: message");

        // do print to stdout by Godot's native functions
        GD.Print("Godot:Print: message");
        GD.PrintS("Godot:PrintS: message");
        GD.PrintT("Godot:PrintT: message");
        GD.PrintRaw("Godot:PrintRaw: message");
        GD.PrintRich("Godot:PrintRich: message");

        // std error will not be caught
        GD.PrintErr("Godot:PrintErr: message");

        // do just a godot sync
        await ISceneRunner.SyncProcessFrame;
        Console.WriteLine("Console.WriteLine: This is a test message 1");
        Console.WriteLine("Console.WriteLine: last message");
        GD.PrintS("Godot:PrintS last message");
    }

    private static IStringAssert AssertTrimmedExceptionStackTrace(IExceptionAssert? exceptionAssert)
    {
        var stackTrace = (exceptionAssert as ExceptionAssert<Exception>)?.GetExceptionStackTrace()!;
        var trimmedStackTrace = ExecutionStage<int>.TrimStackTrace(stackTrace);
        return AssertString(trimmedStackTrace);
    }

    private class ClassWithoutToString
    {
        public ClassWithoutToString(string value) => Value = value;

        protected string Value { get; }
    }

    private sealed class ClassWithToString : ClassWithoutToString
    {
        public ClassWithToString(string value)
            : base(value)
        {
        }

        public override string ToString() => $"{Value}";
    }
}

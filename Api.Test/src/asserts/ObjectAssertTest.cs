namespace GdUnit4.Tests.Asserts;

using System.Collections.Generic;

using GdUnit4.Asserts;
using GdUnit4.Core.Execution;
using GdUnit4.Core.Execution.Exceptions;

using Godot;

using static Assertions;

internal partial class CustomClass : RefCounted
{
    public partial class InnerClassA : Node
    {
    }

    public sealed partial class InnerClassB : InnerClassA
    {
    }

    public sealed partial class InnerClassC : Node
    {
    }
}

internal sealed partial class CustomClassB : CustomClass
{
}

[TestSuite]
public class ObjectAssertTest
{
    [TestCase]
    [RequireGodotRuntime]
    public void IsEqual()
    {
        var obj = new object();
        var boxMesh1 = new BoxMesh();
        var boxMesh2 = new BoxMesh
        {
            FlipFaces = true
        };
        AssertObject(boxMesh1).IsEqual(boxMesh1);
        AssertObject(boxMesh1).IsEqual(new BoxMesh());
        AssertObject(obj).IsEqual(obj);

        // should fail because the current is an BoxMesh and we expect equal to a BoxMesh with flipped faces
        AssertThrown(() => AssertObject(boxMesh1).IsEqual(boxMesh2))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting be equal:
                    $boxMesh2 but is $boxMesh1
                """
                .Replace("$boxMesh1", AssertFailures.AsObjectId(boxMesh1))
                .Replace("$boxMesh2", AssertFailures.AsObjectId(boxMesh2)));
        AssertThrown(() => AssertObject(obj).IsEqual(new List<int>()))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting be equal:
                    <Empty>
                 but is
                    $obj
                """
                .Replace("$obj", AssertFailures.AsObjectId(obj)));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void IsNotEqual()
    {
        var obj = new object();
        var boxMesh = new BoxMesh();
        var boxMesh2 = new BoxMesh
        {
            FlipFaces = true
        };
        AssertObject(boxMesh).IsNotEqual(boxMesh2);
        AssertObject(obj).IsNotEqual(new List<object>());

        // should fail because the current is an BoxMesh and we expect not equal to a BoxMesh
        AssertThrown(() => AssertObject(boxMesh).IsNotEqual(boxMesh))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting be NOT equal:
                    $boxMesh but is $boxMesh
                """
                .Replace("$boxMesh", AssertFailures.AsObjectId(boxMesh)));
        AssertThrown(() => AssertObject(obj).IsNotEqual(obj))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting be NOT equal:
                    $obj but is $obj
                """
                .Replace("$obj", AssertFailures.AsObjectId(obj)));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void IsInstanceOf()
    {
        // engine class test
        AssertObject(AutoFree(new Path2D())).IsInstanceOf<Node>();
        AssertObject(AutoFree(new Camera2D())).IsInstanceOf<Camera2D>();

        // script class test
        // inner class test
        AssertObject(AutoFree(new CustomClass.InnerClassA())).IsInstanceOf<Node>();
        AssertObject(AutoFree(new CustomClass.InnerClassB())).IsInstanceOf<CustomClass.InnerClassA>();

        // c# class
        AssertObject(new object()).IsInstanceOf<object>();
        AssertObject(string.Empty).IsInstanceOf<object>();
        AssertObject(new CustomClass()).IsInstanceOf<object>();
        AssertObject(new CustomClassB()).IsInstanceOf<object>();
        AssertObject(new CustomClassB()).IsInstanceOf<CustomClass>();

        // should fail because the current is not a instance of `Tree`
        AssertThrown(() => AssertObject(AutoFree(new Path2D())).IsInstanceOf<Tree>())
            .IsInstanceOf<TestFailedException>()
            .HasMessage("Expected be instance of:\n"
                        + "    <Godot.Tree> but is <Godot.Path2D>");
        AssertThrown(() => AssertObject(null).IsInstanceOf<Tree>())
            .IsInstanceOf<TestFailedException>()
            .HasMessage("Expected be instance of:\n"
                        + "    <Godot.Tree> but is <Null>");
        AssertThrown(() => AssertObject(new CustomClass()).IsInstanceOf<CustomClassB>())
            .IsInstanceOf<TestFailedException>()
            .HasMessage("Expected be instance of:\n"
                        + "    <GdUnit4.Tests.Asserts.CustomClassB> but is <GdUnit4.Tests.Asserts.CustomClass>");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void IsNotInstanceOf()
    {
        AssertObject(null).IsNotInstanceOf<Node>();

        // engine class test
        AssertObject(AutoFree(new Path2D())).IsNotInstanceOf<Tree>();

        // inner class test
        AssertObject(AutoFree(new CustomClass.InnerClassA())).IsNotInstanceOf<Tree>();
        AssertObject(AutoFree(new CustomClass.InnerClassB())).IsNotInstanceOf<CustomClass.InnerClassC>();

        // c# class
        AssertObject(new CustomClass()).IsNotInstanceOf<CustomClassB>();

        // should fail because the current is not a instance of `Tree`
        AssertThrown(() => AssertObject(AutoFree(new Path2D())).IsNotInstanceOf<Node>())
            .IsInstanceOf<TestFailedException>()
            .HasMessage("Expecting be NOT a instance of:\n"
                        + "    <Godot.Node>");
        AssertThrown(() => AssertObject(AutoFree(new CustomClassB())).IsNotInstanceOf<CustomClass>())
            .IsInstanceOf<TestFailedException>()
            .HasMessage("Expecting be NOT a instance of:\n"
                        + "    <GdUnit4.Tests.Asserts.CustomClass>");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void IsNull()
    {
        AssertObject(null).IsNull();

        // should fail because the current is not null
        AssertThrown(() => AssertObject(AutoFree(new Node())).IsNull())
            .IsInstanceOf<TestFailedException>()
            .StartsWithMessage("Expecting be <Null>:\n"
                               + " but is\n"
                               + "    <Godot.Node>");
        AssertThrown(() => AssertObject(new object()).IsNull())
            .IsInstanceOf<TestFailedException>()
            .StartsWithMessage("Expecting be <Null>:\n"
                               + " but is\n"
                               + "    <System.Object>");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void IsNotNull()
    {
        AssertObject(AutoFree(new Node())).IsNotNull();
        AssertObject(new object()).IsNotNull();

        // should fail because the current is null
        AssertThrown(() => AssertObject(null).IsNotNull())
            .IsInstanceOf<TestFailedException>()
            .HasMessage("Expecting be NOT <Null>:");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void IsSame()
    {
        var obj1 = AutoFree(new Node());
        var obj2 = obj1;
        var obj3 = AutoFree(obj1.Duplicate());
        AssertObject(obj1).IsSame(obj1);
        AssertObject(obj1).IsSame(obj2);
        AssertObject(obj2).IsSame(obj1);
        var o1 = new object();
        var o2 = o1;
        AssertObject(o1).IsSame(o1);
        AssertObject(o1).IsSame(o2);
        AssertObject(o2).IsSame(o1);

        AssertThrown(() => AssertObject(null).IsSame(obj1))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting be same:
                    $obj
                 to refer to the same object
                    <Null>
                """
                .Replace("$obj", AssertFailures.AsObjectId(obj1)));
        AssertThrown(() => AssertObject(obj1).IsSame(obj3))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting be same:
                    $obj3
                 to refer to the same object
                    $obj1
                """
                .Replace("$obj1", AssertFailures.AsObjectId(obj1))
                .Replace("$obj3", AssertFailures.AsObjectId(obj3)));
        AssertThrown(() => AssertObject(obj3).IsSame(obj1))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting be same:
                    $obj1
                 to refer to the same object
                    $obj3
                """
                .Replace("$obj1", AssertFailures.AsObjectId(obj1))
                .Replace("$obj3", AssertFailures.AsObjectId(obj3)));
        AssertThrown(() => AssertObject(obj3).IsSame(obj2))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting be same:
                    $obj2
                 to refer to the same object
                    $obj3
                """
                .Replace("$obj2", AssertFailures.AsObjectId(obj2))
                .Replace("$obj3", AssertFailures.AsObjectId(obj3)));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void IsNotSame()
    {
        var obj1 = AutoFree(new Node());
        var obj2 = obj1;
        var obj3 = AutoFree(obj1.Duplicate());
        AssertObject(null).IsNotSame(obj1);
        AssertObject(obj1).IsNotSame(obj3);
        AssertObject(obj3).IsNotSame(obj1);
        AssertObject(obj3).IsNotSame(obj2);

        var o1 = new object();
        var o2 = new object();
        AssertObject(null).IsNotSame(o1);
        AssertObject(o1).IsNotSame(o2);
        AssertObject(o2).IsNotSame(o1);

        AssertThrown(() => AssertObject(obj1).IsNotSame(obj1))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("Expecting be NOT same: $obj".Replace("$obj", AssertFailures.AsObjectId(obj1)));
        AssertThrown(() => AssertObject(obj1).IsNotSame(obj2))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("Expecting be NOT same: $obj".Replace("$obj", AssertFailures.AsObjectId(obj2)));
        AssertThrown(() => AssertObject(obj2).IsNotSame(obj1))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("Expecting be NOT same: $obj".Replace("$obj", AssertFailures.AsObjectId(obj1)));
    }

    [TestCase]
    public void MustFailIsPrimitive()
    {
        AssertThrown(() => AssertObject(1))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("ObjectAssert initial error: current is primitive <System.Int32>");
        AssertThrown(() => AssertObject(1.3))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("ObjectAssert initial error: current is primitive <System.Double>");
        AssertThrown(() => AssertObject(true))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("ObjectAssert initial error: current is primitive <System.Boolean>");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void OverrideFailureMessage()
        => AssertThrown(() => AssertObject(AutoFree(new Node()))
                .OverrideFailureMessage("Custom failure message")
                .IsNull())
            .IsInstanceOf<TestFailedException>()
            .HasMessage("Custom failure message");

    [TestCase]
    [RequireGodotRuntime]
    public void AppendFailureMessage()
    {
        var node = AutoFree(new Node());
        AssertThrown(() => AssertObject(node)
                    .AppendFailureMessage("custom data")
                    .IsNull())
            .IsInstanceOf<TestFailedException>()
            .HasMessage($"""
                         Expecting be <Null>:
                          but is
                             {AssertFailures.AsObjectId(node)}

                         Additional info:
                         custom data
                         """);
    }

    [TestCase]
    public void InterruptIsFailure()
    {
        // we disable failure reporting until we simulate an failure
        if (ExecutionContext.Current != null)
            ExecutionContext.Current.FailureReporting = false;

        // try to fail
        AssertObject(null).IsNotNull();

        // expect this line will never called because of the test is interrupted by a failing assert
        AssertBool(true).OverrideFailureMessage("This line should never be called").IsFalse();
    }

    [TestCase]
    public void MethodChainingBaseAssert()
    {
        var obj = new object();
        AssertObject(obj).IsNotNull().IsSame(obj).IsInstanceOf<object>();
        AssertObject(obj).IsSame(obj).IsNotNull().IsInstanceOf<object>();
    }
}

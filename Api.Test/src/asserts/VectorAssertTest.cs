// GdUnit generated TestSuite

namespace GdUnit4.Tests.Asserts;

using System.Collections.Generic;
using System.Globalization;
using GdUnit4.Asserts;
using GdUnit4.Core.Execution.Exceptions;
using Godot;
using static Assertions;
using SystemVector2 = System.Numerics.Vector2;
using SystemVector3 = System.Numerics.Vector3;
using SystemVector4 = System.Numerics.Vector4;

[TestSuite]
public class VectorAssertTest
{
    private static IEnumerable<object[]> GodotVectorApproximateDataPoints =>
    [

        // Vector2
        [new Vector2(0, 1), new Vector2(0.04f, 0.04f)], [new Vector2(3, 3), new Vector2(0.04f, 0.04f)],
        [new Vector2(-3, -3), new Vector2(0.04f, 0.04f)],

        // Vector2I
        [new Vector2I(1, 1), new Vector2I(1, 1)], [new Vector2I(3, 3), new Vector2I(1, 1)], [new Vector2I(-3, -3), new Vector2I(1, 1)],

        // Vector3
        [new Vector3(0, 1, 0), new Vector3(0.04f, 0.04f, 0.04f)], [new Vector3(3, 3, 3), new Vector3(0.04f, 0.04f, 0.04f)],
        [new Vector3(-3, -3, -3), new Vector3(0.04f, 0.04f, 0.04f)],

        // Vector3I
        [new Vector3I(1, 1, 1), new Vector3I(1, 1, 1)], [new Vector3I(3, 3, 3), new Vector3I(1, 1, 1)],
        [new Vector3I(-3, -3, -3), new Vector3I(1, 1, 1)],

        // Vector4
        [new Vector4(0, 1, 0, 0), new Vector4(0.04f, 0.04f, 0.04f, 0.04f)], [new Vector4(3, 3, 3, 3), new Vector4(0.04f, 0.04f, 0.04f, 0.04f)],
        [new Vector4(-3, -3, -3, -3), new Vector4(0.04f, 0.04f, 0.04f, 0.04f)],

        // Vector4I
        [new Vector4I(0, 1, 0, 0), new Vector4I(1, 1, 1, 1)], [new Vector4I(3, 3, 3, 3), new Vector4I(1, 1, 1, 1)],
        [new Vector4I(-3, -3, -3, -3), new Vector4I(1, 1, 1, 1)]
    ];

    private static IEnumerable<object[]> SystemVectorApproximateDataPoints =>
    [

        // System Vector2
        [new SystemVector2(0, 1), new SystemVector2(0.04f, 0.04f)], [new SystemVector2(3, 3), new SystemVector2(0.04f, 0.04f)],
        [new SystemVector2(-3, -3), new SystemVector2(0.04f, 0.04f)],

        // System Vector3
        [new SystemVector3(0, 1, 0), new SystemVector3(0.04f, 0.04f, 0.04f)], [new SystemVector3(3, 3, 3), new SystemVector3(0.04f, 0.04f, 0.04f)],
        [new SystemVector3(-3, -3, -3), new SystemVector3(0.04f, 0.04f, 0.04f)],

        // System Vector4
        [new SystemVector4(0, 1, 0, 0), new SystemVector4(0.04f, 0.04f, 0.04f, 0.04f)],
        [new SystemVector4(3, 3, 3, 3), new SystemVector4(0.04f, 0.04f, 0.04f, 0.04f)],
        [new SystemVector4(-3, -3, -3, -3), new SystemVector4(0.04f, 0.04f, 0.04f, 0.04f)]
    ];

    [BeforeTest]
    public void Setup() =>

        // we need for testing assert failure messages to run on 'en-US' locale
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US", true);

    [TestCase]
    [RequireGodotRuntime]
    public void AssertThatMapsToVectorAssert()
    {
        AssertObject(AssertThat(Vector2.One)).IsInstanceOf<IVectorAssert<Vector2>>();
        AssertObject(AssertThat(Vector2I.One)).IsInstanceOf<IVectorAssert<Vector2I>>();
        AssertObject(AssertThat(Vector3.One)).IsInstanceOf<IVectorAssert<Vector3>>();
        AssertObject(AssertThat(Vector3I.One)).IsInstanceOf<IVectorAssert<Vector3I>>();
        AssertObject(AssertThat(Vector4.One)).IsInstanceOf<IVectorAssert<Vector4>>();
        AssertObject(AssertThat(Vector4I.One)).IsInstanceOf<IVectorAssert<Vector4I>>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void IsBetween()
    {
        AssertThat(Vector2.Zero).IsBetween(Vector2.Zero, Vector2.One);
        AssertThat(Vector2.One).IsBetween(Vector2.Zero, Vector2.One);

        // false test
        AssertThrown(() => AssertThat(new Vector2(0, -.1f)).IsBetween(Vector2.Zero, Vector2.One))
            .HasFileLineNumber(ExpectedLineNumber())
            .HasMessage("""
                        Expecting:
                            '(0, -0.1)'
                         in range between
                            '(0, 0)' <> '(1, 1)'
                        """);
        AssertThrown(() => AssertThat(new Vector2(1.1f, 0)).IsBetween(Vector2.Zero, Vector2.One))
            .HasMessage("""
                        Expecting:
                            '(1.1, 0)'
                         in range between
                            '(0, 0)' <> '(1, 1)'
                        """);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void IsEqual()
    {
        AssertThat(Vector2.One).IsEqual(Vector2.One);
        AssertThat(Vector2.Inf).IsEqual(Vector2.Inf);
        AssertThat(new Vector2(1.2f, 1.000001f)).IsEqual(new Vector2(1.2f, 1.000001f));

        // false test
        AssertThrown(() => AssertThat(Vector2.One).IsEqual(new Vector2(1.2f, 1.000001f)))
            .HasFileLineNumber(ExpectedLineNumber())
            .HasMessage("""
                        Expecting be equal:
                            '(1.2, 1.000001)' but is '(1, 1)'
                        """);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void IsNotEqual()
    {
        AssertThat(Vector2.One).IsNotEqual(Vector2.Inf);
        AssertThat(Vector2.Inf).IsNotEqual(Vector2.One);
        AssertThat(new Vector2(1.2f, 1.000001f)).IsNotEqual(new Vector2(1.2f, 1.000002f));

        // false test
        AssertThrown(() => AssertThat(new Vector2(1.2f, 1.000001f)).IsNotEqual(new Vector2(1.2f, 1.000001f)))
            .HasFileLineNumber(ExpectedLineNumber())
            .HasMessage("""
                        Expecting be NOT equal:
                            '(1.2, 1.000001)' but is '(1.2, 1.000001)'
                        """);
    }

    [TestCase]
    [DataPoint(nameof(SystemVectorApproximateDataPoints))]
    public void SystemVectorIsEqualApprox(dynamic vector, dynamic epsilon)
    {
        AssertThat(vector).IsEqualApprox(vector, epsilon);
        AssertThat(vector + epsilon).IsEqualApprox(vector, epsilon);
        AssertThat(vector - epsilon).IsEqualApprox(vector, epsilon);

        var lessValue = vector - (epsilon * 2);
        var greaterValue = vector + (epsilon * 2);
        var min = vector - epsilon;
        var max = vector + epsilon;
        AssertThrown(() => AssertThat(lessValue).IsEqualApprox(vector, epsilon))
            .HasFileLineNumber(ExpectedLineNumber())
            .HasMessage($"""
                         Expecting:
                             '{lessValue}'
                          in range between
                             '{min}' <> '{max}'
                         """);
        AssertThrown(() => AssertThat(greaterValue).IsEqualApprox(vector, epsilon))
            .HasFileLineNumber(ExpectedLineNumber())
            .HasMessage($"""
                         Expecting:
                             '{greaterValue}'
                          in range between
                             '{min}' <> '{max}'
                         """);
    }

    [TestCase]
    [DataPoint(nameof(GodotVectorApproximateDataPoints))]
    [RequireGodotRuntime]
    public void GodotVectorIsEqualApprox(dynamic vector, dynamic epsilon)
    {
        AssertThat(vector).IsEqualApprox(vector, epsilon);
        AssertThat(vector + epsilon).IsEqualApprox(vector, epsilon);
        AssertThat(vector - epsilon).IsEqualApprox(vector, epsilon);

        var lessValue = vector - (epsilon * 2);
        var greaterValue = vector + (epsilon * 2);
        var min = vector - epsilon;
        var max = vector + epsilon;
        AssertThrown(() => AssertThat(lessValue).IsEqualApprox(vector, epsilon))
            .HasFileLineNumber(ExpectedLineNumber())
            .HasMessage($"""
                         Expecting:
                             '{lessValue}'
                          in range between
                             '{min}' <> '{max}'
                         """);
        AssertThrown(() => AssertThat(greaterValue).IsEqualApprox(vector, epsilon))
            .HasFileLineNumber(ExpectedLineNumber())
            .HasMessage($"""
                         Expecting:
                             '{greaterValue}'
                          in range between
                             '{min}' <> '{max}'
                         """);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void IsGreater()
    {
        AssertThat(Vector2.Inf).IsGreater(Vector2.One);
        AssertThat(new Vector2(1.2f, 1.000002f)).IsGreater(new Vector2(1.2f, 1.000001f));

        // false test
        AssertThrown(() => AssertThat(Vector2.Zero).IsGreater(Vector2.One))
            .HasFileLineNumber(ExpectedLineNumber())
            .HasMessage("""
                        Expecting to be greater than:
                            '(1, 1)' but is '(0, 0)'
                        """);
        AssertThrown(() => AssertThat(new Vector2(1.2f, 1.000001f)).IsGreater(new Vector2(1.2f, 1.000001f)))
            .HasFileLineNumber(ExpectedLineNumber())
            .HasMessage("""
                        Expecting to be greater than:
                            '(1.2, 1.000001)' but is '(1.2, 1.000001)'
                        """);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void IsGreaterEqual()
    {
        AssertThat(Vector2.Inf).IsGreaterEqual(Vector2.One);
        AssertThat(Vector2.One).IsGreaterEqual(Vector2.One);
        AssertThat(new Vector2(1.2f, 1.000001f)).IsGreaterEqual(new Vector2(1.2f, 1.000001f));
        AssertThat(new Vector2(1.2f, 1.000002f)).IsGreaterEqual(new Vector2(1.2f, 1.000001f));

        // false test
        AssertThrown(() => AssertThat(Vector2.Zero).IsGreaterEqual(Vector2.One))
            .HasFileLineNumber(ExpectedLineNumber())
            .HasMessage("""
                        Expecting to be greater than or equal:
                            '(1, 1)' but is '(0, 0)'
                        """);
        AssertThrown(() => AssertThat(new Vector2(1.2f, 1.000002f)).IsGreaterEqual(new Vector2(1.2f, 1.000003f)))
            .HasFileLineNumber(ExpectedLineNumber())
            .HasMessage("""
                        Expecting to be greater than or equal:
                            '(1.2, 1.000003)' but is '(1.2, 1.000002)'
                        """);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void IsLess()
    {
        AssertThat(Vector2.One).IsLess(Vector2.Inf);
        AssertThat(new Vector2(1.2f, 1.000001f)).IsLess(new Vector2(1.2f, 1.000002f));

        // false test
        AssertThrown(() => AssertThat(Vector2.One).IsLess(Vector2.One))
            .HasFileLineNumber(ExpectedLineNumber())
            .HasMessage("""
                        Expecting to be less than:
                            '(1, 1)' but is '(1, 1)'
                        """);
        AssertThrown(() => AssertThat(new Vector2(1.2f, 1.000001f)).IsLess(new Vector2(1.2f, 1.000001f)))
            .HasFileLineNumber(ExpectedLineNumber())
            .HasMessage("""
                        Expecting to be less than:
                            '(1.2, 1.000001)' but is '(1.2, 1.000001)'
                        """);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void IsLessForAllVectorTypes()
    {
        AssertThat(Vector2.One).IsLess(Vector2.Inf);
        AssertThat(Vector2I.One).IsLess(Vector2I.MaxValue);
        AssertThat(Vector3.One).IsLess(Vector3.Inf);
        AssertThat(Vector3I.One).IsLess(Vector3I.MaxValue);
        AssertThat(Vector4.One).IsLess(Vector4.Inf);
        AssertThat(Vector4I.One).IsLess(Vector4I.MaxValue);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void IsLessEqual()
    {
        AssertThat(Vector2.One).IsLessEqual(Vector2.Inf);
        AssertThat(new Vector2(1.2f, 1.000001f)).IsLessEqual(new Vector2(1.2f, 1.000001f));
        AssertThat(new Vector2(1.2f, 1.000001f)).IsLessEqual(new Vector2(1.2f, 1.000002f));

        // false test
        AssertThrown(() => AssertThat(Vector2.One).IsLessEqual(Vector2.Zero))
            .HasFileLineNumber(ExpectedLineNumber())
            .HasMessage("""
                        Expecting to be less than or equal:
                            '(0, 0)' but is '(1, 1)'
                        """);
        AssertThrown(() => AssertThat(new Vector2(1.2f, 1.000002f)).IsLessEqual(new Vector2(1.2f, 1.000001f)))
            .HasFileLineNumber(ExpectedLineNumber())
            .HasMessage("""
                        Expecting to be less than or equal:
                            '(1.2, 1.000001)' but is '(1.2, 1.000002)'
                        """);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void IsNotBetween()
    {
        AssertThat(new Vector2(1f, 1.0002f)).IsNotBetween(Vector2.Zero, Vector2.One);

        // false test
        AssertThrown(() => AssertThat(Vector2.One).IsNotBetween(Vector2.Zero, Vector2.One))
            .HasFileLineNumber(ExpectedLineNumber())
            .HasMessage("""
                        Expecting:
                            '(1, 1)'
                         be NOT in range between
                            '(0, 0)' <> '(1, 1)'
                        """);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void OverrideFailureMessage() =>
        AssertThrown(() => AssertThat(Vector2.One)
                .OverrideFailureMessage("Custom Error")
                .IsEqual(Vector2.Zero))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("Custom Error");

    [TestCase]
    [RequireGodotRuntime]
    public void AppendFailureMessage()
        => AssertThrown(() => AssertThat(Vector2.One)
                .AppendFailureMessage("custom data")
                .IsEqual(Vector2.Zero))
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(332)
            .HasMessage("""
                        Expecting be equal:
                            '(0, 0)' but is '(1, 1)'

                        Additional info:
                        custom data
                        """);

    [TestCase]
    [RequireGodotRuntime]
    public void MethodChainingBaseAssert()
    {
        AssertThat(Vector3.Left).IsNotNull().IsEqual(Vector3.Left);
        AssertThat(Vector3.Left).IsEqual(Vector3.Left).IsNotNull();
    }
}

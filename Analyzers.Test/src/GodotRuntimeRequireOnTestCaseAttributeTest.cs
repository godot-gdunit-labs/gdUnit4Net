namespace GdUnit4.Analyzers.Test;

using System.Globalization;

using Gu.Roslyn.Asserts;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using static TestSourceBuilder;

[TestClass]
public class GodotRuntimeRequireOnTestCaseAttributeTest
{
    private readonly GodotRuntimeRequireAnalyzer analyzer = new();

    [TestMethod]
    public void TestCaseExpressionUsingGodotReference()
    {
        var source = Instrument(
            """

            [TestCase]
            public void TestMethod()
            {
                var x = new RefCounted();
            }
            """);

        var errorLine = new LinePosition(19, 12);
        var expected = ExpectedDiagnostic
            .Create(
                DiagnosticRules.RuleIds.REQUIRES_GODOT_RUNTIME_ON_METHOD_ID,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DiagnosticRules.GodotEngine.RequiresGodotRuntimeOnMethod.MessageFormat.ToString(),
                    "TestMethod"))
            .WithPosition(new FileLinePositionSpan("TestClass.cs", errorLine, errorLine));

        RoslynAssert.Diagnostics(analyzer, expected, source);
    }

    [TestMethod]
    public void TestCaseExpressionBodyUsingGodotReference()
    {
        var source = Instrument(
            """

            [TestCase]
            public void TestMethod() => new RefCounted();
            """);

        var errorLine = new LinePosition(19, 12);
        var expected = ExpectedDiagnostic
            .Create(
                DiagnosticRules.RuleIds.REQUIRES_GODOT_RUNTIME_ON_METHOD_ID,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DiagnosticRules.GodotEngine.RequiresGodotRuntimeOnMethod.MessageFormat.ToString(),
                    "TestMethod"))
            .WithPosition(new FileLinePositionSpan("TestClass.cs", errorLine, errorLine));

        RoslynAssert.Diagnostics(analyzer, expected, source);
    }

    [TestMethod]
    public void TestCaseExpressionBodyUsingGodotProperty()
    {
        var source = Instrument(
            """

            [TestCase]
            public void TestMethod()
            {
                var classPath = ProjectSettings.GlobalizePath("res://src/core/resources/sources/TestPerson.cs");
            }
            """);

        var errorLine = new LinePosition(19, 12);
        var expected = ExpectedDiagnostic
            .Create(
                DiagnosticRules.RuleIds.REQUIRES_GODOT_RUNTIME_ON_METHOD_ID,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DiagnosticRules.GodotEngine.RequiresGodotRuntimeOnMethod.MessageFormat.ToString(),
                    "TestMethod"))
            .WithPosition(new FileLinePositionSpan("TestClass.cs", errorLine, errorLine));

        RoslynAssert.Diagnostics(analyzer, expected, source);
    }

    [TestMethod]
    public void TestCaseExpressionBodyUsingStaticMethod()
    {
        var source = Instrument(
            """

            [TestCase]
            public void CaseA()
            {
                var tmp = UtilsX1.Foo();

                ProjectSettings.GlobalizePath("res://src/core/resources/sources/TestPerson.cs");
                new Node();
            }


            [TestCase]
            public void CaseB()
            {
                var tmp = UtilsX2.Foo();

                ProjectSettings.GlobalizePath("res://src/core/resources/sources/TestPerson.cs");
                new Node();
            }
            """);

        // Both methods should trigger the diagnostic because they use Godot types
        var expectedA = ExpectedDiagnostic
            .Create(
                DiagnosticRules.RuleIds.REQUIRES_GODOT_RUNTIME_ON_METHOD_ID,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DiagnosticRules.GodotEngine.RequiresGodotRuntimeOnMethod.MessageFormat.ToString(),
                    "CaseA"))
            .WithPosition(new FileLinePositionSpan("TestClass.cs", new LinePosition(19, 12), new LinePosition(19, 12)));

        var expectedB = ExpectedDiagnostic
            .Create(
                DiagnosticRules.RuleIds.REQUIRES_GODOT_RUNTIME_ON_METHOD_ID,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DiagnosticRules.GodotEngine.RequiresGodotRuntimeOnMethod.MessageFormat.ToString(),
                    "CaseB"))
            .WithPosition(new FileLinePositionSpan("TestClass.cs", new LinePosition(29, 12), new LinePosition(29, 12)));

        RoslynAssert.Diagnostics(analyzer, [expectedA, expectedB], source);
    }

    [TestMethod]
    [Ignore("We need to extend the analyzer to analyze method calls inside the method body")]
    public void TestCaseExpressionBodyUsingStaticClassStaticMethod()
    {
        var source = Instrument(
            """

            [TestCase]
            public void TestMethod()
            {
                // UtilsX2 is located locally but uses godot stuff inside and should get an error
                var tmp = UtilsX2.Bar();

                Console.WriteLine("");
            }

            """);

        // Both methods should trigger the diagnostic because they use Godot types
        var expectedA = ExpectedDiagnostic
            .Create(
                DiagnosticRules.RuleIds.REQUIRES_GODOT_RUNTIME_ON_METHOD_ID,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DiagnosticRules.GodotEngine.RequiresGodotRuntimeOnMethod.MessageFormat.ToString(),
                    "TestMethod"))
            .WithPosition(new FileLinePositionSpan("TestClass.cs", new LinePosition(19, 12), new LinePosition(19, 12)));

        RoslynAssert.Diagnostics(analyzer, expectedA, source);
    }

    [TestMethod]
    public void TestCaseExpressionBodyUsingISceneRunner()
    {
        var source = Instrument(
            """

            [TestCase]
            public void TestMethod()
            {
                var sceneRunner = ISceneRunner.Load("res://src/core/resources/scenes/TestSceneWithExceptionTest.tscn", true);
            }

            """);

        // Both methods should trigger the diagnostic because they use Godot types
        var expectedA = ExpectedDiagnostic
            .Create(
                DiagnosticRules.RuleIds.REQUIRES_GODOT_RUNTIME_ON_METHOD_ID,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DiagnosticRules.GodotEngine.RequiresGodotRuntimeOnMethod.MessageFormat.ToString(),
                    "TestMethod"))
            .WithPosition(new FileLinePositionSpan("TestClass.cs", new LinePosition(19, 12), new LinePosition(19, 12)));

        RoslynAssert.Diagnostics(analyzer, expectedA, source);
    }

    [TestMethod]
    public void TestCaseCallingMethodDeclaredInAnotherFile()
    {
        var source = Instrument(
            """

            [TestCase]
            public void TestMethod()
            {
                var value = Helper.Value();
            }
            """);
        const string helperSource = """
                                    namespace GdUnit4.Analyzers.Test.Example
                                    {
                                        public static class Helper
                                        {
                                            public static int Value() => 42;
                                        }
                                    }
                                    """;

        // the analysis of the called method from another syntax tree must not break the analyzer
        RoslynAssert.Valid(analyzer, source, helperSource);
    }

    [TestMethod]
    public void TestCaseExpressionBodyWithRequireGodotRuntimeAttributeOnMethodLevel()
    {
        var source = Instrument(
            """

            [TestCase]
            [RequireGodotRuntime]
            public void TestMethod()
            {
                var x = new RefCounted();
            }
            """);

        RoslynAssert.Valid(analyzer, source);
    }

    [TestMethod]
    public void TestCaseExpressionBodyWithRequireGodotRuntimeAttributeOnClassLevel()
    {
        var source = """
                     using System;
                     using System.Collections;
                     using System.Collections.Generic;
                     using System.Collections.Immutable;
                     using System.Collections.Specialized;
                     using GdUnit4.Asserts;
                     using GdUnit4.Core.Execution.Exceptions;
                     using GdUnit4.Core.Extensions;
                     using Godot;
                     using Godot.Collections;
                     using static GdUnit4.Assertions;

                     namespace GdUnit4.Analyzers.Test.Example
                     {
                         [TestSuite]
                         [RequireGodotRuntime]
                         public class TestClass
                         {
                             [TestCase]
                             public void TestMethod() => new RefCounted();
                         }
                     }
                     """;

        RoslynAssert.Valid(analyzer, source);
    }

    [TestMethod]
    public void TestCaseExpressionBodyCallsMethod()
    {
        var source = Instrument(
            """

            private void UsingGodotObjects() => new Node();

            private void UsingNetObjects() => new object();

            [TestCase]
            public void TestMethod() {
                UsingNetObjects();
                UsingNetObjects();
                UsingGodotObjects();
                UsingGodotObjects();
            }
            """);

        var errorLine = new LinePosition(23, 12);
        var expected = ExpectedDiagnostic
            .Create(
                DiagnosticRules.RuleIds.REQUIRES_GODOT_RUNTIME_ON_METHOD_ID,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DiagnosticRules.GodotEngine.RequiresGodotRuntimeOnMethod.MessageFormat.ToString(),
                    "TestMethod"))
            .WithPosition(new FileLinePositionSpan("TestClass.cs", errorLine, errorLine));

        RoslynAssert.Diagnostics(analyzer, expected, source);
    }

    [TestMethod]
    public void TestCaseExpressionBodyCreatesInstanceInheritFromGodotObjects()
    {
        var source = Instrument(
            """

            [TestCase]
            public void TestMethod()
            {
                var node = new MyNode();
            }

            public partial class MyNode : Node
            {

            }
            """);

        var errorLine = new LinePosition(19, 12);
        var expected = ExpectedDiagnostic
            .Create(
                DiagnosticRules.RuleIds.REQUIRES_GODOT_RUNTIME_ON_METHOD_ID,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DiagnosticRules.GodotEngine.RequiresGodotRuntimeOnMethod.MessageFormat.ToString(),
                    "TestMethod"))
            .WithPosition(new FileLinePositionSpan("TestClass.cs", errorLine, errorLine));

        RoslynAssert.Diagnostics(analyzer, expected, source);
    }

    [TestMethod]
    public void TestCaseExpressionBodyUsingStaticProperty()
    {
        var source = Instrument(
            """
            private static readonly string[] TEST_KEYS = { new("a1"), new("aa2"), new("aaa3"), new("aaaa4") };

            private static readonly object[] TestDataPointKeys =
            {
                // system dictionary types
                new ListDictionary
                {
                    { TEST_KEYS[0], 100 },
                    { TEST_KEYS[1], 200 }
                },
                new System.Collections.Generic.Dictionary<string, int>
                {
                    { TEST_KEYS[0], 100 },
                    { TEST_KEYS[1], 200 }
                },
                // Godot dictionary types
                new Dictionary
                {
                    { TEST_KEYS[0], 100 },
                    { TEST_KEYS[1], 200 }
                },
                new Godot.Collections.Dictionary<Variant, int>
                {
                    { TEST_KEYS[0], 100 },
                    { TEST_KEYS[1], 200 }
                }
            };

            [TestCase(0, TestName = "IDictionary")]
            [TestCase(1, TestName = "IDictionary<string, int>")]
            [TestCase(2, TestName = "GodotDictionary")]
            [TestCase(3, TestName = "GodotDictionary<string, int>")]
            public void TestMethod(int dataPointIndex)
            {
                dynamic current = TestDataPointKeys[dataPointIndex];
            }

            [TestCase]
            public void TestMethod2()
            {

            }
            """);

        var errorLine = new LinePosition(49, 12);
        var expected = ExpectedDiagnostic
            .Create(
                DiagnosticRules.RuleIds.REQUIRES_GODOT_RUNTIME_ON_METHOD_ID,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DiagnosticRules.GodotEngine.RequiresGodotRuntimeOnMethod.MessageFormat.ToString(),
                    "TestMethod"))
            .WithPosition(new FileLinePositionSpan("TestClass.cs", errorLine, errorLine));

        RoslynAssert.Diagnostics(analyzer, expected, source);
    }

    [TestMethod]
    public void TestCaseExpressionUsingGdUnitAssertion()
    {
        var source = Instrument(
            """

            [TestCase]
            public void OverrideFailureMessage() =>
                AssertThrown(() => AssertThat((IDictionary?)null)
                        .OverrideFailureMessage("Custom failure message")
                        .IsNotNull())
                    .IsInstanceOf<TestFailedException>()
                    .HasFileLineNumber(145)
                    .HasMessage("Custom failure message");
            """);

        RoslynAssert.Valid(analyzer, source);
    }

    [TestMethod]
    public void TestCaseCallsSelfRecursiveMethod()
    {
        var source = Instrument(
            """

            [TestCase]
            public static void TestMethod() => Count(3);

            private static int Count(int n) => n <= 0 ? 0 : Count(n - 1);
            """);

        RoslynAssert.Valid(analyzer, source);
    }

    [TestMethod]
    public void TestCaseCallsMutuallyRecursiveMethods()
    {
        var source = Instrument(
            """

            [TestCase]
            public static void TestMethod() => IsEven(3);

            private static bool IsEven(int n) => n == 0 || IsOdd(n - 1);

            private static bool IsOdd(int n) => n != 0 && IsEven(n - 1);
            """);

        RoslynAssert.Valid(analyzer, source);
    }

    [TestMethod]
    public void TestCaseCallsMethodReferencingFieldInitializedByThatMethod()
    {
        var source = Instrument(
            """

            private static readonly int Seed = NextSeed() * 2;

            [TestCase]
            public static void TestMethod() => NextSeed();

            private static int NextSeed() => Seed + 1;
            """);

        RoslynAssert.Valid(analyzer, source);
    }

    [TestMethod]
    public void TestCaseCallsSelfRecursiveMethodUsingGodotReference()
    {
        var source = Instrument(
            """

            [TestCase]
            public static void TestMethod() => CountChildren(3);

            private static int CountChildren(int n) => n <= 0 ? 0 : CountChildren(n - 1) + new Node().GetChildCount();
            """);

        var errorLine = new LinePosition(19, 19);
        var expected = ExpectedDiagnostic
            .Create(
                DiagnosticRules.RuleIds.REQUIRES_GODOT_RUNTIME_ON_METHOD_ID,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DiagnosticRules.GodotEngine.RequiresGodotRuntimeOnMethod.MessageFormat.ToString(),
                    "TestMethod"))
            .WithPosition(new FileLinePositionSpan("TestClass.cs", errorLine, errorLine));

        RoslynAssert.Diagnostics(analyzer, expected, source);
    }

    [TestMethod]
    public void TestCaseCallsMutuallyRecursiveMethodsUsingGodotReference()
    {
        var source = Instrument(
            """

            [TestCase]
            public static void TestMethod() => Visit(3);

            private static void Visit(int n)
            {
                if (n > 0)
                    VisitChildren(n - 1);
            }

            private static void VisitChildren(int n)
            {
                Visit(n);
                var node = new Node();
            }
            """);

        var errorLine = new LinePosition(19, 19);
        var expected = ExpectedDiagnostic
            .Create(
                DiagnosticRules.RuleIds.REQUIRES_GODOT_RUNTIME_ON_METHOD_ID,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DiagnosticRules.GodotEngine.RequiresGodotRuntimeOnMethod.MessageFormat.ToString(),
                    "TestMethod"))
            .WithPosition(new FileLinePositionSpan("TestClass.cs", errorLine, errorLine));

        RoslynAssert.Diagnostics(analyzer, expected, source);
    }
}

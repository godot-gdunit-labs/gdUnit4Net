namespace GdUnit4.Analyzers.Test;

using System.Globalization;

using Gu.Roslyn.Asserts;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using static TestSourceBuilder;

[TestClass]
public class GodotRuntimeRequireOnTestHookAttributeTest
{
    private readonly GodotRuntimeRequireAnalyzer analyzer = new();

    [TestMethod]
    public void HookBeforeNotRequireGodotRuntime()
    {
        var source = Instrument(
            """

            [Before]
            public void Setup() {
                // contains non Godot related code
            }

            [TestCase]
            public void OverrideFailureMessage() =>
                AssertThat(true).IsTrue();
            """);

        RoslynAssert.Valid(analyzer, source);
    }

    [TestMethod]
    public void HookBeforeCallsSelfRecursiveMethod()
    {
        var source = Instrument(
            """

            [Before]
            public void Setup() => Count(3);

            private static int Count(int n) => n <= 0 ? 0 : Count(n - 1);

            [TestCase]
            public void OverrideFailureMessage() =>
                AssertThat(true).IsTrue();
            """);

        RoslynAssert.Valid(analyzer, source);
    }

    [TestMethod]
    public void HookBeforeIsRequireGodotRuntime()
    {
        var source = Instrument(
            """

            [Before]
            public void Setup() =>
                // use a dedicated FPS because we calculate frames by time
                Engine.PhysicsTicksPerSecond = 60;

            [TestCase]
            public void OverrideFailureMessage() =>
                AssertThat(true).IsTrue();
            """);

        var errorLine = new LinePosition(15, 17); // Position at the class level
        var expected = ExpectedDiagnostic
            .Create(
                DiagnosticRules.RuleIds.REQUIRES_GODOT_RUNTIME_ON_CLASS_ID,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DiagnosticRules.GodotEngine.RequiresGodotRuntimeOnClass.MessageFormat.ToString(),
                    "TestClass"))
            .WithPosition(new FileLinePositionSpan("TestClass.cs", errorLine, errorLine));

        RoslynAssert.Diagnostics(analyzer, expected, source);
    }

    [TestMethod]
    public void HookAfterNotRequireGodotRuntime()
    {
        var source = Instrument(
            """

            [After]
            public void Teardown() {
                // contains non Godot related code
            }

            [TestCase]
            public void OverrideFailureMessage() =>
                AssertThat(true).IsTrue();
            """);

        RoslynAssert.Valid(analyzer, source);
    }

    [TestMethod]
    public void HookAfterIsRequireGodotRuntime()
    {
        var source = Instrument(
            """

            [After]
            public void Teardown() =>
                // use a dedicated FPS because we calculate frames by time
                Engine.PhysicsTicksPerSecond = 60;

            [TestCase]
            public void OverrideFailureMessage() =>
                AssertThat(true).IsTrue();
            """);

        var errorLine = new LinePosition(15, 17); // Position at the class level
        var expected = ExpectedDiagnostic
            .Create(
                DiagnosticRules.RuleIds.REQUIRES_GODOT_RUNTIME_ON_CLASS_ID,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DiagnosticRules.GodotEngine.RequiresGodotRuntimeOnClass.MessageFormat.ToString(),
                    "TestClass"))
            .WithPosition(new FileLinePositionSpan("TestClass.cs", errorLine, errorLine));

        RoslynAssert.Diagnostics(analyzer, expected, source);
    }

    [TestMethod]
    public void HookBeforeTestNotRequireGodotRuntime()
    {
        var source = Instrument(
            """

            [BeforeTest]
            public void SetupTest() {
                // contains non Godot related code
            }

            [TestCase]
            public void OverrideFailureMessage() =>
                AssertThat(true).IsTrue();
            """);

        RoslynAssert.Valid(analyzer, source);
    }

    [TestMethod]
    public void HookBeforeTestIsRequireGodotRuntime()
    {
        var source = Instrument(
            """

            [BeforeTest]
            public void SetupTest() =>
                // use a dedicated FPS because we calculate frames by time
                Engine.PhysicsTicksPerSecond = 60;

            [TestCase]
            public void OverrideFailureMessage() =>
                AssertThat(true).IsTrue();
            """);

        var errorLine = new LinePosition(15, 17); // Position at the class level
        var expected = ExpectedDiagnostic
            .Create(
                DiagnosticRules.RuleIds.REQUIRES_GODOT_RUNTIME_ON_CLASS_ID,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DiagnosticRules.GodotEngine.RequiresGodotRuntimeOnClass.MessageFormat.ToString(),
                    "TestClass"))
            .WithPosition(new FileLinePositionSpan("TestClass.cs", errorLine, errorLine));

        RoslynAssert.Diagnostics(analyzer, expected, source);
    }

    [TestMethod]
    public void HookAfterTestNotRequireGodotRuntime()
    {
        var source = Instrument(
            """

            [AfterTest]
            public void TeardownTest() {
                // contains non Godot related code
            }

            [TestCase]
            public void OverrideFailureMessage() =>
                AssertThat(true).IsTrue();
            """);

        RoslynAssert.Valid(analyzer, source);
    }

    [TestMethod]
    public void HookAfterTestIsRequireGodotRuntime()
    {
        var source = Instrument(
            """

            [AfterTest]
            public void TeardownTest() =>
                // use a dedicated FPS because we calculate frames by time
                Engine.PhysicsTicksPerSecond = 60;

            [TestCase]
            public void OverrideFailureMessage() =>
                AssertThat(true).IsTrue();
            """);

        var errorLine = new LinePosition(15, 17); // Position at the class level
        var expected = ExpectedDiagnostic
            .Create(
                DiagnosticRules.RuleIds.REQUIRES_GODOT_RUNTIME_ON_CLASS_ID,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DiagnosticRules.GodotEngine.RequiresGodotRuntimeOnClass.MessageFormat.ToString(),
                    "TestClass"))
            .WithPosition(new FileLinePositionSpan("TestClass.cs", errorLine, errorLine));

        RoslynAssert.Diagnostics(analyzer, expected, source);
    }

    [TestMethod]
    public void HookBeforeTestIsRequireGodotRuntimeForProperty()
    {
        var source = Instrument(
            """

            [Before]
            public void TeardownTest() {
                var classPath = ProjectSettings.GlobalizePath("res://src/core/resources/sources/TestPerson.cs");
            }

            [TestCase]
            public void OverrideFailureMessage() =>
                AssertThat(true).IsTrue();
            """);

        var errorLine = new LinePosition(15, 17); // Position at the class level
        var expected = ExpectedDiagnostic
            .Create(
                DiagnosticRules.RuleIds.REQUIRES_GODOT_RUNTIME_ON_CLASS_ID,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DiagnosticRules.GodotEngine.RequiresGodotRuntimeOnClass.MessageFormat.ToString(),
                    "TestClass"))
            .WithPosition(new FileLinePositionSpan("TestClass.cs", errorLine, errorLine));

        RoslynAssert.Diagnostics(analyzer, expected, source);
    }
}

namespace GdUnit4.Analyzers.Test;

using System.Globalization;

using Gu.Roslyn.Asserts;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using static TestSourceBuilder;

[TestClass]
public sealed class UniqueTestCaseNameAnalyzerTest
{
    private readonly DiagnosticAnalyzer analyzer = new UniqueTestCaseNameAnalyzer();

    [TestMethod]
    public void SingleTestCase()
    {
        var source = Instrument(
            """
            [TestCase(TestName = "A")]
            public void TestMethod()
            {
            }
            """);

        RoslynAssert.Valid(analyzer, source);
    }

    [TestMethod]
    public void MultipleTestCasesNoMatch()
    {
        var source = Instrument(
            """
            [TestCase(TestName = "A")]
            [TestCase(TestName = "B")]
            public void TestMethod()
            {
            }
            """);

        RoslynAssert.Valid(analyzer, source);
    }

    [TestMethod]
    public void MultipleTestCasesWithExplicitMatch()
    {
        var source = Instrument(
            """
            [TestCase(TestName = "A")]
            [TestCase(TestName = "A")]
            public void TestMethod()
            {
            }
            """);

        var errorLine = new LinePosition(18, 1);
        var expected = ExpectedDiagnostic
            .Create(
                DiagnosticRules.RuleIds.TESTCASE_NAMES_MUST_BE_UNIQUE_ID,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DiagnosticRules.TestCase.TestCaseNamesMustBeUnique.MessageFormat.ToString(),
                    "TestMethod"))
            .WithPosition(new FileLinePositionSpan("TestClass.cs", errorLine, errorLine));

        RoslynAssert.Diagnostics(analyzer, expected, source);
    }

    [TestMethod]
    public void MultipleTestCasesWithInterpolatedMatch()
    {
        var source = Instrument(
            """
            [TestCase(TestName = "ABC")]
            [TestCase(TestName = $"A{"B"}C")]
            public void TestMethod()
            {
            }
            """);

        var errorLine = new LinePosition(18, 1);
        var expected = ExpectedDiagnostic
            .Create(
                DiagnosticRules.RuleIds.TESTCASE_NAMES_MUST_BE_UNIQUE_ID,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DiagnosticRules.TestCase.TestCaseNamesMustBeUnique.MessageFormat.ToString(),
                    "TestMethod"))
            .WithPosition(new FileLinePositionSpan("TestClass.cs", errorLine, errorLine));

        RoslynAssert.Diagnostics(analyzer, expected, source);
    }

    [TestMethod]
    public void MultipleTestCasesWithNameOfMatch()
    {
        var source = Instrument(
            """
            [TestCase(TestName = "A")]
            [TestCase(TestName = nameof(A))]
            public void TestMethod()
            {
            }

            public void A()
            {
            }
            """);

        var errorLine = new LinePosition(18, 1);
        var expected = ExpectedDiagnostic
            .Create(
                DiagnosticRules.RuleIds.TESTCASE_NAMES_MUST_BE_UNIQUE_ID,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DiagnosticRules.TestCase.TestCaseNamesMustBeUnique.MessageFormat.ToString(),
                    "TestMethod"))
            .WithPosition(new FileLinePositionSpan("TestClass.cs", errorLine, errorLine));

        RoslynAssert.Diagnostics(analyzer, expected, source);
    }
}

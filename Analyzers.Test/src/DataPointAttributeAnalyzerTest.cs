namespace GdUnit4.Analyzers.Test;

using System.Globalization;

using Gu.Roslyn.Asserts;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using static TestSourceBuilder;

[TestClass]
public class DataPointAttributeAnalyzerTest
{
    private readonly DiagnosticAnalyzer analyzer = new DataPointAttributeAnalyzer();

    [TestMethod]
    public void SingleTestCaseWithDataPoint()
    {
        string source = Instrument(
            """
            public static object[] TestData => new object[] { new object[] { 1, 2, 3 } };

            [TestCase]
            [DataPoint(nameof(TestData))]
            public void TestMethod(int a, int b, int expected)
            {
            }
            """);

        RoslynAssert.Valid(analyzer, source);
    }

    [TestMethod]
    public void MultipleTestCaseWithoutDataPoint()
    {
        var source = Instrument(
            """
            [TestCase]
            [TestCase]
            public void TestMethod(int a, int b)
            {
            }
            """);

        RoslynAssert.Valid(analyzer, source);
    }

    [TestMethod]
    public void MultipleTestCaseWithDataPoint()
    {
        var source = Instrument(
            """
            public static object[] TestData => new object[] { new object[] { 1, 2, 3 } };

            [TestCase]
            [TestCase] // should fail
            [DataPoint(nameof(TestData))]
            public void TestMethod(int a, int b, int expected)
            {
            }
            """);

        var errorLine = new LinePosition(20, 1);
        var expected = ExpectedDiagnostic
            .Create(
                DiagnosticRules.RuleIds.DATA_POINT_WITH_MULTIPLE_TEST_CASE,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DiagnosticRules.DataPoint.MultipleTestCaseAttributes.MessageFormat.ToString(),
                    "TestMethod"))
            .WithPosition(new FileLinePositionSpan("TestClass.cs", errorLine, errorLine));

        RoslynAssert.Diagnostics(analyzer, expected, source);
    }
}

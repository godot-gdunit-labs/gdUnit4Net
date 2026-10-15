namespace GdUnit4.Tests.Core;

using System.Collections.Generic;
using System.Threading.Tasks;

using static Assertions;

using static Utils;

[TestSuite]
public sealed class ExampleTestSuite
{
    [Before]
    public void Before()
    {
    }

    [After]
    public void After()
    {
    }

    [BeforeTest]
    public void BeforeTest()
    {
    }

    [AfterTest]
    public void AfterTest()
    {
    }

    [TestCase]
    [TestCategory("CategoryA")]
    [Trait("Category", "Foo")]
    public void TestFoo()
    {
#pragma warning disable IDE0022 // Use expression body for method
        AssertBool(true).IsEqual(true);
#pragma warning restore IDE0022 // Use expression body for method
    }

    [TestCase]
    public void TestBar()
        => AssertBool(true).IsEqual(true);

    [TestCase]
    public async Task Waiting()
        => await DoWait(200);

    [TestCase(TestName = "Customized")]
    public void TestFooBar()
        => AssertBool(true).IsEqual(true);

    [TestCase(1, 2, 3, 6, TestName = "TestCaseA")]
    [TestCase(3, 4, 5, 12, TestName = "TestCaseB")]
    [TestCase(6, 7, 8, 21, TestName = "TestCaseC")]
    public void TestCaseArguments(int a, int b, int c, int expect)
        => AssertThat(a + b + c).IsEqual(expect);

    [TestCase(1, 2, 3, 6, TestName = "TestCaseA")]
    [TestCase(3, 4, 5, 12, TestName = "TestCaseB")]
    [TestCase(6, 7, 8, 21, TestName = "TestCaseC")]
    public void TestCasesWithCustomTestName(int a, double b, int c, int expect)
    {
#pragma warning disable IDE0022 // Use expression body for method
        AssertThat(a + b + c).IsEqual(expect);
#pragma warning restore IDE0022 // Use expression body for method
    }

    [TestCase(true)]
    public void ParameterizedSingleTest(bool value)
        => AssertThat(value).IsTrue();

    [TestCase]
    [IgnoreUntil(Until = "2030-08-23 22:56:00", Description = "Ignored until Aug 23, 2030 22:56 local time")]
    public void SkippedUntilLocalDate()
        => AssertBool(false)
            .OverrideFailureMessage("Should never be called before Aug 23, 2030 22:56 local time")
            .IsTrue();

    [TestCase]
    [IgnoreUntil(UntilUtc = "2030-08-23 20:56:00", Description = "Ignored until Aug 23, 2030 20:56 UTC (22:56 CEST)")]
    public void SkippedUntilUtcDate()
        => AssertBool(false)
            .OverrideFailureMessage("Should never be called before Aug 23, 2030 20:56 UTC")
            .IsTrue();

    [TestCase]
    [IgnoreUntil(Description = "Permanently ignored until attribute is removed")]
    public void Skipped()
        => AssertBool(false)
            .OverrideFailureMessage("Should never be called, this test is skipped")
            .IsTrue();

    [TestCase]
    [DataPoint(nameof(TestDataProvider.GetTestData), typeof(TestDataProvider))]
    public void TestWithDataPointProperty(int a, int b, int expected)
        => AssertThat(a + b).IsEqual(expected);

    private sealed class TestDataProvider
    {
        public static IEnumerable<object[]> GetTestData()
        {
            yield return [1, 2, 3];
            yield return [5, 5, 10];
            yield return [-1, 1, 0];
        }
    }
}

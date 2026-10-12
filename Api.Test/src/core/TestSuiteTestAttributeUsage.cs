namespace GdUnit4.Tests.Core;

using static Assertions;

[TestSuite]
public sealed class TestSuiteTestAttributeUsage
{
    [TestCase]
    [TestCategory("CategoryA")]
    [Trait("Category", "Foo")]
    public void TestFoo()
        => AssertBool(true).IsEqual(true);

    [TestCase]
    public void TestBar()
        => AssertBool(true).IsEqual(true);

    [TestCase(TestName = "Customized")]
    public void TestFooBar()
        => AssertBool(true).IsEqual(true);

    [TestCase(1, 2, 3, 6, TestName = "TestCaseA")]
    [TestCase(3, 4, 5, 12, TestName = "TestCaseB")]
    [TestCase(6, 7, 8, 21, TestName = "TestCaseC")]
    public void TestCasesWithCustomTestName(int a, double b, int c, int expect)
        => AssertThat(a + b + c).IsEqual(expect);

    [TestCase(true)]
    public void ParameterizedSingleTest(bool value)
        => AssertThat(value).IsTrue();

    [TestCase("")]
    [TestCase(null)]
    public void ParameterizedSingleNullValue(object? value)
        => AssertThat(value == null || value.Equals(string.Empty)).IsTrue();

    [TestCase("foo", null)]
    public void ParameterizedSingleTestNullValues(object? value1, object? value2)
    {
        AssertThat(value1).IsEqual("foo");
        AssertThat(value2).IsNull();
    }

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
}

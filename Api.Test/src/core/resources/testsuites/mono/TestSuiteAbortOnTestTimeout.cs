namespace GdUnit4.Tests.Resources;

using System.Threading.Tasks;

using static Assertions;
using static Utils;

// will be ignored because of missing `[TestSuite]` annotation
// used by executor integration test
// [TestSuite]
public class TestSuiteAbortOnTestTimeout
{
    [Before]
    public async Task Before()
        => await DoWait(500);

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

    [TestCase(Timeout = 1000, Description = "This test will be interrupted after a timeout of 1000ms.")]
    public async Task TestCase1()
    {
        AssertBool(true).IsEqual(true);

        // wait 1500ms to enforce an test interrupt by a timeout
        var elapsedMilliseconds = await DoWait(1500);
        AssertBool(true).OverrideFailureMessage($"Expected this test is interrupted after 1000ms but is runs {elapsedMilliseconds}ms").IsFalse();
    }

    [TestCase(Timeout = 1000, Description = "This test will end with a failure and no timeout.")]
    public async Task TestCase2()
    {
        _ = await DoWait(500);
        AssertBool(true).IsEqual(false);
    }

    [TestCase(Timeout = 1000, Description = "This test will end with a success and no timeout.")]
    public async Task TestCase3()
    {
        _ = await DoWait(500);
        AssertBool(true).IsEqual(true);
    }

    [TestCase(Timeout = 1000, Description = "This test has a invalid signature and should be end with a failure.")]
#pragma warning disable CS1998
    public async void TestCase4()
        => AssertBool(true).IsEqual(true);
#pragma warning restore CS1998

    [TestCase(Description = "This test has no timeout definition and expect to end with success.")]
    public void TestCase5()
        => AssertBool(true).IsEqual(true);
}

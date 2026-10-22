namespace GdUnit4.Tests.Core;

using GdUnit4.Core;

using static Assertions;

[TestSuite]
public class GdUnit4TestEngineTest
{
    [TestCase(30000, "the test session ran into its timeout of 30s, increase 'TestSessionTimeout' in your '.runsettings' file")]
    [TestCase(90000, "the test session ran into its timeout of 1min 30s, increase 'TestSessionTimeout' in your '.runsettings' file")]
    public void DescribeSessionTimeoutCause(int sessionTimeout, string expected)
        => AssertThat(GdUnit4TestEngine.DescribeSessionTimeoutCause(sessionTimeout)).IsEqual(expected);
}

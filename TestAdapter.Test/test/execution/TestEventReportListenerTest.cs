namespace GdUnit4.TestAdapter.Test.Execution;

using Api;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using TestAdapter.Execution;

using static Assertions;

using TestCase = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase;
using TestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

[TestClass]
public class TestEventReportListenerTest
{
    private readonly TestCase testA = new("Examples.ASuiteTest.First", new Uri(GdUnit4TestExecutor.EXECUTOR_URI), "Examples.dll");
    private readonly TestCase testB = new("Examples.BSuiteTest.Second", new Uri(GdUnit4TestExecutor.EXECUTOR_URI), "Examples.dll");
    private readonly List<TestResult> recordedResults = [];
    private readonly List<string> logMessages = [];
    private readonly Mock<IFrameworkHandle> frameworkHandle = new();

    [TestInitialize]
    public void Setup()
    {
        _ = frameworkHandle
            .Setup(fh => fh.RecordResult(It.IsAny<TestResult>()))
            .Callback<TestResult>(recordedResults.Add);
        _ = frameworkHandle
            .Setup(fh => fh.SendMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()))
            .Callback<TestMessageLevel, string>((level, message) => logMessages.Add($"{level}: {message}"));
    }

    [TestMethod]
    public void ReportNotExecutedTestsReportsTestWithoutResultAsFailed()
    {
        var listener = new TestEventReportListener(frameworkHandle.Object, [testA, testB]);
        listener.PublishEvent(TestAfterEvent(testA));
        recordedResults.Clear();

        listener.ReportNotExecutedTests();

        _ = AssertThat(recordedResults)
            .Extract("TestCase.FullyQualifiedName")
            .ContainsExactly("Examples.BSuiteTest.Second");
        _ = AssertThat(recordedResults)
            .Extract("Outcome")
            .ContainsExactly(TestOutcome.Failed);
        _ = AssertThat(recordedResults)
            .Extract("ErrorMessage")
            .ContainsExactly("The test 'Examples.BSuiteTest.Second' was not executed, the test engine reported no result for it.");
        _ = AssertThat(logMessages)
            .Contains("Error: [GdUnit4] 1 test(s) not executed, the test engine reported no result for them.");
        frameworkHandle.Verify(fh => fh.RecordEnd(testB, TestOutcome.Failed), Times.Once);
    }

    [TestMethod]
    public void ReportNotExecutedTestsReportsNothingWhenAllTestsHaveResults()
    {
        var listener = new TestEventReportListener(frameworkHandle.Object, [testA, testB]);
        listener.PublishEvent(TestAfterEvent(testA));
        listener.PublishEvent(TestAfterEvent(testB));
        recordedResults.Clear();

        listener.ReportNotExecutedTests();

        _ = AssertThat(recordedResults).IsEmpty();
        _ = AssertThat(logMessages).NotContains("Error: [GdUnit4] 1 test(s) not executed, the test engine reported no result for them.");
    }

    [TestMethod]
    public void ReportNotExecutedTestsReportsTestOnlyOnce()
    {
        var listener = new TestEventReportListener(frameworkHandle.Object, [testA]);

        listener.ReportNotExecutedTests();
        listener.ReportNotExecutedTests();

        _ = AssertThat(recordedResults)
            .Extract("TestCase.FullyQualifiedName")
            .ContainsExactly("Examples.ASuiteTest.First");
        _ = AssertThat(recordedResults)
            .Extract("Outcome")
            .ContainsExactly(TestOutcome.Failed);
    }

    private static ITestEvent TestAfterEvent(TestCase testCase)
        => Mock.Of<ITestEvent>(e =>
            e.Type == EventType.TestAfter
            && e.Id == testCase.Id
            && e.FullyQualifiedName == testCase.FullyQualifiedName
            && e.IsSuccess
            && e.Reports == new List<ITestReport>());
}

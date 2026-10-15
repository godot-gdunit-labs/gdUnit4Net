// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.TestAdapter.Execution;

using System.Collections.Concurrent;
using System.Text.Encodings.Web;

using Api;

using Core.Extensions;

using Extensions;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

using Utilities;

using static Api.EventType;
using static Api.ReportType;

using TestCase = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase;

internal sealed class TestEventReportListener : ITestEventListener
{
    private readonly ConcurrentDictionary<Guid, bool> reportedTests = new();

    public TestEventReportListener(IFrameworkHandle framework, IReadOnlyList<TestCase> testCases)
    {
        Framework = framework;
        TestCases = testCases;
        DetailedOutput = new[] { Ide.VisualStudio, Ide.VisualStudioCode, Ide.JetBrainsRider }.Contains(IdeType);
        framework.SendMessage(TestMessageLevel.Informational, $"[GdUnit4] Detected IDE {IdeType}");
    }

    public IFrameworkHandle Framework { get; }

    public IReadOnlyList<TestCase> TestCases { get; }

    public bool DetailedOutput { get; }

    public Ide IdeType => IdeDetector.Detect(Framework);

    public int CompletedTests { get; set; }

    public bool IsFailed { get; set; }

    public void PublishEvent(ITestEvent testEvent)
    {
        switch (testEvent.Type)
        {
            case SuiteBefore:
                if (DetailedOutput)
                    Framework.SendMessage(TestMessageLevel.Informational, $"TestSuite: {testEvent.FullyQualifiedName} Processing...");
                ReportSuiteFailure(testEvent, "[Before]");
                break;

            case TestBefore:
            {
                var testCase = FindTestCase(testEvent);
                if (testCase == null)
                {
                    // check is the event just the parent of parameterized tests we do ignore it because all children will be executed
                    if (FindParameterizedTestCase(testEvent))
                        return;
                    Framework.SendMessage(TestMessageLevel.Error, $"TESTCASE_BEFORE: cant find test case Id: {testEvent.Id}");
                    return;
                }

                Framework.RecordStart(testCase);
                if (DetailedOutput)
                    Framework.SendMessage(TestMessageLevel.Informational, $"TestCase: {testEvent.FullyQualifiedName} Processing...");
                break;
            }

            case TestAfter:
            {
                var testCase = FindTestCase(testEvent);
                if (testCase == null)
                {
                    // check is the event just the parent of parameterized tests we do ignore it because all children will be executed
                    if (FindParameterizedTestCase(testEvent))
                        return;
                    Framework.SendMessage(TestMessageLevel.Error, $"TESTCASE_AFTER: cant find test case {testEvent.FullyQualifiedName}");
                    return;
                }

                var testResult = new TestResult(testCase)
                {
                    DisplayName = IdeType is Ide.DotNet or Ide.Unknown ? testCase.FullyQualifiedName : testCase.DisplayName,
                    Outcome = testEvent.AsTestOutcome(),
                    EndTime = DateTimeOffset.Now,
                    Duration = testEvent.ElapsedInMs
                };

                // Set dynamic driven test name (DataPointAttribute)
                if (testEvent.DisplayName != null)
                    testResult.DisplayName = testEvent.DisplayName;

                testEvent.Reports
                    .ToList()
                    .ForEach(report => AddTestReport(report, testResult));

                if (DetailedOutput)
                    Framework.SendMessage(TestMessageLevel.Informational, $"TestCase: {testEvent.FullyQualifiedName} {testResult.Outcome}");
                Framework.RecordResult(testResult);
                Framework.RecordEnd(testCase, testResult.Outcome);
                reportedTests[testCase.Id] = true;
                CompletedTests += 1;
                break;
            }

            case SuiteAfter:
                if (DetailedOutput)
                    Framework.SendMessage(TestMessageLevel.Informational, $"TestSuite: {testEvent.FullyQualifiedName}: {testEvent.AsTestOutcome()}\n");
                ReportSuiteFailure(testEvent, "[After]");
                break;

            case Init:
            case Stop:
            case DiscoverStart:
            case DiscoverEnd:
            default:
                break;
        }
    }

    public void Dispose()
    {
    }

    /// <summary>
    ///     Reports every test case the test engine has not reported a result for as failed.
    ///     A test case without a result was dropped during execution, e.g. because it could not be mapped to its test method,
    ///     and must not be lost silently from the test run.
    /// </summary>
    public void ReportNotExecutedTests()
    {
        var notExecutedTests = TestCases
            .Where(testCase => !reportedTests.ContainsKey(testCase.Id))
            .ToList();
        if (notExecutedTests.Count == 0)
            return;

        Framework.SendMessage(TestMessageLevel.Error, $"[GdUnit4] {notExecutedTests.Count} test(s) not executed, the test engine reported no result for them.");
        foreach (var testCase in notExecutedTests)
        {
            var testResult = new TestResult(testCase)
            {
                DisplayName = IdeType is Ide.DotNet or Ide.Unknown ? testCase.FullyQualifiedName : testCase.DisplayName,
                Outcome = TestOutcome.Failed,
                ErrorMessage = $"The test '{testCase.FullyQualifiedName}' was not executed, the test engine reported no result for it.",
                EndTime = DateTimeOffset.Now
            };

            Framework.RecordResult(testResult);
            Framework.RecordEnd(testCase, testResult.Outcome);
            reportedTests[testCase.Id] = true;
        }
    }

    private static bool IsEventFailed(ITestEvent e)
        => e.Reports.Count > 0;

    private void ReportSuiteFailure(ITestEvent testEvent, string displayName)
    {
        try
        {
            if (!IsEventFailed(testEvent))
                return;
            FindChildTestCases(testEvent)
                .ForEach(testCase =>
                {
                    var testResult = new TestResult(testCase)
                    {
                        DisplayName = $"{displayName}.{testCase.DisplayName}",
                        Outcome = testEvent.AsTestOutcome(),
                        EndTime = DateTimeOffset.Now
                    };

                    testEvent.Reports
                        .ToList()
                        .ForEach(report => AddTestReport(report, testResult));

                    Framework.RecordStart(testCase);
                    Framework.RecordResult(testResult);
                    Framework.RecordEnd(testCase, testResult.Outcome);
                    reportedTests[testCase.Id] = true;
                });
        }

        // VSTest boundary: the listener handles the test events received over the pipe and reports them to the VSTest framework handle,
        // an exception must not end the event processing of the test run
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            Framework.SendMessage(TestMessageLevel.Error, $"{e.Message}\n{e.StackTrace}");
        }
    }

    // ReSharper disable once UnusedMethodReturnValue.Local
#pragma warning disable IDE0072
    private TestResult AddTestReport(ITestReport report, TestResult testResult)
        => IdeDetector.Detect(Framework) switch
#pragma warning restore IDE0072
        {
            Ide.JetBrainsRider => AddRiderTestReport(report, testResult),
            Ide.VisualStudio => AddVisualStudio2022TestReport(report, testResult),
            Ide.VisualStudioCode => AddVisualStudioCodeTestReport(report, testResult),
            _ => AddDefaultTestReport(report, testResult)
        };

    private TestResult AddRiderTestReport(ITestReport report, TestResult testResult)
    {
        var normalizedMessage = report.Message.RichTextNormalize().TrimEnd();

        switch (report.Type)
        {
            case Stdout:
                Framework.SendMessage(TestMessageLevel.Informational, $"Standard Output:\n{normalizedMessage.Indent()}");
                testResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, normalizedMessage.FormatMessageColored(report.Type)));
                break;

            case Warning:
            case Orphan:
                normalizedMessage = normalizedMessage.Replace("WARNING:\n", string.Empty, StringComparison.Ordinal);
                testResult.ErrorMessage = "Warning Detected!";
                testResult.Messages.Add(new TestResultMessage(TestResultMessage.AdditionalInfoCategory, normalizedMessage.FormatMessageColored(report.Type)));
                Framework.SendMessage(TestMessageLevel.Warning, $"Warning:\n{normalizedMessage.Indent()}");
                break;
            case Success:
            case Skipped:
                break;
            case Failure:
            case Terminated:
            case Interrupted:
            case Abort:
            default:
                testResult.ErrorMessage = normalizedMessage;
                testResult.ErrorStackTrace = report.StackTrace;
                Framework.SendMessage(TestMessageLevel.Error, $"Error:\n{normalizedMessage.Indent()}");
                break;
        }

        return testResult;
    }

    private TestResult AddVisualStudio2022TestReport(ITestReport report, TestResult testResult)
    {
        var normalizedMessage = report.Message.RichTextNormalize().TrimEnd();

        switch (report.Type)
        {
            case Stdout:
                testResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, normalizedMessage));
                Framework.SendMessage(TestMessageLevel.Informational, $"Standard Output:\n{normalizedMessage.Indent()}");
                break;

            case Warning:
            case Orphan:
                // for now, we report in category error
                // see https://developercommunity.visualstudio.com/t/Test-Explorer-not-show-additional-report/10768871?port=1025&fsid=1427bd7b-5ee3-4b74-9bc6-3f3f4663546c
                normalizedMessage = normalizedMessage.Replace("WARNING:", "Warning:", StringComparison.Ordinal);
                testResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardErrorCategory, normalizedMessage));
                Framework.SendMessage(TestMessageLevel.Warning, normalizedMessage);
                break;

            case Success:
            case Skipped:
                break;
            case Failure:
            case Terminated:
            case Interrupted:
            case Abort:
            default:
                testResult.ErrorMessage = normalizedMessage;
                testResult.ErrorStackTrace = report.StackTrace;
                Framework.SendMessage(TestMessageLevel.Error, $"Error:\n{normalizedMessage.Indent()}");
                break;
        }

        return testResult;
    }

    private TestResult AddVisualStudioCodeTestReport(ITestReport report, TestResult testResult)
    {
        var normalizedMessage = report.Message.RichTextNormalize().TrimEnd();

        switch (report.Type)
        {
            case Stdout:
                testResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, normalizedMessage));
                Framework.SendMessage(TestMessageLevel.Informational, $"Standard Output:\n{normalizedMessage.Indent()}");
                break;

            case Warning:
            case Orphan:
                // for now, we report in category error
                // see https://developercommunity.visualstudio.com/t/Test-Explorer-not-show-additional-report/10768871?port=1025&fsid=1427bd7b-5ee3-4b74-9bc6-3f3f4663546c
                testResult.ErrorMessage = normalizedMessage;
                testResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardErrorCategory, normalizedMessage));
                Framework.SendMessage(TestMessageLevel.Warning, $"{normalizedMessage.Replace("WARNING:", "Warning:", StringComparison.Ordinal)}");
                break;
            case Success:
            case Skipped:
                break;
            case Failure:
            case Terminated:
            case Interrupted:
            case Abort:
            default:
                testResult.ErrorMessage = normalizedMessage;
                testResult.ErrorStackTrace = report.StackTrace;
                Framework.SendMessage(TestMessageLevel.Error, $"Error:\n{normalizedMessage.Indent()}");
                break;
        }

        return testResult;
    }

    private TestResult AddDefaultTestReport(ITestReport report, TestResult testResult)
    {
        var normalizedMessage = report.Message
            .RichTextNormalize()
            .TrimEnd()
            .Replace("WARNING:", "Warning:", StringComparison.Ordinal)
            .Replace("ERROR:", "Error:", StringComparison.Ordinal);

        switch (report.Type)
        {
            case Stdout:
                testResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, normalizedMessage));
                foreach (var message in normalizedMessage.Split("\n"))
                    Framework.SendMessage(TestMessageLevel.Informational, HtmlEncoder.Default.Encode($"    {message}"));
                break;

            case Warning:
            case Orphan:
                // for now, we report in category error
                // see https://developercommunity.visualstudio.com/t/Test-Explorer-not-show-additional-report/10768871?port=1025&fsid=1427bd7b-5ee3-4b74-9bc6-3f3f4663546c
                testResult.ErrorMessage = normalizedMessage;
                testResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardErrorCategory, normalizedMessage));
                Framework.SendMessage(TestMessageLevel.Warning, normalizedMessage);
                break;
            case Success:
            case Skipped:
                break;
            case Failure:
            case Terminated:
            case Interrupted:
            case Abort:
            default:
                testResult.ErrorMessage = normalizedMessage;
                testResult.ErrorStackTrace = report.StackTrace;
                Framework.SendMessage(TestMessageLevel.Error, normalizedMessage);
                break;
        }

        return testResult;
    }

    private TestCase? FindTestCase(ITestEvent e)
        => TestCases.FirstOrDefault(t => e.Id.Equals(t.Id));

    private bool FindParameterizedTestCase(ITestEvent e)
        => TestCases.Any(t => t.FullyQualifiedName.StartsWith(e.FullyQualifiedName, StringComparison.Ordinal));

    private List<TestCase> FindChildTestCases(ITestEvent e)
        => [.. TestCases.Where(t => t.FullyQualifiedName.StartsWith(e.FullyQualifiedName, StringComparison.Ordinal))];
}

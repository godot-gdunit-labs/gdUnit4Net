// ReSharper disable NullableWarningSuppressionIsUsed
namespace GdUnit4.Tests.Core.Execution;

using System;
using System.Collections.Generic;

using Api;

using GdUnit4.Core.Execution;
using GdUnit4.Core.Reporting;
using GdUnit4.Core.Runners;

using static Assertions;

[TestSuite]
public class TestEventTest
{
    [TestCase]
    public void SerializeDeserializeBefore()
    {
        Dictionary<TestEvent.StatisticKey, object> statistics = new()
        {
            { TestEvent.StatisticKey.ElapsedTime, 124 },
            { TestEvent.StatisticKey.ErrorCount, 2 },
            { TestEvent.StatisticKey.FailedCount, 3 },
            { TestEvent.StatisticKey.SkippedCount, 4 },
            { TestEvent.StatisticKey.OrphanNodes, 0 },
            { TestEvent.StatisticKey.Failed, true },
            { TestEvent.StatisticKey.Errors, false },
            { TestEvent.StatisticKey.Warnings, false },
            { TestEvent.StatisticKey.Skipped, true }
        };

        List<ITestReport> reports = [new TestReport(ReportType.Failure, 42, "test failed")];
        var testEvent = TestEvent.Before("foo/bar/TestSuiteXXX.cs", "TestSuiteXXX", 100, statistics, reports);
        var json = PipeMessageSerializer.Serialize(testEvent);

        var current = PipeMessageSerializer.Deserialize<TestEvent>(json);
        AssertThat(current).IsNotNull().IsEqual(testEvent);
        AssertThat(current!.SuiteName).IsEqual("TestSuiteXXX");
        AssertThat(current.TestName).IsEqual("Before");
    }

    [TestCase]
    public void SerializeDeserializeBeforeTest()
    {
        var guid = Guid.NewGuid();
        var testEvent = TestEvent.BeforeTest(guid, "foo/bar/TestSuiteXXX.cs", "TestSuiteXXX", "TestCaseA");
        var json = PipeMessageSerializer.Serialize(testEvent);

        var current = PipeMessageSerializer.Deserialize<TestEvent>(json);
        AssertThat(current).IsNotNull().IsEqual(testEvent);
        AssertThat(current!.Id).IsEqual(guid);
        AssertThat(current.SuiteName).IsEqual("TestSuiteXXX");
        AssertThat(current.TestName).IsEqual("TestCaseA");
    }

    [TestCase]
    public void SerializeDeserializeAfter()
    {
        Dictionary<TestEvent.StatisticKey, object> statistics = new()
        {
            { TestEvent.StatisticKey.ElapsedTime, 124 },
            { TestEvent.StatisticKey.ErrorCount, 2 },
            { TestEvent.StatisticKey.FailedCount, 3 },
            { TestEvent.StatisticKey.SkippedCount, 4 },
            { TestEvent.StatisticKey.OrphanNodes, 0 },
            { TestEvent.StatisticKey.Failed, true },
            { TestEvent.StatisticKey.Errors, false },
            { TestEvent.StatisticKey.Warnings, false },
            { TestEvent.StatisticKey.Skipped, true }
        };

        List<ITestReport> reports = [new TestReport(ReportType.Failure, 42, "test failed")];

        var testEvent = TestEvent.After("foo/bar/TestSuiteXXX.cs", "TestSuiteXXX", statistics, reports);
        var json = PipeMessageSerializer.Serialize(testEvent);

        var current = PipeMessageSerializer.Deserialize<TestEvent>(json);
        AssertThat(current).IsNotNull().IsEqual(testEvent);
        AssertThat(current!.Reports).Contains(new TestReport(ReportType.Failure, 42, "test failed"));
        AssertThat(current.SuiteName).IsEqual("TestSuiteXXX");
        AssertThat(current.TestName).IsEqual("After");
        AssertThat(current.ElapsedInMs).IsEqual(TimeSpan.FromMilliseconds(124));
        AssertThat(current.ErrorCount).IsEqual(2);
        AssertThat(current.FailedCount).IsEqual(3);
        AssertThat(current.SkippedCount).IsEqual(4);
        AssertThat(current.OrphanCount).IsEqual(0);
        AssertThat(current.IsFailed).IsEqual(true);
        AssertThat(current.IsError).IsEqual(false);
        AssertThat(current.IsWarning).IsEqual(false);
        AssertThat(current.IsSkipped).IsEqual(true);
    }

    [TestCase]
    public void SerializeDeserializeAfterTest()
    {
        Dictionary<TestEvent.StatisticKey, object> statistics = new()
        {
            { TestEvent.StatisticKey.ElapsedTime, 124 },
            { TestEvent.StatisticKey.ErrorCount, 2 },
            { TestEvent.StatisticKey.FailedCount, 3 },
            { TestEvent.StatisticKey.SkippedCount, 4 },
            { TestEvent.StatisticKey.OrphanNodes, 0 },
            { TestEvent.StatisticKey.Failed, true },
            { TestEvent.StatisticKey.Errors, false },
            { TestEvent.StatisticKey.Warnings, false },
            { TestEvent.StatisticKey.Skipped, true }
        };

        List<ITestReport> reports = [new TestReport(ReportType.Failure, 42, "test failed")];

        var testEvent = TestEvent.AfterTest(Guid.Empty, "foo/bar/TestSuiteXXX.cs", "TestSuiteXXX", "TestCaseA", statistics, reports);
        var json = PipeMessageSerializer.Serialize(testEvent);

        var current = PipeMessageSerializer.Deserialize<TestEvent>(json);
        AssertThat(current).IsNotNull().IsEqual(testEvent);
        AssertThat(current!.Reports).Contains(new TestReport(ReportType.Failure, 42, "test failed"));
        AssertThat(current.Id).IsEqual(testEvent.Id);
        AssertThat(current.ElapsedInMs).IsEqual(TimeSpan.FromMilliseconds(124));
        AssertThat(current.ErrorCount).IsEqual(2);
        AssertThat(current.FailedCount).IsEqual(3);
        AssertThat(current.SkippedCount).IsEqual(4);
        AssertThat(current.OrphanCount).IsEqual(0);
        AssertThat(current.IsFailed).IsEqual(true);
        AssertThat(current.IsError).IsEqual(false);
        AssertThat(current.IsWarning).IsEqual(false);
        AssertThat(current.IsSkipped).IsEqual(true);
    }
}

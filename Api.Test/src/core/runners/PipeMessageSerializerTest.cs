namespace GdUnit4.Tests.Core.Runners;

using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;

using Api;

using GdUnit4.Core.Commands;
using GdUnit4.Core.Execution;
using GdUnit4.Core.Reporting;
using GdUnit4.Core.Runners;

using static Assertions;

[TestSuite]
public class PipeMessageSerializerTest
{
    #region Round trip of the known command types

    [TestCase]
    public void RoundTripExecuteTestSuiteCommand()
    {
        var suiteId = Guid.NewGuid();
        var testId = Guid.NewGuid();
        var suite = new TestSuiteNode
        {
            Id = suiteId,
            ParentId = Guid.Empty,
            ManagedType = "Examples.ExampleTest",
            AssemblyPath = "/path/to/Examples.dll",
            SourceFile = "/path/to/ExampleTest.cs",
            Tests =
            [
                new TestCaseNode
                {
                    Id = testId,
                    ParentId = suiteId,
                    ManagedMethod = "TestFoo",
                    LineNumber = 42,
                    AttributeIndex = 1,
                    RequireRunningGodotEngine = true
                }
            ]
        };

        var json = PipeMessageSerializer.Serialize(new ExecuteTestSuiteCommand(suite, true, false));
        var current = PipeMessageSerializer.Deserialize<BaseCommand>(json);

        AssertThat(current).IsInstanceOf<ExecuteTestSuiteCommand>();

        // the command keeps its state private, so the read command must serialize to the same message
        AssertThat(PipeMessageSerializer.Serialize(current)).IsEqual(json);
        using var document = JsonDocument.Parse(json);
        var data = document.RootElement.GetProperty(PipeMessageSerializer.DataProperty);
        AssertThat(data.GetProperty("IsCaptureStdOut").GetBoolean()).IsTrue();
        AssertThat(data.GetProperty("IsEngineMode").GetBoolean()).IsTrue();
        AssertThat(data.GetProperty("IsReportOrphanNodesEnabled").GetBoolean()).IsFalse();
        var suiteData = data.GetProperty("Suite");
        AssertThat(suiteData.GetProperty("Id").GetGuid()).IsEqual(suiteId);
        AssertThat(suiteData.GetProperty("ManagedType").GetString()).IsEqual("Examples.ExampleTest");
        AssertThat(suiteData.GetProperty("AssemblyPath").GetString()).IsEqual("/path/to/Examples.dll");
        AssertThat(suiteData.GetProperty("SourceFile").GetString()).IsEqual("/path/to/ExampleTest.cs");
        var testData = suiteData.GetProperty("Tests")[0];
        AssertThat(testData.GetProperty("Id").GetGuid()).IsEqual(testId);
        AssertThat(testData.GetProperty("ParentId").GetGuid()).IsEqual(suiteId);
        AssertThat(testData.GetProperty("ManagedMethod").GetString()).IsEqual("TestFoo");
        AssertThat(testData.GetProperty("LineNumber").GetInt32()).IsEqual(42);
        AssertThat(testData.GetProperty("AttributeIndex").GetInt32()).IsEqual(1);
    }

    [TestCase]
    public void RoundTripIsAliveCommand()
    {
        var json = /*lang=json,strict*/ """{"$type":"IsAlive","data":{}}""";

        var current = PipeMessageSerializer.Deserialize<BaseCommand>(json);

        AssertThat(current).IsInstanceOf<IsAlive>();
        AssertThat(PipeMessageSerializer.Serialize(current)).IsEqual(json);
    }

    [TestCase]
    public void RoundTripTerminateGodotInstanceCommand()
    {
        var json = PipeMessageSerializer.Serialize(new TerminateGodotInstanceCommand());

        var current = PipeMessageSerializer.Deserialize<BaseCommand>(json);

        AssertThat(current).IsInstanceOf<TerminateGodotInstanceCommand>();
        AssertThat(PipeMessageSerializer.Serialize(current)).IsEqual(json);
    }

    #endregion

    #region Round trip of the known response and event types

    [TestCase]
    public void RoundTripResponse()
    {
        var response = new Response { StatusCode = HttpStatusCode.Gone, Payload = "Connection interrupted." };

        var current = PipeMessageSerializer.Deserialize(PipeMessageSerializer.Serialize(response));

        AssertThat(current).IsInstanceOf<Response>().IsEqual(response);
    }

    [TestCase]
    public void RoundTripTestEventWithStatisticsAndReports()
    {
        var id = Guid.NewGuid();
        var statistics = TestEvent.BuildStatistics(1, true, 2, true, 3, false, false, 0, 1234L);
        List<ITestReport> reports =
        [
            new TestReport(ReportType.Failure, 42, "test failed", "at Foo.Bar()"),
            new TestReport(ReportType.Warning, 0, "orphan detected")
        ];
        var testEvent = TestEvent.AfterTest(id, "res://ExampleTest.cs", "ExampleTest", "TestFoo", statistics, reports)
            .WithFullyQualifiedName("Examples.ExampleTest.TestFoo")
            .WithDisplayName("TestFoo (1)");

        var current = PipeMessageSerializer.Deserialize(PipeMessageSerializer.Serialize(testEvent));

        AssertThat(current).IsInstanceOf<TestEvent>().IsEqual(testEvent);
        var currentEvent = (TestEvent)current;
        AssertThat(currentEvent.Id).IsEqual(id);
        AssertThat(currentEvent.Type).IsEqual(EventType.TestAfter);
        AssertThat(currentEvent.FullyQualifiedName).IsEqual("Examples.ExampleTest.TestFoo");
        AssertThat(currentEvent.DisplayName).IsEqual("TestFoo (1)");
        AssertThat(currentEvent.OrphanCount).IsEqual(1);
        AssertThat(currentEvent.IsError).IsTrue();
        AssertThat(currentEvent.ErrorCount).IsEqual(2);
        AssertThat(currentEvent.IsFailed).IsTrue();
        AssertThat(currentEvent.FailedCount).IsEqual(3);
        AssertThat(currentEvent.IsWarning).IsFalse();
        AssertThat(currentEvent.ElapsedInMs).IsEqual(TimeSpan.FromMilliseconds(1234));
        AssertThat(currentEvent.Reports)
            .Extract("Type")
            .ContainsExactly(ReportType.Failure, ReportType.Warning);
        AssertThat(currentEvent.Reports)
            .Extract("LineNumber")
            .ContainsExactly(42, 0);
        AssertThat(currentEvent.Reports)
            .Extract("Message")
            .ContainsExactly("test failed", "orphan detected");
        AssertThat(currentEvent.Reports)
            .Extract("StackTrace")
            .ContainsExactly("at Foo.Bar()", null);
    }

    #endregion

    #region Unknown message types are rejected

    [TestCase]
    public void DeserializeRejectsUnknownMessageType()
    {
        var json = /*lang=json,strict*/ """{"$type":"System.IO.FileInfo, System.IO.FileSystem","data":{"fileName":"foo.txt"}}""";

        AssertThrown(() => PipeMessageSerializer.Deserialize(json))
            .IsInstanceOf<JsonException>()
            .HasMessage("Unknown pipe message type 'System.IO.FileInfo, System.IO.FileSystem'.");
    }

    [TestCase]
    public void DeserializeRejectsAssemblyQualifiedNameOfKnownType()
    {
        var typeName = typeof(IsAlive).AssemblyQualifiedName;
        var json = $$"""{"$type":"{{typeName}}","data":{} }""";

        AssertThrown(() => PipeMessageSerializer.Deserialize(json))
            .IsInstanceOf<JsonException>()
            .HasMessage($"Unknown pipe message type '{typeName}'.");
    }

    [TestCase]
    public void DeserializeRejectsMessageWithoutType()
        => AssertThrown(() => PipeMessageSerializer.Deserialize(/*lang=json,strict*/ """{"data":{}}"""))
            .IsInstanceOf<JsonException>()
            .HasMessage("Invalid pipe message, the message type or the data is missing.");

    [TestCase]
    public void DeserializeRejectsMessageOfUnexpectedType()
    {
        var json = PipeMessageSerializer.Serialize(new Response { StatusCode = HttpStatusCode.OK });

        AssertThrown(() => PipeMessageSerializer.Deserialize<BaseCommand>(json))
            .IsInstanceOf<JsonException>()
            .HasMessage("The pipe message is not of the expected type 'BaseCommand'.");
    }

    [TestCase]
    public void SerializeRejectsUnknownMessageType()
        => AssertThrown(() => PipeMessageSerializer.Serialize(new TestReport(ReportType.Failure, 1, "failure")))
            .IsInstanceOf<NotSupportedException>()
            .HasMessage("The type 'GdUnit4.Core.Reporting.TestReport' is not a known pipe message type.");

    #endregion

    #region Statistic values of a test event are restricted to numbers and booleans

    [TestCase]
    public void DeserializeRejectsTestEventStatisticWithObjectValue()
    {
        var json = /*lang=json,strict*/ """
                     {"$type":"TestEvent","data":{
                     "Statistics":{"ErrorCount":{"$type":"System.IO.FileInfo, System.IO.FileSystem","fileName":"foo.txt"}}}}
                     """;

        AssertThrown(() => PipeMessageSerializer.Deserialize(json))
            .IsInstanceOf<JsonException>()
            .HasMessage("Unsupported value 'StartObject' of statistic 'ErrorCount'.");
    }

    [TestCase]
    public void DeserializeRejectsTestEventStatisticWithUnknownKey()
    {
        var json = /*lang=json,strict*/ """{"$type":"TestEvent","data":{"Statistics":{"Foo":1}}}""";

        AssertThrown(() => PipeMessageSerializer.Deserialize(json))
            .IsInstanceOf<JsonException>()
            .HasMessage("Unknown statistic key 'Foo'.");
    }

    #endregion
}

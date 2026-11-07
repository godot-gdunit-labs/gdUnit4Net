namespace GdUnit4.Tests.Core.Execution.Monitoring;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Api;

using GdUnit4.Core.Execution;
using GdUnit4.Core.Execution.Exceptions;
using GdUnit4.Core.Execution.Monitoring;

using Godot;

using static Assertions;

[TestSuite]
[RequireGodotRuntime]
public partial class GodotExceptionMonitorTest
{
    [TestCase]
    public void IsExceptionMonitorIsEnabledOnBeforeStage()
    {
        var stage = new BeforeExecutionStage(new TestSuite(typeof(GodotExceptionMonitorTest), new List<TestCaseNode>(), string.Empty));

        // no [Before] hock is set
        AssertBool(stage.IsMonitoringOnGodotExceptionsEnabled).IsFalse();
    }

    [TestCase]
    public void IsExceptionMonitorIsEnabledOnAfterStage()
    {
        var stage = new AfterExecutionStage(new TestSuite(typeof(GodotExceptionMonitorTest), new List<TestCaseNode>(), string.Empty));

        // no [After] hock is set
        AssertBool(stage.IsMonitoringOnGodotExceptionsEnabled).IsFalse();
    }

    [TestCase]
    public void IsExceptionMonitorIsEnabledOnBeforeTestStage()
    {
        var stage = new BeforeTestExecutionStage(new TestSuite(typeof(GodotExceptionMonitorTest), new List<TestCaseNode>(), string.Empty));

        // no [BeforeTest] hock is set
        AssertBool(stage.IsMonitoringOnGodotExceptionsEnabled).IsFalse();
    }

    [TestCase]
    public void IsExceptionMonitorIsEnabledOnAfterTestStage()
    {
        var stage = new AfterTestExecutionStage(new TestSuite(typeof(GodotExceptionMonitorTest), new List<TestCaseNode>(), string.Empty));

        // no [AfterTest] hock is set
        AssertBool(stage.IsMonitoringOnGodotExceptionsEnabled).IsFalse();
    }

    [TestCase]
    [ThrowsException(typeof(InvalidOperationException), "TestNode '_Ready' failed.",
        "/src/core/execution/monitoring/GodotExceptionMonitorTest.cs", 170)]
    public void CatchExceptionOnAddingNodeToSceneTree()
    {
        var sceneTree = (SceneTree)Engine.GetMainLoop();
        sceneTree.Root.AddChild(new TestNode());
    }

    [TestCase]
    [ThrowsException(typeof(InvalidProgramException), "Exception during scene processing",
        "src/core/resources/scenes/TestSceneWithExceptionTest.cs", 23)]
    public async Task CatchExceptionOnSceneTreeProcessing()
    {
        var sceneRunner = ISceneRunner.Load("res://src/core/resources/scenes/TestSceneWithExceptionTest.tscn", true);

        // run scene, it will throw an InvalidProgramException at frame 10
        await sceneRunner.SimulateFrames(10);
    }

    [TestCase]
    [GodotExceptionMonitor]
    public async Task MonitorOnExceptionsButNotThrows()
    {
        var sceneRunner = ISceneRunner.Load("res://src/core/resources/scenes/TestSceneWithExceptionTest.tscn", true);

        // run scene
        await sceneRunner.SimulateFrames(6);
    }

    [TestCase]
    [ThrowsException(typeof(InvalidOperationException), "Test Exception",
        "src/core/resources/scenes/TestSceneWithExceptionTest.cs", 14)]
    public void CatchExceptionIsThrownOnSceneInvoke()
    {
        var runner = ISceneRunner.Load("res://src/core/resources/scenes/TestSceneWithExceptionTest.tscn");

        runner.Invoke("SomeMethodThatThrowsException");
    }

    [TestCase]
    [ThrowsException(typeof(TestFailedException), "Testing Godot PushError",
        "src/core/execution/monitoring/GodotExceptionMonitorTest.cs", 100)]
    public void PushErrorAsTestFailure() => GD.PushError("Testing Godot PushError");

    [TestCase]
    [ThrowsException(typeof(InvalidOperationException), "DispatchNode 'Throw' failed.",
        "src/core/execution/monitoring/GodotExceptionMonitorTest.cs", 181)]
    public void CatchExceptionOnGodotCall()
        => AutoFree(new DispatchNode()).Call(DispatchNode.MethodName.Throw);

    [TestCase]
    [ThrowsException(typeof(InvalidOperationException), "DispatchNode 'Throw' failed.",
        "src/core/execution/monitoring/GodotExceptionMonitorTest.cs", 181)]
    public async Task CatchExceptionOnGodotCallDeferred()
    {
        AutoFree(new DispatchNode()).CallDeferred(DispatchNode.MethodName.Throw);
        await ISceneRunner.SyncProcessFrame;
    }

    [TestCase]
    [ThrowsException(typeof(InvalidOperationException), "DispatchNode 'Value' is read-only.",
        "src/core/execution/monitoring/GodotExceptionMonitorTest.cs", 178)]
    public void CatchExceptionOnGodotPropertySet()
        => AutoFree(new DispatchNode()).Set(DispatchNode.PropertyName.Value, 42);

    [TestCase]
    [ThrowsException(typeof(InvalidOperationException), "DispatchNode failed.",
        "src/core/execution/monitoring/GodotExceptionMonitorTest.cs", 185)]
    public void CatchTheExceptionGodotReported()
    {
        var node = AutoFree(new DispatchNode());

        // thrown with the same type and message, but caught by the test, so Godot never reports it
        AssertThrown(node.ThrowFirst).IsInstanceOf<InvalidOperationException>();
        node.Call(DispatchNode.MethodName.ThrowSecond);
    }

    [TestCase]
    [ThrowsException(typeof(TestFailedException), "Testing Godot PushError from a worker thread",
        "src/core/execution/monitoring/GodotExceptionMonitorTest.cs", 138)]
    public async Task PushErrorFromWorkerThreadAsTestFailure() => await Task.Run(() => GD.PushError("Testing Godot PushError from a worker thread"));

    [TestCase]
    public async Task PushErrorReportsTheStackTraceFromTheCaller()
    {
        using var monitor = GodotExceptionMonitor.Start();
        PushErrorFromHelper();

        var stackTrace = string.Empty;
        try
        {
            await monitor.StopThrow();
        }
        catch (TestFailedException e)
        {
            stackTrace = e.StackTrace!;
        }

        var frames = stackTrace.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        AssertThat(frames).HasSize(2);
        AssertThat(frames[0]).Contains("GodotExceptionMonitorTest.PushErrorFromHelper()").EndsWith("GodotExceptionMonitorTest.cs:line 162");
        AssertThat(frames[1]).Contains("PushErrorReportsTheStackTraceFromTheCaller").EndsWith("GodotExceptionMonitorTest.cs:line 144");
    }

    private static void PushErrorFromHelper() => GD.PushError("Testing Godot PushError from a helper");

    // Test class to verify the interceptor
    public partial class TestNode : Node
    {
        public override void _Ready() =>

            // Trigger a test exception
            throw new InvalidOperationException("TestNode '_Ready' failed.");
    }

    public partial class DispatchNode : Node
    {
        public int Value
        {
            get => 0;
            set => throw new InvalidOperationException("DispatchNode 'Value' is read-only.");
        }

        public void Throw() => throw new InvalidOperationException("DispatchNode 'Throw' failed.");

        public void ThrowFirst() => throw new InvalidOperationException("DispatchNode failed.");

        public void ThrowSecond() => throw new InvalidOperationException("DispatchNode failed.");
    }
}

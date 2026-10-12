namespace GdUnit4.Tests.Core.Execution.Monitoring;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Api;

using GdUnit4.Core.Execution;
using GdUnit4.Core.Execution.Exceptions;

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
        "/src/core/execution/monitoring/GodotExceptionMonitorTest.cs", 107)]
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
        "src/core/execution/monitoring/GodotExceptionMonitorTest.cs", 99)]
    public void PushErrorAsTestFailure() => GD.PushError("Testing Godot PushError");

    // Test class to verify the interceptor
    public partial class TestNode : Node
    {
        public override void _Ready() =>

            // Trigger a test exception
            throw new InvalidOperationException("TestNode '_Ready' failed.");
    }
}

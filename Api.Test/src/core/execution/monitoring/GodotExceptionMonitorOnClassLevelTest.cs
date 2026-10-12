namespace GdUnit4.Tests.Core.Execution.Monitoring;

using System;
using System.Collections.Generic;

using Api;

using GdUnit4.Core.Execution;

using Godot;

using static Assertions;

[TestSuite]
[RequireGodotRuntime]
[GodotExceptionMonitor]
public class GodotExceptionMonitorOnClassLevelTest
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
    public void IsExceptionMonitorIsEnabledOnBeforeStage()
    {
        var stage = new BeforeExecutionStage(new TestSuite(typeof(GodotExceptionMonitorOnClassLevelTest), new List<TestCaseNode>(), string.Empty));
        AssertBool(stage.IsMonitoringOnGodotExceptionsEnabled).IsTrue();
    }

    [TestCase]
    public void IsExceptionMonitorIsEnabledOnAfterStage()
    {
        var stage = new AfterExecutionStage(new TestSuite(typeof(GodotExceptionMonitorOnClassLevelTest), new List<TestCaseNode>(), string.Empty));
        AssertBool(stage.IsMonitoringOnGodotExceptionsEnabled).IsTrue();
    }

    [TestCase]
    public void IsExceptionMonitorIsEnabledOnBeforeTestStage()
    {
        var stage = new BeforeTestExecutionStage(new TestSuite(typeof(GodotExceptionMonitorOnClassLevelTest), new List<TestCaseNode>(), string.Empty));
        AssertBool(stage.IsMonitoringOnGodotExceptionsEnabled).IsTrue();
    }

    [TestCase]
    public void IsExceptionMonitorIsEnabledOnAfterTestStage()
    {
        var stage = new AfterTestExecutionStage(new TestSuite(typeof(GodotExceptionMonitorOnClassLevelTest), new List<TestCaseNode>(), string.Empty));
        AssertBool(stage.IsMonitoringOnGodotExceptionsEnabled).IsTrue();
    }

    [TestCase]
    [ThrowsException(typeof(NullReferenceException), "Nope", "src/core/execution/monitoring/ExampleWithWithEventBus.cs", 13)]
    public void MonitorExceptionOnEmitSignal()
    {
        var tree = (SceneTree)Engine.GetMainLoop();

        var eventBus = AutoFree(new ExampleEventBus());
        tree.Root.AddChild(eventBus);

        var myClass = AutoFree(new ExampleWithWithEventBus());
        tree.Root.AddChild(myClass);

        myClass.Register(eventBus);

        // The emitting of the signal do results into a NullReferenceException
        eventBus.Emit();
    }
}

namespace GdUnit4.Tests.Asserts;

// GdUnit generated TestSuite
using System.Threading.Tasks;

using GdUnit4.Asserts;
using GdUnit4.Core.Execution.Exceptions;
using GdUnit4.Core.Extensions;
using GdUnit4.Core.Signals;

using Godot;

using static Assertions;

[RequireGodotRuntime]
[TestSuite]
public partial class SignalAssertTest
{
    [AfterTest]
    public void TearDown()
    {
        var signalCollector = GodotSignalCollector.Instance;
        AssertThat(signalCollector.CollectedSignals.Keys)
            .OverrideFailureMessage($"Found keys: {signalCollector.CollectedSignals.Keys.Formatted()}")
            .IsEmpty();
    }

    [TestCase]
    public async Task IsEmitted()
    {
        var node = AutoFree(new TestEmitter());
        await AssertSignal(node).IsEmitted("SignalA").WithTimeout(200);
        await AssertSignal(node).IsEmitted("SignalB", "abc").WithTimeout(200);
        await AssertSignal(node).IsEmitted("SignalC", "abc", 100).WithTimeout(200);

        await AssertThrown(AssertSignal(node).IsEmitted("SignalC", "abc", 101).WithTimeout(200))
            .ContinueWith(result => result.Result?
                .IsInstanceOf<TestFailedException>()
                .HasFileLineNumber(36)
                .HasMessage("""
                    Expecting do emitting signal:
                        "SignalC(["abc", 101])"
                     by
                        $obj
                    """
                    .Replace("$obj", AssertFailures.AsObjectId(node))));
    }

    [TestCase]
    public async Task IsNoEmitted()
    {
        var node = AddNode(new Node2D());
        await AssertSignal(node).IsNotEmitted("visibility_changed", 10).WithTimeout(100);
        await AssertSignal(node).IsNotEmitted("visibility_changed", 20).WithTimeout(100);
        await AssertSignal(node).IsNotEmitted("script_changed").WithTimeout(100);

        node.Visible = false;
        await AssertThrown(AssertSignal(node).IsNotEmitted("visibility_changed").WithTimeout(200))
            .ContinueWith(result => result.Result?
                .IsInstanceOf<TestFailedException>()
                .HasMessage("""
                    Expecting do NOT emitting signal:
                        "visibility_changed(<Empty>)"
                     by
                        $obj
                    """
                    .Replace("$obj", AssertFailures.AsObjectId(node))));
    }

    [TestCase]
    public async Task NodeChangedEmittingSignals()
    {
        var node = AddNode(new Node2D());

        await AssertSignal(node).IsEmitted("draw").WithTimeout(200);

        node.Visible = false;
        await AssertSignal(node).IsEmitted("visibility_changed").WithTimeout(200);

        // expecting to fail, we have not changed the visibility
        // node.Visible = false;
        await AssertThrown(AssertSignal(node).IsEmitted("visibility_changed").WithTimeout(200))
            .ContinueWith(result => result.Result?
                .IsInstanceOf<TestFailedException>()
                .HasMessage("""
                    Expecting do emitting signal:
                        "visibility_changed(<Empty>)"
                     by
                        $obj
                    """
                    .Replace("$obj", AssertFailures.AsObjectId(node))));

        node.Show();
        await AssertSignal(node).IsEmitted("draw").WithTimeout(200);
    }

    [TestCase]
    public void IsSignalExists()
    {
        var node = AutoFree(new Node2D());

        AssertSignal(node).IsSignalExists("visibility_changed")
            .IsSignalExists("draw")
            .IsSignalExists("visibility_changed")
            .IsSignalExists("tree_entered")
            .IsSignalExists("tree_exiting")
            .IsSignalExists("tree_exited");

        AssertThrown(() => AssertSignal(node).IsSignalExists("not_existing_signal"))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                Expecting signal exists:
                    "not_existing_signal()"
                 on
                    $obj
                """
                .Replace("$obj", AssertFailures.AsObjectId(node)));
    }

    [TestCase(Timeout = 1000)]
    public async Task MonitorOnSignal()
    {
        var emitterA = AutoFree(new MyEmitter());
        var emitterB = AutoFree(new MyEmitter());

        // verify initial the emitters are not monitored
        AssertThat(GodotSignalCollector.Instance.IsSignalCollecting(emitterA, MyEmitter.SignalName.SignalA)).IsFalse();
        AssertThat(GodotSignalCollector.Instance.IsSignalCollecting(emitterA, MyEmitter.SignalName.SignalB)).IsFalse();
        AssertThat(GodotSignalCollector.Instance.IsSignalCollecting(emitterB, MyEmitter.SignalName.SignalA)).IsFalse();
        AssertThat(GodotSignalCollector.Instance.IsSignalCollecting(emitterB, MyEmitter.SignalName.SignalB)).IsFalse();

        // start monitoring on the emitter A
        AssertSignal(emitterA).StartMonitoring();

        // verify the emitters are now monitored
        AssertThat(GodotSignalCollector.Instance.IsSignalCollecting(emitterA, MyEmitter.SignalName.SignalA)).IsTrue();
        AssertThat(GodotSignalCollector.Instance.IsSignalCollecting(emitterA, MyEmitter.SignalName.SignalB)).IsTrue();
        AssertThat(GodotSignalCollector.Instance.IsSignalCollecting(emitterB, MyEmitter.SignalName.SignalA)).IsFalse();
        AssertThat(GodotSignalCollector.Instance.IsSignalCollecting(emitterB, MyEmitter.SignalName.SignalB)).IsFalse();

        // verify the signals are not emitted initial
        await AssertSignal(emitterA).IsNotEmitted(MyEmitter.SignalName.SignalA).WithTimeout(50);
        await AssertSignal(emitterA).IsNotEmitted(MyEmitter.SignalName.SignalB).WithTimeout(50);
        await AssertSignal(emitterB).IsNotEmitted(MyEmitter.SignalName.SignalA).WithTimeout(50);
        await AssertSignal(emitterB).IsNotEmitted(MyEmitter.SignalName.SignalB).WithTimeout(50);

        // emit signal `signal_a` on emitter_a
        emitterA.DoEmitSignalA();
        await AssertSignal(emitterA).IsEmitted(MyEmitter.SignalName.SignalA).WithTimeout(50);

        // emit signal `my_signal_b` on emitter_a
        emitterA.DoEmitSignalB();
        await AssertSignal(emitterA).IsEmitted(MyEmitter.SignalName.SignalB, "foo").WithTimeout(50);

        // verify emitter_b still has nothing emitted
        await AssertSignal(emitterB).IsNotEmitted(MyEmitter.SignalName.SignalA).WithTimeout(50);
        await AssertSignal(emitterB).IsNotEmitted(MyEmitter.SignalName.SignalB).WithTimeout(50);

        // now verify emitter b
        emitterB.DoEmitSignalA();
        await AssertSignal(emitterB).IsEmitted(MyEmitter.SignalName.SignalA).WithTimeout(50);
    }

    [TestCase(Timeout = 1000, Description = "See https://github.com/MikeSchulze/gdUnit4Net/issues/135")]
    public async Task EmitSignalOnNoneNodeObjects()
    {
        var emitter = AutoFree(new NonNodeEmitter());

        // verify initial the emitters are not monitored
        AssertThat(GodotSignalCollector.Instance.IsSignalCollecting(emitter, NonNodeEmitter.SignalName.SignalA)).IsFalse();

        // start monitoring on the emitter
        AssertSignal(emitter).StartMonitoring();

        // verify the emitters are now monitored
        AssertThat(GodotSignalCollector.Instance.IsSignalCollecting(emitter, NonNodeEmitter.SignalName.SignalA)).IsTrue();

        // verify the signals are not emitted initial
        await AssertSignal(emitter).IsNotEmitted(NonNodeEmitter.SignalName.SignalA).WithTimeout(50);

        // emit signal `signal_a` on emitter
        emitter.DoEmitSignalA();
        await AssertSignal(emitter).IsEmitted(NonNodeEmitter.SignalName.SignalA).WithTimeout(50);
    }

    [TestCase]
    public async Task AwaitSignalOn_Process()
    {
        var runner = AutoFree(new TimedEmitter { RunTime = 0.1f });

        // verify await on a node not attached to the scene tree
        await AssertSignal(runner)
            .IsEmitted(TimedEmitter.SignalName.OnFinished)
            .WithTimeout(150);

        // verify await on a node is attached to the scene tree
        AddNode(runner);
        await AssertSignal(runner)
            .IsEmitted(TimedEmitter.SignalName.OnFinished)
            .WithTimeout(150);
    }

    [TestCase]
    public void OverrideFailureMessage()
    {
        var emitter = AutoFree(new TimedEmitter { RunTime = 0.1f });
        AssertThrown(() => AssertSignal(emitter)
                .OverrideFailureMessage("custom error")
                .IsSignalExists("foo"))
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(207)
            .HasMessage("custom error");
    }

    [TestCase]
    public void AppendFailureMessage()
    {
        var emitter = AutoFree(new TimedEmitter { RunTime = 0.1f });
        AssertThrown(() => AssertSignal(emitter)
                .AppendFailureMessage("custom data")
                .IsSignalExists("foo"))
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(219)
            .HasMessage($"""
                         Expecting signal exists:
                             "foo()"
                          on
                             {AssertFailures.AsObjectId(emitter)}

                         Additional info:
                         custom data
                         """);
    }

    [TestCase]
    public void MethodChainingBaseAssert()
    {
        var emitter = AutoFree(new TimedEmitter { RunTime = 0.1f });
        AssertSignal(emitter).IsNotNull().IsSignalExists("OnFinished");
        AssertSignal(emitter).IsSignalExists("OnFinished").IsNotNull();
    }

    [TestCase]
    public async Task TestAwaitSignalOnSceneTree()
    {
        var ctrl = AddNode(new Control());
        await AwaitSignalOn(ctrl.GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private sealed partial class TestEmitter : Node
    {
        [Signal]
        public delegate void SignalAEventHandler();

        [Signal]
        public delegate void SignalBEventHandler(string value);

        [Signal]
        public delegate void SignalCEventHandler(string value, int count);

        private int frame;

        public override void _Process(double delta)
        {
            switch (frame)
            {
                case 5:
                    EmitSignal(SignalName.SignalA);
                    break;
                case 10:
                    EmitSignal(SignalName.SignalB, "abc");
                    break;
                case 15:
                    EmitSignal(SignalName.SignalC, "abc", 100);
                    break;
            }

            frame++;
        }
    }

    public sealed partial class MyEmitter : Node
    {
        [Signal]
        public delegate void SignalAEventHandler();

        [Signal]
        public delegate void SignalBEventHandler(string value);

        public void DoEmitSignalA() => EmitSignal(SignalName.SignalA);

        public void DoEmitSignalB() => EmitSignal(SignalName.SignalB, "foo");
    }

    public sealed partial class NonNodeEmitter : RefCounted
    {
        [Signal]
        public delegate void SignalAEventHandler();

        public void DoEmitSignalA() => EmitSignal(SignalName.SignalA);
    }

    public partial class TimedEmitter : Node
    {
        [Signal]
        public delegate void OnFinishedEventHandler();

        private float elapsedTime;

        public float RunTime { get; set; }

        public override void _Process(double delta)
        {
            elapsedTime += (float)delta;

            if (!(elapsedTime >= RunTime))
                return;

            EmitSignal(SignalName.OnFinished);
        }
    }
}

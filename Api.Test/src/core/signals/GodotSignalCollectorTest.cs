namespace GdUnit4.Tests.Core.Signals;

using GdUnit4.Core.Signals;

using Godot;

using static Assertions;

[TestSuite]
[RequireGodotRuntime]
public partial class GodotSignalCollectorTest
{
    [TestCase]
    public void DoesNodeProcessingOnParentLessNode()
    {
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var emitter = AutoFree(new Node());
        var doesNodeProcessing = GodotSignalCollector.DoesNodeProcessing(emitter);

        // The node is not hooked into the scene tree, and the `_Process` and `_PhysicsProcess` are not overwritten
        AssertThat(doesNodeProcessing.NeedsCallProcessing).IsFalse();
        AssertThat(doesNodeProcessing.NeedsCallPhysicsProcessing).IsFalse();
    }

    [TestCase]
    public void DoesNodeProcessingOnParentLessObject()
    {
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var emitter = AutoFree(new GodotObject());
        var doesNodeProcessing = GodotSignalCollector.DoesNodeProcessing(emitter);

        // The object is not hooked into the scene tree, and the `_Process` and `_PhysicsProcess` are not overwritten
        AssertThat(doesNodeProcessing.NeedsCallProcessing).IsFalse();
        AssertThat(doesNodeProcessing.NeedsCallPhysicsProcessing).IsFalse();
    }

    [TestCase]
    public void DoesNodeProcessingOnParentNode()
    {
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var emitter = AutoFree(new Node());

        // ReSharper disable once NullableWarningSuppressionIsUsed
        var parent = AutoFree(new Node());
        parent.AddChild(emitter);

        var doesNodeProcessing = GodotSignalCollector.DoesNodeProcessing(emitter);

        // The node is not hooked into the scene tree, and the `_Process` and `_PhysicsProcess` are not overwritten
        AssertThat(doesNodeProcessing.NeedsCallProcessing).IsFalse();
        AssertThat(doesNodeProcessing.NeedsCallPhysicsProcessing).IsFalse();
    }

    [TestCase]
    public void DoesNodeProcessingOnEngineAddedNode()
    {
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var emitter = AutoFree(new Node());
        AddNode(emitter);

        var doesNodeProcessing = GodotSignalCollector.DoesNodeProcessing(emitter);

        // The node is hooked into the scene tree, we don't need to call manually the `_Process` and `_PhysicsProcess`
        AssertThat(doesNodeProcessing.NeedsCallProcessing).IsFalse();
        AssertThat(doesNodeProcessing.NeedsCallPhysicsProcessing).IsFalse();
    }

    [TestCase]
    public void DoesNodeProcessingOnEngineAddedNodeImplements_Process()
    {
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var emitter = AutoFree(new EmitterWithProcessHandler());
        AddNode(emitter);

        var doesNodeProcessing = GodotSignalCollector.DoesNodeProcessing(emitter);

        // The node is hooked into the scene tree, we don't need to call manually the `_Process` and `_PhysicsProcess`
        AssertThat(doesNodeProcessing.NeedsCallProcessing).IsFalse();
        AssertThat(doesNodeProcessing.NeedsCallPhysicsProcessing).IsFalse();
    }

    [TestCase]
    public void DoesNodeProcessingOnEngineNodeImplements_Process()
    {
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var emitter = AutoFree(new EmitterWithProcessHandler());

        var doesNodeProcessing = GodotSignalCollector.DoesNodeProcessing(emitter);

        // The node is not hooked into the scene tree, we must call manually the `_Process` and `_PhysicsProcess`
        AssertThat(doesNodeProcessing.NeedsCallProcessing).IsTrue();
        AssertThat(doesNodeProcessing.NeedsCallPhysicsProcessing).IsTrue();
    }

    public partial class EmitterWithProcessHandler : Node
    {
        public override void _Process(double delta)
        {
        }

        public override void _PhysicsProcess(double delta)
        {
        }
    }
}

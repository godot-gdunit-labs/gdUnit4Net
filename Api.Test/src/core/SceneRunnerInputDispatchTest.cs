namespace GdUnit4.Tests.Core;

using System.Threading.Tasks;

using Godot;

using static Assertions;

[RequireGodotRuntime]
[TestSuite]
public sealed class SceneRunnerInputDispatchTest
{
    [TestCase]
    public async Task KeyEventsReachEachHandlerOnce()
    {
        var runner = ISceneRunner.Load(new InputCountingNode(), true);
        runner.MaximizeView();

        runner.SimulateKeyPressed(Key.A);
        await runner.AwaitInputProcessed();

        var scene = runner.Scene();

        AssertThat(scene.InputCount).IsEqual(2);
        AssertThat(scene.UnhandledCount).IsEqual(2);
    }

    [TestCase]
    public async Task MouseEventsReachEachHandlerOnce()
    {
        var runner = ISceneRunner.Load(new InputCountingNode(), true);
        runner.MaximizeView();

        runner.SetMousePos(new Vector2(50, 50));
        runner.SimulateMouseButtonPressed(MouseButton.Left);
        await runner.AwaitInputProcessed();

        var scene = runner.Scene();

        AssertThat(scene.InputCount).IsEqual(2);
        AssertThat(scene.UnhandledCount).IsEqual(2);
    }

    [TestCase]
    public async Task MouseEventsConsumedByControlSkipUnhandledInput()
    {
        var runner = ISceneRunner.Load(new InputCountingControl { Size = new Vector2(200, 200) }, true);
        runner.MaximizeView();

        runner.SetMousePos(new Vector2(50, 50));
        runner.SimulateMouseButtonPressed(MouseButton.Left);
        await runner.AwaitInputProcessed();

        var scene = runner.Scene();

        AssertThat(scene.InputCount).IsEqual(2);
        AssertThat(scene.GuiCount).IsEqual(2);
        AssertThat(scene.UnhandledCount).IsEqual(0);
    }

    [TestCase]
    public async Task KeyEventsReachFocusedControlOnce()
    {
        var runner = ISceneRunner.Load(new InputCountingControl { FocusMode = Control.FocusModeEnum.All }, true);
        runner.MaximizeView();
        var scene = runner.Scene();
        scene.GrabFocus();

        runner.SimulateKeyPressed(Key.A);
        await runner.AwaitInputProcessed();

        AssertThat(scene.InputCount).IsEqual(2);
        AssertThat(scene.GuiCount).IsEqual(2);
        AssertThat(scene.UnhandledCount).IsEqual(2);
    }

    [TestCase]
    public async Task KeyEventsSkipUnfocusedControl()
    {
        var runner = ISceneRunner.Load(new InputCountingControl { FocusMode = Control.FocusModeEnum.All }, true);
        runner.MaximizeView();

        runner.SimulateKeyPressed(Key.A);
        await runner.AwaitInputProcessed();

        var scene = runner.Scene();

        AssertThat(scene.InputCount).IsEqual(2);
        AssertThat(scene.GuiCount).IsEqual(0);
        AssertThat(scene.UnhandledCount).IsEqual(2);
    }

    // only the simulated key and button count, Input.WarpMouse and real input can add events from the OS
    internal static bool IsSimulated(InputEvent inputEvent)
        => inputEvent is InputEventKey { Keycode: Key.A } or InputEventMouseButton { ButtonIndex: MouseButton.Left };
}

public partial class InputCountingNode : Node
{
    public int InputCount { get; private set; }

    public int UnhandledCount { get; private set; }

    public override void _Input(InputEvent @event)
    {
        if (SceneRunnerInputDispatchTest.IsSimulated(@event))
            InputCount++;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (SceneRunnerInputDispatchTest.IsSimulated(@event))
            UnhandledCount++;
    }
}

public partial class InputCountingControl : Control
{
    public int InputCount { get; private set; }

    public int GuiCount { get; private set; }

    public int UnhandledCount { get; private set; }

    public override void _Input(InputEvent @event)
    {
        if (SceneRunnerInputDispatchTest.IsSimulated(@event))
            InputCount++;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (SceneRunnerInputDispatchTest.IsSimulated(@event))
            GuiCount++;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (SceneRunnerInputDispatchTest.IsSimulated(@event))
            UnhandledCount++;
    }
}

namespace GdUnit4.Tests.Core;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Godot;

using static Assertions;

/// <summary>
///     Verifies the key events the scene receives, the scene runner pushes the simulated events to the scene viewport only.
/// </summary>
[RequireGodotRuntime]
[TestSuite]
public sealed class SceneRunnerKeyEventTest
{
#nullable disable
    private ISceneRunner<KeyRecorderNode> runner;
#nullable enable

    [BeforeTest]
    public async Task BeforeTest()
    {
        runner = ISceneRunner.Load(new KeyRecorderNode(), true);
        await runner.AwaitInputProcessed();
    }

    [AfterTest]
    public void AfterTest()
        => runner.Dispose();

    [TestCase]
    public async Task SimulateKeyPressShiftAndA()
    {
        runner
            .SimulateKeyPress(Key.Shift)
            .SimulateKeyPress(Key.A);
        await runner.AwaitInputProcessed();

        AssertPressedKeys(Key.Shift, Key.A);
        AssertKeyEvents(
            (Key.Shift, true, true),
            (Key.A, true, true));
    }

    [TestCase]
    public async Task SimulateKeyPressAAndShift()
    {
        runner
            .SimulateKeyPress(Key.A)
            .SimulateKeyPress(Key.Shift);
        await runner.AwaitInputProcessed();

        AssertPressedKeys(Key.Shift, Key.A);
        AssertKeyEvents(
            (Key.A, true, false),
            (Key.Shift, true, true));
    }

    [TestCase]
    public async Task SimulateKeyReleaseShiftAndA()
    {
        runner
            .SimulateKeyRelease(Key.Shift)
            .SimulateKeyRelease(Key.A);
        await runner.AwaitInputProcessed();

        AssertPressedKeys();
        AssertKeyEvents(
            (Key.Shift, false, true),
            (Key.A, false, true));
    }

    [TestCase]
    public async Task SimulateKeyPressedShiftAndA()
    {
        runner
            .SimulateKeyPressed(Key.Shift)
            .SimulateKeyPressed(Key.A);
        await runner.AwaitInputProcessed();

        AssertPressedKeys();
        AssertKeyEvents(
            (Key.Shift, true, true),
            (Key.Shift, false, true),
            (Key.A, true, true),
            (Key.A, false, true));
    }

    [TestCase]
    public async Task SimulateKeyPressAWithShiftModifier()
    {
        runner.SimulateKeyPress(Key.Shift);
        await runner.AwaitInputProcessed();
        runner.SimulateKeyPress(Key.A);
        await runner.AwaitInputProcessed();

        AssertPressedKeys(Key.Shift, Key.A);
        AssertKeyEvents(
            (Key.Shift, true, true),
            (Key.A, true, true));
    }

    [TestCase]
    public async Task SimulateKeyReleaseAWithShiftModifier()
    {
        runner
            .SimulateKeyPress(Key.Shift)
            .SimulateKeyPress(Key.A);
        await runner.AwaitInputProcessed();
        runner.SimulateKeyRelease(Key.A);
        await runner.AwaitInputProcessed();

        AssertPressedKeys(Key.Shift);
        AssertKeyEvents(
            (Key.Shift, true, true),
            (Key.A, true, true),
            (Key.A, false, true));
    }

    [TestCase]
    public async Task SimulateKeyPressedAWithShiftModifier()
    {
        runner
            .SimulateKeyPress(Key.Shift)
            .SimulateKeyPressed(Key.A);
        await runner.AwaitInputProcessed();

        AssertPressedKeys(Key.Shift);
        AssertKeyEvents(
            (Key.Shift, true, true),
            (Key.A, true, true),
            (Key.A, false, true));
    }

    [TestCase]
    public async Task SimulateKeyPressedAsAction()
    {
        // the action state is checked by process frames, after the physics frame of AwaitInputProcessed it compares physics frames
        await ISceneRunner.SyncProcessFrame;
        using var keyEvent = new InputEventKey { Keycode = Key.Space };
        InputMap.AddAction("player_jump");
        InputMap.ActionAddEvent("player_jump", keyEvent);
        try
        {
            runner.SimulateKeyPressed(Key.Space);

            // do not wait for the next frame, the input action state is cleared then
            AssertThat(Input.IsActionJustPressed("player_jump", true)).IsTrue();
            AssertThat(Input.IsActionJustPressed("ui_accept", true)).IsTrue();
            AssertThat(Input.IsActionJustPressed("ui_select", true)).IsTrue();
        }
        finally
        {
            InputMap.EraseAction("player_jump");
        }
    }

    [TestCase]
    public async Task SimulateKeyPressedWithCtrlIsNoAction()
    {
        // the action state is checked by process frames, after the physics frame of AwaitInputProcessed it compares physics frames
        await ISceneRunner.SyncProcessFrame;
        using var keyEvent = new InputEventKey { Keycode = Key.Space };
        InputMap.AddAction("player_jump");
        InputMap.ActionAddEvent("player_jump", keyEvent);
        try
        {
            runner
                .SimulateKeyPress(Key.Ctrl)
                .SimulateKeyPressed(Key.Space);

            // do not wait for the next frame, the input action state is cleared then
            AssertThat(Input.IsActionJustPressed("player_jump", true)).IsFalse();
            AssertThat(Input.IsActionJustPressed("ui_accept", true)).IsFalse();
            AssertThat(Input.IsActionJustPressed("ui_select", true)).IsFalse();
        }
        finally
        {
            InputMap.EraseAction("player_jump");
        }
    }

    private static void AssertPressedKeys(params Key[] expected)
    {
        Key[] keys = [Key.A, Key.Shift, Key.Alt, Key.Ctrl, Key.Meta];
        AssertThat(keys.Where(Input.IsKeyPressed)).ContainsExactlyInAnyOrder(expected);
    }

    private void AssertKeyEvents(params (Key Key, bool Pressed, bool Shift)[] expected)
        => AssertThat(runner.Scene().KeyEvents.Select(e => (e.Keycode, e.Pressed, e.ShiftPressed, e.AltPressed, e.CtrlPressed, e.MetaPressed)))
            .ContainsExactly(expected.Select(e => (e.Key, e.Pressed, e.Shift, false, false, false)));
}

public partial class KeyRecorderNode : Node
{
    public List<InputEventKey> KeyEvents { get; } = [];

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent)
            KeyEvents.Add(keyEvent);
    }
}

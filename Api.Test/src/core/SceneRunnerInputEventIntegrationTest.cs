// The expected input events document the spy verifications that are not implemented yet
#pragma warning disable IDE0059
namespace GdUnit4.Tests.Core;

using System;
using System.Linq;
using System.Threading.Tasks;

using GdUnit4.Core;

using Godot;

using static Assertions;

[RequireGodotRuntime]
[TestSuite]
public sealed class SceneRunnerInputEventIntegrationTest
{
#nullable disable
    private ISceneRunner sceneRunner;
#nullable enable

    [Before]
    public void Setup()
    {
    }

    [BeforeTest]
    public void BeforeTest()
    {
        sceneRunner = ISceneRunner.Load("res://src/core/resources/scenes/TestSceneCSharp.tscn", true);
        AssertInitialMouseState();
        AssertInitialKeyState();

        // we need to maximize the view, a minimized view cannot handle mouse events see (https://github.com/godotengine/godot/issues/73461)
        sceneRunner.MaximizeView();
    }

    private void AssertInitialMouseState()
    {
        foreach (var button in Enum.GetValues<MouseButton>())
        {
            // MouseButton.None is not a valid button; Godot 4.5+ throws an error when passed to mouse_button_to_mask
            if (button == MouseButton.None)
                continue;
            AssertThat(Input.IsMouseButtonPressed(button))
                .OverrideFailureMessage($"Expect MouseButton {button} is not 'IsMouseButtonPressed'")
                .IsFalse();
        }

        AssertThat((long)Input.GetMouseButtonMask()).IsEqual(0L);
    }

    private void AssertInitialKeyState()
    {
        foreach (var key in Enum.GetValues<Key>())
        {
            // Key.None is not a physical key; querying it triggers a Godot error
            if (key == Key.None)
                continue;
            AssertThat(Input.IsKeyPressed(key))
                .OverrideFailureMessage($"Expect key {key} is not 'IsKeyPressed'")
                .IsFalse();
            AssertThat(Input.IsPhysicalKeyPressed(key))
                .OverrideFailureMessage($"Expect key {key} is not 'IsPhysicalKeyPressed'")
                .IsFalse();
        }
    }

    private Vector2 ActualMousePos() => sceneRunner.Scene().GetViewport().GetMousePosition();

    [TestCase]
    public void ToMouseButtonMask()
    {
        AssertThat(SceneRunner.ToMouseButtonMask(MouseButton.Left)).IsEqual(MouseButtonMask.Left);
        AssertThat(SceneRunner.ToMouseButtonMask(MouseButton.Middle)).IsEqual(MouseButtonMask.Middle);
        AssertThat(SceneRunner.ToMouseButtonMask(MouseButton.Right)).IsEqual(MouseButtonMask.Right);
        AssertThat(SceneRunner.ToMouseButtonMask(MouseButton.WheelUp)).IsEqual((MouseButtonMask)8L);
        AssertThat(SceneRunner.ToMouseButtonMask(MouseButton.WheelDown)).IsEqual((MouseButtonMask)16L);
        AssertThat(SceneRunner.ToMouseButtonMask(MouseButton.WheelLeft)).IsEqual((MouseButtonMask)32L);
        AssertThat(SceneRunner.ToMouseButtonMask(MouseButton.WheelRight)).IsEqual((MouseButtonMask)64L);
        AssertThat(SceneRunner.ToMouseButtonMask(MouseButton.Xbutton1)).IsEqual(MouseButtonMask.MbXbutton1);
        AssertThat(SceneRunner.ToMouseButtonMask(MouseButton.Xbutton2)).IsEqual(MouseButtonMask.MbXbutton2);
    }

    [TestCase]
    public async Task ResetToInitialStateOnRelease()
    {
        // move mouse out of button range to avoid scene button interactions
        sceneRunner.SetMousePos(new Vector2(400, 400));

        // simulate mouse buttons and key press but we never released it
        sceneRunner.SimulateMouseButtonPress(MouseButton.Left);
        sceneRunner.SimulateMouseButtonPress(MouseButton.Right);
        sceneRunner.SimulateMouseButtonPress(MouseButton.Middle);
        sceneRunner.SimulateKeyPress(Key.Key0);
        sceneRunner.SimulateKeyPress(Key.X);
        sceneRunner.SimulateActionPress("ui_up");
        await ISceneRunner.SyncProcessFrame;

        AssertThat(Input.IsMouseButtonPressed(MouseButton.Left)).IsTrue();
        AssertThat(Input.IsMouseButtonPressed(MouseButton.Right)).IsTrue();
        AssertThat(Input.IsMouseButtonPressed(MouseButton.Middle)).IsTrue();
        AssertThat(Input.IsKeyPressed(Key.Key0)).IsTrue();
        AssertThat(Input.IsPhysicalKeyPressed(Key.Key0)).IsTrue();
        AssertThat(Input.IsKeyPressed(Key.X)).IsTrue();
        AssertThat(Input.IsPhysicalKeyPressed(Key.X)).IsTrue();
        AssertThat(Input.IsActionPressed("ui_up")).IsTrue();

        // unreference the scene SceneRunner to enforce reset to initial Input state
        sceneRunner.Dispose();
        await ISceneRunner.SyncProcessFrame;

        AssertThat(Input.IsMouseButtonPressed(MouseButton.Left)).IsFalse();
        AssertThat(Input.IsMouseButtonPressed(MouseButton.Right)).IsFalse();
        AssertThat(Input.IsMouseButtonPressed(MouseButton.Middle)).IsFalse();
        AssertThat(Input.IsKeyPressed(Key.Key0)).IsFalse();
        AssertThat(Input.IsPhysicalKeyPressed(Key.Key0)).IsFalse();
        AssertThat(Input.IsKeyPressed(Key.X)).IsFalse();
        AssertThat(Input.IsPhysicalKeyPressed(Key.X)).IsFalse();
        AssertThat(Input.IsActionPressed("ui_up")).IsFalse();
    }

    [TestCase]
    public void SimulateKeyPressAsAction()
    {
        var @event = new InputEventKey { Keycode = Key.Space };
        InputMap.AddAction("player_jump");
        InputMap.ActionAddEvent("player_jump", @event);

        AssertThat(sceneRunner.GetProperty("player_jump_action")).IsFalse();

        sceneRunner.SimulateKeyPressed(Key.Space);
        AssertThat(Input.IsActionJustPressed("player_jump")).IsTrue();
        AssertThat(sceneRunner.GetProperty("player_jump_action")).IsTrue();
    }

    [TestCase]
    public async Task SimulateActionPress()
    {
        // iterate over some example actions
        var actionsToSimulate = new[] { "ui_up", "ui_down", "ui_left", "ui_right" };
        foreach (var action in actionsToSimulate)
        {
            AssertThat(InputMap.HasAction(action)).IsTrue();
            sceneRunner.SimulateActionPress(action);
            await ISceneRunner.SyncProcessFrame;

            AssertThat(Input.IsActionPressed(action))
                .OverrideFailureMessage($"Expect the action '{action}' is pressed")
                .IsTrue();
        }

        // other actions are not pressed
        foreach (var action in new[] { "ui_accept", "ui_select", "ui_cancel" })
        {
            AssertThat(Input.IsActionPressed(action))
                .OverrideFailureMessage($"Expect the action '{action}' is NOT pressed")
                .IsFalse();
        }
    }

    [TestCase]
    public async Task SimulateActionRelease()
    {
        // setup do run actions as press
        var actionsToSimulate = new[] { "ui_up", "ui_down", "ui_left", "ui_right" };
        foreach (var action in actionsToSimulate)
        {
            AssertThat(InputMap.HasAction(action)).IsTrue();
            sceneRunner.SimulateActionPress(action);
            await ISceneRunner.SyncProcessFrame;

            // precondition
            AssertThat(Input.IsActionPressed(action))
                .OverrideFailureMessage($"Expect the action '{action}' is pressed")
                .IsTrue();

            // test
            sceneRunner.SimulateActionRelease(action);
            await ISceneRunner.SyncProcessFrame;

            // validate
            AssertThat(Input.IsActionPressed(action))
                .OverrideFailureMessage($"Expect the action '{action}' is released")
                .IsFalse();
        }
    }

    [TestCase]
    public async Task SimulateKeyPress()
    {
        Key[] keys = [Key.A, Key.D, Key.X, Key.Key0];

        foreach (var key in keys)
        {
            sceneRunner.SimulateKeyPress(key);
            await ISceneRunner.SyncProcessFrame;

            var eventKey = new InputEventKey
            {
                Keycode = key,
                PhysicalKeycode = key,
                Pressed = true
            };

            AssertThat(Input.IsKeyPressed(key)).IsTrue();
        }

        AssertThat(Input.IsKeyPressed(Key.A)).IsTrue();
        AssertThat(Input.IsKeyPressed(Key.D)).IsTrue();
        AssertThat(Input.IsKeyPressed(Key.X)).IsTrue();
        AssertThat(Input.IsKeyPressed(Key.Key0)).IsTrue();

        AssertThat(Input.IsKeyPressed(Key.B)).IsFalse();
        AssertThat(Input.IsKeyPressed(Key.G)).IsFalse();
        AssertThat(Input.IsKeyPressed(Key.Z)).IsFalse();
        AssertThat(Input.IsKeyPressed(Key.Key1)).IsFalse();
    }

    [TestCase]
    public async Task SimulateKeyPressWithModifiers()
    {
        // press shift key + A
        sceneRunner
            .SimulateKeyPress(Key.Shift)
            .SimulateKeyPress(Key.A);
        await ISceneRunner.SyncProcessFrame;

        AssertThat(Input.IsKeyPressed(Key.Shift)).IsTrue();
        AssertThat(Input.IsKeyPressed(Key.A)).IsTrue();
    }

    [TestCase]
    public async Task SimulateManyKeysPress()
    {
        // press and hold keys W and Z
        sceneRunner
            .SimulateKeyPress(Key.W)
            .SimulateKeyPress(Key.Z);
        await ISceneRunner.SyncProcessFrame;

        AssertThat(Input.IsKeyPressed(Key.W)).IsTrue();
        AssertThat(Input.IsPhysicalKeyPressed(Key.W)).IsTrue();
        AssertThat(Input.IsKeyPressed(Key.Z)).IsTrue();
        AssertThat(Input.IsPhysicalKeyPressed(Key.Z)).IsTrue();

        // now release key w
        sceneRunner.SimulateKeyRelease(Key.W);
        await ISceneRunner.SyncProcessFrame;

        AssertThat(Input.IsKeyPressed(Key.W)).IsFalse();
        AssertThat(Input.IsPhysicalKeyPressed(Key.W)).IsFalse();
        AssertThat(Input.IsKeyPressed(Key.Z)).IsTrue();
        AssertThat(Input.IsPhysicalKeyPressed(Key.Z)).IsTrue();
    }

    [TestCase]
    public async Task SimulateSetMousePos()
    {
        // save current global mouse pos
        var gmp = sceneRunner.GetGlobalMousePosition();

        // set mouse to pos 100, 100
        sceneRunner.SetMousePos(new Vector2(100, 100));
        await ISceneRunner.SyncProcessFrame;

        AssertThat(ActualMousePos()).IsEqual(new Vector2(100, 100));

        var mouseEvent = new InputEventMouseMotion
        {
            Position = new Vector2(100, 100),
            GlobalPosition = gmp
        };

        // verify(_scene_spy, 1)._input(mouseEvent)

        // set mouse to pos 800, 400
        gmp = sceneRunner.GetGlobalMousePosition();
        sceneRunner.SetMousePos(new Vector2(800, 400));
        await ISceneRunner.SyncProcessFrame;

        AssertThat(ActualMousePos()).IsEqual(new Vector2(800, 400));

        mouseEvent = new InputEventMouseMotion
        {
            Position = new Vector2(800, 400),
            GlobalPosition = gmp
        };

        // verify(_scene_spy, 1)._input(mouseEvent)

        // and again back to 100,100
        gmp = sceneRunner.GetGlobalMousePosition();
        sceneRunner.SetMousePos(new Vector2(100, 100));
        await ISceneRunner.SyncProcessFrame;

        AssertThat(ActualMousePos()).IsEqual(new Vector2(100, 100));

        mouseEvent = new InputEventMouseMotion
        {
            Position = new Vector2(100, 100),
            GlobalPosition = gmp
        };

        // verify(_scene_spy, 1)._input(mouseEvent)
    }

    [TestCase]
    public async Task SimulateSetMousePosWithModifiers()
    {
        var isAlt = false;
        var isControl = false;
        var isShift = false;

        Key[] modifiers = [Key.Shift, Key.Ctrl, Key.Alt];
        MouseButton[] buttons = [MouseButton.Left, MouseButton.Middle, MouseButton.Right];

        foreach (var modifier in modifiers)
        {
            isAlt = isAlt || modifier == Key.Alt;
            isControl = isControl || modifier == Key.Ctrl;
            isShift = isShift || modifier == Key.Shift;

            foreach (var mouse_button in buttons)
            {
                // simulate press shift, set mouse pos and final press mouse button
                var gmp = sceneRunner.GetGlobalMousePosition();

                sceneRunner.SimulateKeyPress(modifier);
                sceneRunner.SetMousePos(Vector2.Zero);
                sceneRunner.SimulateMouseButtonPress(mouse_button);
                await ISceneRunner.SyncProcessFrame;

                var mouseEvent = new InputEventMouseButton
                {
                    Position = Vector2.Zero,
                    GlobalPosition = gmp,
                    AltPressed = isAlt,
                    CtrlPressed = isControl,
                    ShiftPressed = isShift,
                    Pressed = true,
                    ButtonIndex = mouse_button,
                    ButtonMask = SceneRunner.ToMouseButtonMask(mouse_button)
                };

                // verify(_scene_spy, 1)._input(mouseEvent)
                AssertThat(ActualMousePos()).IsEqual(Vector2.Zero);
                AssertThat(Input.IsMouseButtonPressed(mouse_button)).IsTrue();
                AssertThat(Input.GetMouseButtonMask()).IsEqual(mouseEvent.ButtonMask);

                // finally release it
                sceneRunner.SimulateMouseButtonRelease(mouse_button);
                await ISceneRunner.SyncProcessFrame;
                AssertThat(Input.IsMouseButtonPressed(mouse_button)).IsFalse();
                AssertThat(Input.GetMouseButtonMask()).IsEqual((MouseButtonMask)0L);
            }
        }
    }

    [TestCase]
    public async Task SimulateMouseMove()
    {
        var gmp = sceneRunner.GetGlobalMousePosition();

        sceneRunner.SimulateMouseMove(new Vector2(400, 100));
        await ISceneRunner.SyncProcessFrame;

        AssertThat(ActualMousePos()).IsEqual(new Vector2(400, 100));
        var mouseEvent = new InputEventMouseMotion
        {
            Position = new Vector2(400, 100),
            GlobalPosition = gmp,
            Relative = new Vector2(400, 100) - new Vector2(10, 10)
        };

        // verify(_scene_spy, 1)._input(mouseEvent)

        // move mouse to next pos
        gmp = sceneRunner.GetGlobalMousePosition();
        sceneRunner.SimulateMouseMove(new Vector2(55, 42));
        await ISceneRunner.SyncProcessFrame;

        AssertThat(ActualMousePos()).IsEqual(new Vector2(55, 42));
        mouseEvent = new InputEventMouseMotion
        {
            Position = new Vector2(55, 42),
            GlobalPosition = gmp,
            Relative = new Vector2(55, 42) - new Vector2(400, 100)
        };

        // verify(_scene_spy, 1)._input(mouseEvent)
    }

    [TestCase(Timeout = 2000)]
    public async Task SimulateMouseMoveRelative()
    {
        var sourcePosition = new Vector2(10, 10);
        sceneRunner.SimulateMouseMove(sourcePosition);
        await ISceneRunner.SyncProcessFrame;

        // initial pos
        AssertThat(ActualMousePos()).IsEqual(sourcePosition);

        // now move it from source position with offset, in 1s
        await sceneRunner.SimulateMouseMoveRelative(new Vector2(900, 400));

        // check relative position is reached
        AssertThat(ActualMousePos()).IsEqual(new Vector2(910, 410));

        // and now move it back to source position, in 500ms
        await sceneRunner.SimulateMouseMoveRelative(new Vector2(-900, -400), .5);
        AssertThat(ActualMousePos()).IsEqual(sourcePosition);
    }

    [TestCase(Timeout = 2000)]
    public async Task SimulateMouseMoveAbsolute()
    {
        var sourcePosition = new Vector2(10, 10);
        sceneRunner.SimulateMouseMove(sourcePosition);
        await ISceneRunner.SyncProcessFrame;

        // initial pos
        AssertThat(ActualMousePos()).IsEqual(sourcePosition);

        // now move it to a new position, in 1 s
        await sceneRunner.SimulateMouseMoveAbsolute(new Vector2(900, 400));

        // check relative position is reached
        AssertThat(ActualMousePos()).IsEqual(new Vector2(900, 400));

        // and now move it back to the source position, in 500 ms
        await sceneRunner.SimulateMouseMoveAbsolute(sourcePosition, .5);
        AssertThat(ActualMousePos()).IsEqual(sourcePosition);
    }

    [TestCase]
    public async Task SimulateMouseButtonPressLeft()
    {
        // simulate mouse button press and hold
        var gmp = sceneRunner.GetGlobalMousePosition();
        sceneRunner.SimulateMouseButtonPress(MouseButton.Left);
        await ISceneRunner.SyncProcessFrame;

        var mouseEvent = new InputEventMouseButton
        {
            Position = Vector2.Zero,
            GlobalPosition = gmp,
            Pressed = true,
            DoubleClick = false,
            ButtonIndex = MouseButton.Left,
            ButtonMask = MouseButtonMask.Left
        };

        // verify(_scene_spy, 1)._input(mouseEvent)
        AssertThat(Input.IsMouseButtonPressed(MouseButton.Left)).IsTrue();
        AssertThat(Input.GetMouseButtonMask()).IsEqual(MouseButtonMask.Left);
    }

    [TestCase]
    public async Task SimulateMouseButtonPressLeftDoubleClick()
    {
        // simulate mouse button press double_click
        var gmp = sceneRunner.GetGlobalMousePosition();
        sceneRunner.SimulateMouseButtonPress(MouseButton.Left, true);
        await ISceneRunner.SyncProcessFrame;

        var mouseEvent = new InputEventMouseButton
        {
            Position = Vector2.Zero,
            GlobalPosition = gmp,
            Pressed = true,
            DoubleClick = true,
            ButtonIndex = MouseButton.Left,
            ButtonMask = MouseButtonMask.Left
        };

        // verify(_scene_spy, 1)._input(mouseEvent)
        AssertThat(Input.IsMouseButtonPressed(MouseButton.Left)).IsTrue();
        AssertThat(Input.GetMouseButtonMask()).IsEqual(MouseButtonMask.Left);
    }

    [TestCase]
    public async Task SimulateMouseButtonPressRight()
    {
        // simulate mouse button press and hold
        var gmp = sceneRunner.GetGlobalMousePosition();
        sceneRunner.SimulateMouseButtonPress(MouseButton.Right);
        await ISceneRunner.SyncProcessFrame;

        var mouseEvent = new InputEventMouseButton
        {
            Position = Vector2.Zero,
            GlobalPosition = gmp,
            Pressed = true,
            DoubleClick = false,
            ButtonIndex = MouseButton.Right,
            ButtonMask = MouseButtonMask.Right
        };

        // verify(_scene_spy, 1)._input(mouseEvent)
        AssertThat(Input.IsMouseButtonPressed(MouseButton.Right)).IsTrue();
        AssertThat(Input.GetMouseButtonMask()).IsEqual(MouseButtonMask.Right);
    }

    [TestCase]
    public async Task SimulateMouseButtonPressRightDoubleClick()
    {
        // simulate mouse button press double_click
        var gmp = sceneRunner.GetGlobalMousePosition();
        sceneRunner.SimulateMouseButtonPress(MouseButton.Right, true);
        await ISceneRunner.SyncProcessFrame;

        var mouseEvent = new InputEventMouseButton
        {
            Position = Vector2.Zero,
            GlobalPosition = gmp,
            Pressed = true,
            DoubleClick = true,
            ButtonIndex = MouseButton.Right,
            ButtonMask = MouseButtonMask.Right
        };

        // verify(_scene_spy, 1)._input(mouseEvent)
        AssertThat(Input.IsMouseButtonPressed(MouseButton.Right)).IsTrue();
        AssertThat(Input.GetMouseButtonMask()).IsEqual(MouseButtonMask.Right);
    }

    [TestCase]
    public async Task SimulateMouseButtonPressLeftAndRight()
    {
        // simulate mouse button press left+right
        var gmp = sceneRunner.GetGlobalMousePosition();
        sceneRunner.SimulateMouseButtonPress(MouseButton.Left);
        sceneRunner.SimulateMouseButtonPress(MouseButton.Right);
        await ISceneRunner.SyncProcessFrame;

        // results in two events, first is left mouse button
        var mouseEvent = new InputEventMouseButton
        {
            Position = Vector2.Zero,
            GlobalPosition = gmp,
            Pressed = true,
            ButtonIndex = MouseButton.Left,
            ButtonMask = MouseButtonMask.Left
        };

        // verify(_scene_spy, 1)._input(mouseEvent)

        // second is left+right and combined mask
        mouseEvent = new InputEventMouseButton
        {
            Position = Vector2.Zero,
            GlobalPosition = gmp,
            Pressed = true,
            ButtonIndex = MouseButton.Right,
            ButtonMask = MouseButtonMask.Left | MouseButtonMask.Right
        };

        // verify(_scene_spy, 1)._input(mouseEvent)
        AssertThat(Input.IsMouseButtonPressed(MouseButton.Left)).IsTrue();
        AssertThat(Input.IsMouseButtonPressed(MouseButton.Right)).IsTrue();
        AssertThat(Input.GetMouseButtonMask()).IsEqual(MouseButtonMask.Left | MouseButtonMask.Right);
    }

    [TestCase]
    public async Task SimulateMouseButtonPressLeftAndRightAndRelease()
    {
        // simulate mouse button press left+right
        var gmp = sceneRunner.GetGlobalMousePosition();
        sceneRunner.SimulateMouseButtonPress(MouseButton.Left);
        sceneRunner.SimulateMouseButtonPress(MouseButton.Right);
        await ISceneRunner.SyncProcessFrame;

        // will results into two events
        // first for left mouse button
        var mouseEvent = new InputEventMouseButton
        {
            Position = Vector2.Zero,
            GlobalPosition = gmp,
            Pressed = true,
            ButtonIndex = MouseButton.Left,
            ButtonMask = MouseButtonMask.Left
        };

        // verify(_scene_spy, 1)._input(mouseEvent)

        // second is left+right and combined mask
        mouseEvent = new InputEventMouseButton
        {
            Position = Vector2.Zero,
            GlobalPosition = gmp,
            Pressed = true,
            ButtonIndex = MouseButton.Right,
            ButtonMask = MouseButtonMask.Left | MouseButtonMask.Right
        };

        // verify(_scene_spy, 1)._input(mouseEvent)
        AssertThat(Input.IsMouseButtonPressed(MouseButton.Left)).IsTrue();
        AssertThat(Input.IsMouseButtonPressed(MouseButton.Right)).IsTrue();
        AssertThat(Input.GetMouseButtonMask()).IsEqual(MouseButtonMask.Left | MouseButtonMask.Right);

        // now release the right button
        gmp = sceneRunner.GetGlobalMousePosition();
        sceneRunner.SimulateMouseButtonPressed(MouseButton.Right);
        await ISceneRunner.SyncProcessFrame;

        // will result in right button press false but stay with mask for left pressed
        mouseEvent = new InputEventMouseButton
        {
            Position = Vector2.Zero,
            GlobalPosition = gmp,
            Pressed = false,
            ButtonIndex = MouseButton.Right,
            ButtonMask = MouseButtonMask.Left
        };

        // verify(_scene_spy, 1)._input(mouseEvent)
        AssertThat(Input.IsMouseButtonPressed(MouseButton.Left)).IsTrue();
        AssertThat(Input.IsMouseButtonPressed(MouseButton.Right)).IsFalse();
        AssertThat(Input.GetMouseButtonMask()).IsEqual(MouseButtonMask.Left);

        // finally release left button
        gmp = sceneRunner.GetGlobalMousePosition();
        sceneRunner.SimulateMouseButtonPressed(MouseButton.Left);
        await ISceneRunner.SyncProcessFrame;

        // will result in right button press false but stay with mask for left pressed
        mouseEvent = new InputEventMouseButton
        {
            Position = Vector2.Zero,
            GlobalPosition = gmp,
            Pressed = false,
            ButtonIndex = MouseButton.Left,
            ButtonMask = 0
        };

        // verify(_scene_spy, 1)._input(mouseEvent)
        AssertThat(Input.IsMouseButtonPressed(MouseButton.Left)).IsFalse();
        AssertThat(Input.IsMouseButtonPressed(MouseButton.Right)).IsFalse();
        AssertThat(Input.GetMouseButtonMask()).IsEqual((MouseButtonMask)0L);
    }

    [TestCase]
    public async Task SimulateMouseButtonPressed()
    {
        MouseButton[] buttons = [MouseButton.Left, MouseButton.Middle, MouseButton.Right];
        foreach (var mouse_button in buttons)
        {
            // simulate mouse button press and release
            var gmp = sceneRunner.GetGlobalMousePosition();
            sceneRunner.SimulateMouseButtonPressed(mouse_button);
            await ISceneRunner.SyncProcessFrame;

            // it generates two events, first for press and second as released
            var mouseEvent = new InputEventMouseButton
            {
                Position = Vector2.Zero,
                GlobalPosition = gmp,
                Pressed = true,
                ButtonIndex = mouse_button,
                ButtonMask = SceneRunner.ToMouseButtonMask(mouse_button)
            };

            // verify(_scene_spy, 1)._input(mouseEvent)
            mouseEvent = new InputEventMouseButton
            {
                Position = Vector2.Zero,
                GlobalPosition = gmp,
                Pressed = false,
                ButtonIndex = mouse_button,
                ButtonMask = 0L
            };

            // verify(_scene_spy, 1)._input(mouseEvent)
            AssertThat(Input.IsMouseButtonPressed(mouse_button)).IsFalse();
            AssertThat(Input.GetMouseButtonMask()).IsEqual((MouseButtonMask)0L);

            // verify(_scene_spy, 2)._input(any_class(InputEventMouseButton))
            // reset(_scene_spy)
        }
    }

    [TestCase]
    public async Task SimulateMouseButtonPressedDoubleClick()
    {
        MouseButton[] buttons = [MouseButton.Left, MouseButton.Middle, MouseButton.Right];
        foreach (var mouse_button in buttons)
        {
            // simulate mouse button press and release by double_click
            var gmp = sceneRunner.GetGlobalMousePosition();
            sceneRunner.SimulateMouseButtonPressed(mouse_button, true);
            await ISceneRunner.SyncProcessFrame;

            // it generates two events, first for press and second as released
            var mouseEvent = new InputEventMouseButton
            {
                Position = Vector2.Zero,
                GlobalPosition = gmp,
                Pressed = true,
                DoubleClick = true,
                ButtonIndex = mouse_button,
                ButtonMask = SceneRunner.ToMouseButtonMask(mouse_button)
            };

            // verify(_scene_spy, 1)._input(mouseEvent)
            mouseEvent = new InputEventMouseButton
            {
                Position = Vector2.Zero,
                GlobalPosition = gmp,
                Pressed = false,
                DoubleClick = false,
                ButtonIndex = mouse_button,
                ButtonMask = 0L
            };

            // verify(_scene_spy, 1)._input(mouseEvent)
            AssertThat(Input.IsMouseButtonPressed(mouse_button)).IsFalse();
            AssertThat(Input.GetMouseButtonMask()).IsEqual((MouseButtonMask)0L);

            // verify(_scene_spy, 2)._input(any_class(InputEventMouseButton))
            // reset(_scene_spy)
        }
    }

    [TestCase]
    public async Task SimulateMouseButtonPressAndRelease()
    {
        MouseButton[] buttons = [MouseButton.Left, MouseButton.Middle, MouseButton.Right];
        foreach (var mouse_button in buttons)
        {
            var gmp = sceneRunner.GetGlobalMousePosition();

            // simulate mouse button press and release
            sceneRunner.SimulateMouseButtonPress(mouse_button);
            await ISceneRunner.SyncProcessFrame;

            var mouseEvent = new InputEventMouseButton
            {
                Position = Vector2.Zero,
                GlobalPosition = gmp,
                Pressed = true,
                ButtonIndex = mouse_button,
                ButtonMask = SceneRunner.ToMouseButtonMask(mouse_button)
            };

            // verify(_scene_spy, 1)._input(mouseEvent)
            AssertThat(Input.IsMouseButtonPressed(mouse_button)).IsTrue();
            AssertThat(Input.GetMouseButtonMask()).IsEqual(mouseEvent.ButtonMask);

            // now simulate mouse button release
            gmp = sceneRunner.GetGlobalMousePosition();
            sceneRunner.SimulateMouseButtonRelease(mouse_button);
            await ISceneRunner.SyncProcessFrame;

            mouseEvent = new InputEventMouseButton
            {
                Position = Vector2.Zero,
                GlobalPosition = gmp,
                Pressed = false,
                ButtonIndex = mouse_button,
                ButtonMask = 0L
            };

            // verify(_scene_spy, 1)._input(mouseEvent)
            AssertThat(Input.IsMouseButtonPressed(mouse_button)).IsFalse();
            AssertThat(Input.GetMouseButtonMask()).IsEqual(mouseEvent.ButtonMask);
        }
    }

    /// <summary>
    ///     Tests text input functionality.
    ///     Validates keyboard input in text controls.
    /// </summary>
    /// <returns>
    ///     A Task representing the asynchronous test operation.
    /// </returns>
    [TestCase]
    public async Task TextInputProcessing()
    {
        var lineEdit = sceneRunner.FindChild("TextInput") as LineEdit
                       ?? throw new InvalidOperationException("Could not find TextInput");

        // Focus the text input and clear any existing content
        lineEdit.GrabFocus();
        AssertThat(lineEdit.HasFocus()).IsTrue();
        lineEdit.Text = string.Empty;

        // Type individual characters
        sceneRunner.SimulateKeyPressed(Key.H);
        sceneRunner.SimulateKeyPressed(Key.I);
        await sceneRunner.AwaitInputProcessed();

        // Verify text accumulation
        AssertThat(lineEdit.Text).IsEqual("HI");
    }

    [TestCase]
    public async Task MouseDragAndDrop()
    {
        var dragAndDropSceneRunner = ISceneRunner.Load("res://src/core/resources/scenes/DragAndDrop/DragAndDropTestScene.tscn", true);

        var scene = dragAndDropSceneRunner.Scene();
        var slot_left = scene.GetNode<TextureRect>(new NodePath("/root/DragAndDropScene/left/TextureRect"));
        var slot_right = scene.GetNode<TextureRect>(new NodePath("/root/DragAndDropScene/right/TextureRect"));

        var save_mouse_pos = dragAndDropSceneRunner.GetMousePosition();

        // set initial mouse pos over the left slot
        var mouse_pos = slot_left.GlobalPosition + new Vector2(10, 10);

        dragAndDropSceneRunner.SetMousePos(mouse_pos);
        await dragAndDropSceneRunner.AwaitMillis(1000);
        await ISceneRunner.SyncProcessFrame;

        var mouseEvent = new InputEventMouseMotion
        {
            Position = mouse_pos,
            GlobalPosition = save_mouse_pos
        };

        // verify(spy_scene, 1)._gui_input(mouseEvent)
        dragAndDropSceneRunner.SimulateMouseButtonPress(MouseButton.Left);
        await ISceneRunner.SyncProcessFrame;

        AssertThat(Input.IsMouseButtonPressed(MouseButton.Left)).IsTrue();

        // # start drag&drop to left panel
        foreach (var i in Enumerable.Range(0, 20))
        {
            dragAndDropSceneRunner.SimulateMouseMove(mouse_pos + new Vector2(i * .5f * i, 0));
            await dragAndDropSceneRunner.AwaitMillis(40);
        }

        dragAndDropSceneRunner.SimulateMouseButtonRelease(MouseButton.Left);
        await ISceneRunner.SyncProcessFrame;

        AssertThat(slot_right.Texture).IsEqual(slot_left.Texture);
    }
}

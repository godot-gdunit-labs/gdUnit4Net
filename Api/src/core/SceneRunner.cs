// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Api;

using Asserts;

using Constraints;

using Execution.Exceptions;

using Extensions;

using Godot;

using Signals;

using static Assertions;

using ExecutionContext = Execution.ExecutionContext;

internal class SceneRunner : ISceneRunner
{
    private readonly ICollection<string> actionOnPress = new HashSet<string>();
    private readonly Node currentScene;
    private readonly ICollection<Key> keyOnPress = new HashSet<Key>();
    private readonly ICollection<MouseButton> mouseButtonOnPress = new HashSet<MouseButton>();
    private readonly Dictionary<int, Vector2> touchPositions = [];

    [SuppressMessage("Reliability", "CA2213:Disposable fields should be disposed", Justification = "The node is freed in Dispose")]
    private readonly SubViewport viewport;

    [SuppressMessage("Reliability", "CA2213:Disposable fields should be disposed", Justification = "The node is freed in Dispose")]
    private readonly SceneRunnerView? viewportView;

    private bool isMouseEmulated;

    public SceneRunner(string resourcePath, bool autoFree = false, bool verbose = false)
        : this(LoadScene(resourcePath), autoFree, verbose)
    {
    }

    public SceneRunner(Node currentScene, bool autoFree = false, bool verbose = false)
    {
        AssertThat(currentScene)
            .OverrideFailureMessage("SceneRunner requires a valid scene instance, but received null")
            .IsNotNull();
        Verbose = verbose;
        SceneAutoFree = autoFree;
        ExecutionContext.RegisterDisposable(this);
        SceneTree = (SceneTree)Engine.GetMainLoop();
        this.currentScene = currentScene;
        viewport = CreateSceneViewport(SceneTree.Root, currentScene);
        SceneTree.Root.AddChild(viewport);
        if (!IsHeadless)
        {
            viewportView = new SceneRunnerView(viewport);
            SceneTree.Root.AddChild(viewportView);
        }

        SavedIterationsPerSecond = Engine.PhysicsTicksPerSecond;
        _ = SetTimeFactor();
    }

    private static bool IsHeadless => DisplayServer.GetName() == "headless";

    private static bool IsEmulateMouseFromTouch
        => ProjectSettings.GetSetting("input_devices/pointing/emulate_mouse_from_touch", true).AsBool();

    private SceneTree SceneTree { get; }

    private bool Verbose { get; }

    private bool SceneAutoFree { get; }

    private double TimeFactor { get; set; }

    private int SavedIterationsPerSecond { get; }

    private InputEvent? LastInputEvent { get; set; }

    private bool IsDisposed { get; set; }

    public ISceneRunner SimulateActionPress(string action, int eventIndex = -1)
    {
        ThrowIfDisposed();
        var inputEvent = new InputEventAction
        {
            Pressed = true,
            Action = action,
            EventIndex = eventIndex
        };
        actionOnPress.Add(action);
        return HandleInputEvent(inputEvent);
    }

    public ISceneRunner SimulateActionPressed(string action, int eventIndex = -1)
    {
        _ = SimulateActionPress(action, eventIndex);
        _ = SimulateActionRelease(action, eventIndex);
        return this;
    }

    public ISceneRunner SimulateActionRelease(string action, int eventIndex = -1)
    {
        ThrowIfDisposed();
        var inputEvent = new InputEventAction
        {
            Pressed = false,
            Action = action,
            EventIndex = eventIndex
        };
        _ = actionOnPress.Remove(action);
        return HandleInputEvent(inputEvent);
    }

    public ISceneRunner SimulateKeyPress(Key keyCode)
        => SimulateKey(keyCode, true);

    public ISceneRunner SimulateKeyPressed(Key keyCode)
    {
        _ = SimulateKeyPress(keyCode);
        _ = SimulateKeyRelease(keyCode);
        return this;
    }

    public ISceneRunner SimulateKeyRelease(Key keyCode)
        => SimulateKey(keyCode, false);

    public ISceneRunner SetMousePosition(Vector2 position)
    {
        ThrowIfDisposed();
        var inputEvent = new InputEventMouseMotion
        {
            Position = position,
            GlobalPosition = ToGlobalPosition(position)
        };
        ApplyInputModifiers(inputEvent);
        return HandleInputEvent(inputEvent);
    }

    public Vector2 GetMousePosition()
    {
        ThrowIfDisposed();
        if (LastInputEvent is InputEventMouse me)
            return me.Position;
        return viewport.GetMousePosition();
    }

    public Vector2 GetGlobalMousePosition()
    {
        if (LastInputEvent is InputEventMouse mouseEvent)
            return mouseEvent.GlobalPosition;
        if (GodotObject.IsInstanceValid(viewport))
            return ToGlobalPosition(viewport.GetMousePosition());
        return Vector2.Zero;
    }

    public ISceneRunner SimulateMouseMove(Vector2 position)
    {
        ThrowIfDisposed();
        var inputEvent = new InputEventMouseMotion
        {
            Position = position,
            GlobalPosition = ToGlobalPosition(position),
            Relative = position - GetMousePosition()
        };
        ApplyInputMouseMask(inputEvent);
        ApplyInputModifiers(inputEvent);
        return HandleInputEvent(inputEvent);
    }

    public async Task SimulateMouseMoveRelative(Vector2 relative, double time = 1.0, Tween.TransitionType transitionType = Tween.TransitionType.Linear)
        => await SimulateMouseMoveAbsolute(GetMousePosition() + relative, time, transitionType)
            .ConfigureAwait(true);

    public async Task SimulateMouseMoveAbsolute(Vector2 position, double time = 1.0, Tween.TransitionType transitionType = Tween.TransitionType.Linear)
    {
        using var mouseMove = new MouseMoveTask(GetMousePosition(), position);
        await mouseMove.WaitOnFinalPosition(this, time, transitionType)
            .ConfigureAwait(true);
    }

    public ISceneRunner SimulateMouseButtonPressed(MouseButton buttonIndex, bool doubleClick = false)
    {
        _ = SimulateMouseButtonPress(buttonIndex, doubleClick);
        _ = SimulateMouseButtonRelease(buttonIndex);
        return this;
    }

    public ISceneRunner SimulateMouseButtonPress(MouseButton buttonIndex, bool doubleClick = false)
    {
        ThrowIfDisposed();
        PrintCurrentFocus();
        var inputEvent = new InputEventMouseButton
        {
            ButtonIndex = buttonIndex,
            Pressed = true,
            DoubleClick = doubleClick
        };

        mouseButtonOnPress.Add(buttonIndex);
        ApplyInputMousePosition(inputEvent);
        ApplyInputMouseMask(inputEvent);
        ApplyInputModifiers(inputEvent);
        return HandleInputEvent(inputEvent);
    }

    public ISceneRunner SimulateMouseButtonRelease(MouseButton buttonIndex)
    {
        ThrowIfDisposed();
        var inputEvent = new InputEventMouseButton
        {
            ButtonIndex = buttonIndex,
            Pressed = false
        };

        _ = mouseButtonOnPress.Remove(buttonIndex);
        ApplyInputMousePosition(inputEvent);
        ApplyInputMouseMask(inputEvent);
        ApplyInputModifiers(inputEvent);
        return HandleInputEvent(inputEvent);
    }

    public ISceneRunner SimulateScreenTouchPressed(int index, Vector2 position, bool doubleTap = false)
    {
        _ = SimulateScreenTouchPress(index, position, doubleTap);
        _ = SimulateScreenTouchRelease(index);
        return this;
    }

    public ISceneRunner SimulateScreenTouchPress(int index, Vector2 position, bool doubleTap = false)
    {
        ThrowIfDisposed();
        if (IsEmulateMouseFromTouch)
        {
            SimulateEmulatedMouse(() =>
            {
                _ = SetMousePosition(position);
                _ = SimulateMouseButtonPress(MouseButton.Left);
            });
        }

        touchPositions[index] = position;
        using var inputEvent = new InputEventScreenTouch
        {
            WindowId = Scene().GetWindow().GetWindowId(),
            Index = index,
            Position = position,
            DoubleTap = doubleTap,
            Pressed = true
        };
        return PushTouchEvent(inputEvent);
    }

    public ISceneRunner SimulateScreenTouchRelease(int index, bool doubleTap = false)
    {
        ThrowIfDisposed();
        if (IsEmulateMouseFromTouch)
            SimulateEmulatedMouse(() => SimulateMouseButtonRelease(MouseButton.Left));

        using var inputEvent = new InputEventScreenTouch
        {
            WindowId = Scene().GetWindow().GetWindowId(),
            Index = index,
            Position = GetScreenTouchDragPosition(index),
            DoubleTap = doubleTap,
            Pressed = false
        };
        return PushTouchEvent(inputEvent);
    }

    public async Task SimulateScreenTouchDragRelative(int index, Vector2 relative, double time = 1.0, Tween.TransitionType transitionType = Tween.TransitionType.Linear)
        => await DoTouchDragAt(index, GetScreenTouchDragPosition(index) + relative, time, transitionType)
            .ConfigureAwait(true);

    public async Task SimulateScreenTouchDragAbsolute(int index, Vector2 position, double time = 1.0, Tween.TransitionType transitionType = Tween.TransitionType.Linear)
        => await DoTouchDragAt(index, position, time, transitionType)
            .ConfigureAwait(true);

    public async Task SimulateScreenTouchDragDrop(int index, Vector2 position, Vector2 dropPosition, double time = 1.0, Tween.TransitionType transitionType = Tween.TransitionType.Linear)
    {
        _ = SimulateScreenTouchPress(index, position);
        await DoTouchDragAt(index, dropPosition, time, transitionType)
            .ConfigureAwait(true);
    }

    public ISceneRunner SimulateScreenTouchDrag(int index, Vector2 position)
    {
        ThrowIfDisposed();
        if (IsEmulateMouseFromTouch)
            SimulateEmulatedMouse(() => SimulateMouseMove(position));

        var relative = touchPositions.GetValueOrDefault(index, position) - position;
        touchPositions[index] = position;
        using var inputEvent = new InputEventScreenDrag
        {
            WindowId = Scene().GetWindow().GetWindowId(),
            Index = index,
            Position = position,
            Relative = relative,
            Velocity = relative / (float)SceneTree.Root.GetProcessDeltaTime(),
            Pressure = 1.0f
        };
        return PushTouchEvent(inputEvent);
    }

    public Vector2 GetScreenTouchDragPosition(int index)
    {
        ThrowIfDisposed();
        if (touchPositions.TryGetValue(index, out var position))
            return position;
        throw new InvalidOperationException($"No touch drag position for index '{index}' is set.");
    }

    public ISceneRunner SetTimeFactor(double timeFactor = 1.0)
    {
        ThrowIfDisposed();
        TimeFactor = Math.Min(9.0, timeFactor);
        ActivateTimeFactor();

        Print("set time factor: {0}", TimeFactor);
        Print("set physics iterations_per_second: {0}", SavedIterationsPerSecond * TimeFactor);
        return this;
    }

    public async Task SimulateFrames(uint frames, uint deltaPeerFrame)
    {
        ThrowIfDisposed();
        for (var frame = 0; frame <= frames; frame++)
        {
            await AwaitMillis(deltaPeerFrame)
                .ConfigureAwait(true);
        }
    }

    public async Task SimulateFrames(uint frames)
    {
        ThrowIfDisposed();
        var timeShiftFrames = Math.Max(1, frames / TimeFactor);
        for (var frame = 0; frame <= timeShiftFrames; frame++)
            _ = await ISceneRunner.SyncProcessFrame;
    }

    public Node Scene()
    {
        ThrowIfDisposed();
        return currentScene;
    }

    public IGodotMethodAwaitable<TVariant> AwaitMethod<[MustBeVariant] TVariant>(string methodName, params Variant[] args)
        where TVariant : notnull
        => new GodotMethodAwaitable<TVariant>(new StackTrace(true), Scene(), methodName, args);

    public IGodotMethodAwaitable<TVariant> AwaitMethodOn<[MustBeVariant] TVariant>(GodotObject source, string methodName, params Variant[] args)
        where TVariant : notnull
        => new GodotMethodAwaitable<TVariant>(new StackTrace(true), source, methodName, args);

    public async Task AwaitMillis(uint timeMillis)
    {
        using var tokenSource = new CancellationTokenSource();
        await Task.Delay(TimeSpan.FromMilliseconds(timeMillis), tokenSource.Token)
            .ConfigureAwait(true);
    }

    public async Task AwaitIdleFrame() => await ISceneRunner.SyncProcessFrame;

    public Variant Invoke(string name, params Variant[] args)
        => GodotObjectExtensions.Invoke(Scene(), name, args)
            .GetAwaiter()
            .GetResult()
            .ToVariant();

    public async Task<Variant> InvokeAsync(string name, params Variant[] args)
    {
        var result = await GodotObjectExtensions.Invoke(Scene(), name, args)
            .ConfigureAwait(true);
        return result.ToVariant();
    }

    public dynamic? GetProperty(string name)
    {
        if (!PropertyExists(name))
            throw new MissingFieldException($"The property '{name}' not exist on loaded scene.");
        return Scene().Get(name).UnboxVariant();
    }

    public T? GetProperty<T>(string name) => GetProperty(name);

    public void SetProperty(string name, Variant value)
    {
        if (!PropertyExists(name))
            throw new MissingFieldException($"The property '{name}' not exist on loaded scene.");
        Scene().Set(name, value);
    }

    public Node FindChild(string name, bool recursive = true, bool owned = false) =>
        Scene().FindChild(name, recursive, owned);

    public void MaximizeView()
        => MoveWindowToForeground();

    public ISceneRunner MoveWindowToForeground()
    {
        if (IsHeadless || Engine.IsEmbeddedInEditor())
            return this;
        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        DisplayServer.WindowMoveToForeground();
        return this;
    }

    public ISceneRunner MoveWindowToBackground()
    {
        if (IsHeadless || Engine.IsEmbeddedInEditor())
            return this;
        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Minimized);
        return this;
    }

    public void Dispose()
    {
        if (IsDisposed)
            return;
        try
        {
            DeactivateTimeFactor();
            ResetInputToDefault();
        }
        finally
        {
            var scene = Scene();
            IsDisposed = true;
            if (GodotObject.IsInstanceValid(scene))
            {
                if (scene.GetParent() == viewport)
                    viewport.RemoveChild(scene);
                if (SceneAutoFree)
                    scene.Free();
            }

            if (GodotObject.IsInstanceValid(viewportView))
                viewportView.Free();
            if (GodotObject.IsInstanceValid(viewport))
                viewport.Free();
        }
    }

    public Task<ISignalConstraint> AwaitSignal(string signal, params Variant[] args) =>
        new SignalAssert(Scene())
            .IsEmitted(signal, args);

    internal static MouseButtonMask ToMouseButtonMask(MouseButton button)
    {
        var button_mask = 1 << ((int)button - 1);
        return (MouseButtonMask)Enum.ToObject(typeof(MouseButtonMask), button_mask);
    }

    /// <summary>
    ///     The scene viewport size follows the window and stretch settings, the size the scene has in the game.
    /// </summary>
    /// <param name="root">The root window.</param>
    /// <returns>The scene viewport size.</returns>
    internal static Vector2I ViewportSize(Window root)
    {
        var contentScaleSize = root.ContentScaleSize;
        if (root.ContentScaleMode != Window.ContentScaleModeEnum.Disabled && contentScaleSize.X > 0 && contentScaleSize.Y > 0)
            return contentScaleSize;
        var windowOverride = new Vector2I(
            ProjectSettings.GetSetting("display/window/size/window_width_override", 0).AsInt32(),
            ProjectSettings.GetSetting("display/window/size/window_height_override", 0).AsInt32());
        if (windowOverride.X > 0 && windowOverride.Y > 0)
            return windowOverride;
        return new Vector2I(
            ProjectSettings.GetSetting("display/window/size/viewport_width").AsInt32(),
            ProjectSettings.GetSetting("display/window/size/viewport_height").AsInt32());
    }

    private protected static Node LoadScene(string resourcePath)
    {
        if (!ResourceLoader.Exists(resourcePath))
            throw new FileNotFoundException($"GdUnitSceneRunner: Can't load scene by given resource path: '{resourcePath}'. The resource does not exists.");
        if (!resourcePath.EndsWith(".tscn") && !resourcePath.EndsWith(".scn") && !resourcePath.StartsWith("uid://"))
            throw new ArgumentException($"GdUnitSceneRunner: The given resource: '{resourcePath}' is not a scene.");

        return ((PackedScene)ResourceLoader.Load(resourcePath)).Instantiate();
    }

    /// <summary>
    ///     The scene must run like in the game, Godot applies the project settings to the root viewport only,
    ///     so the scene viewport takes over all its settings and embeds the popups opened by the scene.
    /// </summary>
    private static SubViewport CreateSceneViewport(Window root, Node scene)
    {
        var sceneViewport = new SubViewport
        {
            Name = "GdUnitSceneRunnerViewport",
            Size = ViewportSize(root),
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always
        };
        foreach (var property in ClassDB.ClassGetPropertyList("Viewport", true))
        {
            if ((property["usage"].AsInt64() & (long)PropertyUsageFlags.Storage) != 0)
            {
                var propertyName = property["name"].AsStringName();
                sceneViewport.Set(propertyName, root.Get(propertyName));
            }
        }

        sceneViewport.GuiEmbedSubwindows = true;
        sceneViewport.Notification((int)Node.NotificationVpMouseEnter);
        sceneViewport.AddChild(scene);
        return sceneViewport;
    }

    /// <summary>
    ///     Godot sends the touch events emulated from mouse input to the root window only, the scene viewport needs its own.
    /// </summary>
    private static InputEvent? EmulatedTouchEvent(InputEvent inputEvent)
    {
        if (!Input.IsEmulatingTouchFromMouse() || inputEvent.Device == InputEvent.DeviceIdEmulation)
            return null;
        return inputEvent switch
        {
            InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseButton => new InputEventScreenTouch
            {
                Device = (int)InputEvent.DeviceIdEmulation,
                Position = mouseButton.Position,
                Pressed = mouseButton.Pressed,
                Canceled = mouseButton.Canceled,
                DoubleTap = mouseButton.DoubleClick
            },
            InputEventMouseMotion mouseMotion when (mouseMotion.ButtonMask & MouseButtonMask.Left) != 0 => new InputEventScreenDrag
            {
                Device = (int)InputEvent.DeviceIdEmulation,
                Position = mouseMotion.Position,
                Relative = mouseMotion.Relative,
                ScreenRelative = mouseMotion.ScreenRelative,
                Velocity = mouseMotion.Velocity,
                ScreenVelocity = mouseMotion.ScreenVelocity,
                Pressure = mouseMotion.Pressure,
                Tilt = mouseMotion.Tilt,
                PenInverted = mouseMotion.PenInverted
            },
            _ => null
        };
    }

    // ReSharper disable once UnusedMethodReturnValue.Local
    private static bool HandleActionEvent(InputEventAction actionEvent)
    {
        if (!InputMap.EventIsAction(actionEvent, actionEvent.Action, true))
            return false;
        if (actionEvent.IsPressed())
            Input.ActionPress(actionEvent.Action, InputMap.ActionGetDeadzone(actionEvent.Action));
        else
            Input.ActionRelease(actionEvent.Action);
        return true;
    }

    private void ResetInputToDefault()
    {
        // reset all mouse buttons to the initial state if it needs
        foreach (var button in mouseButtonOnPress)
        {
            if (Input.IsMouseButtonPressed(button))
                _ = SimulateMouseButtonRelease(button);
        }

        mouseButtonOnPress.Clear();

        foreach (var key in keyOnPress)
        {
            if (Input.IsKeyPressed(key))
                _ = SimulateKeyRelease(key);
        }

        keyOnPress.Clear();

        foreach (var action in actionOnPress)
        {
            if (Input.IsActionPressed(action))
                _ = SimulateActionRelease(action);
        }

        actionOnPress.Clear();

        Input.FlushBufferedEvents();
    }

    private SceneRunner SimulateKey(Key keyCode, bool pressed)
    {
        ThrowIfDisposed();
        PrintCurrentFocus();
        var inputEvent = new InputEventKey
        {
            Pressed = pressed,
            Keycode = keyCode,
            PhysicalKeycode = keyCode,
            Unicode = (long)keyCode,
            AltPressed = keyCode == Key.Alt,
            ShiftPressed = keyCode == Key.Shift,
            CtrlPressed = keyCode == Key.Ctrl
        };
        ApplyInputModifiers(inputEvent);
        if (pressed)
            keyOnPress.Add(keyCode);
        else
            _ = keyOnPress.Remove(keyCode);
        return HandleInputEvent(inputEvent);
    }

    private void ApplyInputModifiers(InputEventWithModifiers inputEvent)
    {
        if (LastInputEvent is InputEventWithModifiers lastInputEvent)
        {
            inputEvent.MetaPressed = inputEvent.MetaPressed || lastInputEvent.MetaPressed;
            inputEvent.AltPressed = inputEvent.AltPressed || lastInputEvent.AltPressed;
            inputEvent.ShiftPressed = inputEvent.ShiftPressed || lastInputEvent.ShiftPressed;
            inputEvent.CtrlPressed = inputEvent.CtrlPressed || lastInputEvent.CtrlPressed;
        }
    }

    private void ApplyInputMouseMask(InputEvent inputEvent)
    {
        // first apply the last mask
        if (LastInputEvent is InputEventMouse lastInputEvent && inputEvent is InputEventMouse ie)
            ie.ButtonMask |= lastInputEvent.ButtonMask;
        if (inputEvent is InputEventMouseButton inputEventMouseButton)
        {
            var mask = ToMouseButtonMask(inputEventMouseButton.ButtonIndex);
            if (inputEventMouseButton.IsPressed())
                inputEventMouseButton.ButtonMask |= mask;
            else
                inputEventMouseButton.ButtonMask ^= mask;
        }
    }

    private void ApplyInputMousePosition(InputEvent inputEvent)
    {
        if (LastInputEvent is InputEventMouse lastInputEvent && inputEvent is InputEventMouseButton ie)
        {
            ie.Position = lastInputEvent.Position;
            ie.GlobalPosition = lastInputEvent.GlobalPosition;
        }
    }

    /// <summary>
    ///     The scene viewport is shown scaled in the window, Godot reports the global position of mouse events in window coordinates.
    /// </summary>
    private Vector2 ToGlobalPosition(Vector2 viewportPosition)
    {
        if (GodotObject.IsInstanceValid(viewportView))
            return viewportView.Position + viewportView.ToViewPosition(viewportPosition);
        return viewportPosition;
    }

    /// <summary>
    ///     for handling read https://docs.godotengine.org/en/stable/tutorials/inputs/inputevent.html?highlight=inputevent#how-does-it-work.
    /// </summary>
    private SceneRunner HandleInputEvent(InputEvent inputEvent)
    {
        if (isMouseEmulated && inputEvent is InputEventMouse)
            inputEvent.Device = (int)InputEvent.DeviceIdEmulation;
        Input.ParseInputEvent(inputEvent);
        if (inputEvent is InputEventAction actionEvent)
            _ = HandleActionEvent(actionEvent);
        Input.FlushBufferedEvents();

        if (GodotObject.IsInstanceValid(Scene()))
        {
            using var touchEvent = EmulatedTouchEvent(inputEvent);
            if (touchEvent != null)
                viewport.PushInput(touchEvent);
            viewport.PushInput(inputEvent);
            Print(() => $"	process event {Scene()} ({SceneName()}) <- {inputEvent.AsText()}");
        }

        // save the last input event needs to be merged with the next InputEventMouseButton
        LastInputEvent = inputEvent;
        return this;
    }

    private async Task DoTouchDragAt(int index, Vector2 dropPosition, double time, Tween.TransitionType transitionType)
    {
        var startPosition = GetScreenTouchDragPosition(index);
        var windowId = Scene().GetWindow().GetWindowId();
        var position = startPosition;
        var elapsed = 0.0;
        while (position != dropPosition)
        {
            _ = await ISceneRunner.SyncProcessFrame;
            ThrowIfDisposed();
            var delta = SceneTree.Root.GetProcessDeltaTime();
            elapsed += delta;
            var nextPosition = elapsed >= time
                ? dropPosition
                : Tween.InterpolateValue(startPosition, dropPosition - startPosition, elapsed, time, transitionType, Tween.EaseType.InOut).AsVector2();
            if (IsEmulateMouseFromTouch)
                SimulateEmulatedMouse(() => SimulateMouseMove(nextPosition));

            using var dragEvent = new InputEventScreenDrag
            {
                WindowId = windowId,
                Index = index,
                Position = nextPosition,
                Relative = nextPosition - position,
                Velocity = (nextPosition - position) / (float)delta,
                Pressure = 1.0f
            };
            _ = PushTouchEvent(dragEvent);
            position = nextPosition;
            touchPositions[index] = position;
        }

        if (IsEmulateMouseFromTouch)
        {
            SimulateEmulatedMouse(() =>
            {
                _ = SimulateMouseMove(dropPosition);
                _ = SimulateMouseButtonRelease(MouseButton.Left);
            });
        }

        using var dropEvent = new InputEventScreenTouch
        {
            WindowId = windowId,
            Index = index,
            Position = dropPosition,
            Pressed = false
        };
        _ = PushTouchEvent(dropEvent);
        _ = await ISceneRunner.SyncProcessFrame;
    }

    /// <summary>
    ///     The mouse events emulated from touch input are marked as emulated, so no touch events are emulated from them again.
    /// </summary>
    private void SimulateEmulatedMouse(Action simulate)
    {
        isMouseEmulated = true;
        try
        {
            simulate();
        }
        finally
        {
            isMouseEmulated = false;
        }
    }

    private SceneRunner PushTouchEvent(InputEvent touchEvent)
    {
        viewport.PushInput(touchEvent);
        Print(() => $"\tprocess event {Scene()} ({SceneName()}) <- {touchEvent.AsText()}");
        return this;
    }

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(IsDisposed, typeof(ISceneRunner));

    private void ActivateTimeFactor()
    {
        if (Verbose)
            Console.WriteLine($"ActivateTimeFactor: Engine.TimeScale={TimeFactor}, Engine.PhysicsTicksPerSecond={(int)(SavedIterationsPerSecond * TimeFactor)}");
        Engine.TimeScale = TimeFactor;
        Engine.PhysicsTicksPerSecond = (int)(SavedIterationsPerSecond * TimeFactor);
    }

    private void DeactivateTimeFactor()
    {
        if (Verbose)
            Console.WriteLine($"ActivateTimeFactor: Engine.TimeScale={1}, Engine.PhysicsTicksPerSecond={SavedIterationsPerSecond}");
        Engine.TimeScale = 1;
        Engine.PhysicsTicksPerSecond = SavedIterationsPerSecond;
    }

    private void Print(string message, params object[] args)
    {
        if (Verbose)
            Console.WriteLine(message, args);
    }

    private void Print(Func<string> message)
    {
        if (Verbose)
            Console.WriteLine(message());
    }

    private void PrintCurrentFocus()
    {
        if (!Verbose || !GodotObject.IsInstanceValid(Scene()))
            return;
        var focusedNode = (Scene() as Control)?.Owner; // .GetFocusOwner();

        if (focusedNode != null)
            Console.WriteLine("	focus on {0}", focusedNode);
        else
            Console.WriteLine("	no focus set");
    }

    private string SceneName()
    {
        var scene = Scene();
        if (scene.GetScript().Obj is not GDScript sceneScript)
            return scene.Name.ToString();
        return sceneScript.ResourceName.GetBaseName();
    }

    private bool PropertyExists(string name)
    {
        var scene = Scene();
        return scene.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null
               || scene.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Any(field => field.Name.Equals(name, StringComparison.Ordinal))
               || scene.GetPropertyList().Any(p => p["name"].VariantEquals(name));
    }

    private sealed class GodotMethodAwaitable<[MustBeVariant] TVariant> : IGodotMethodAwaitable<TVariant>
        where TVariant : notnull
    {
        private readonly StackTrace stackTrace;

        public GodotMethodAwaitable(StackTrace stackTrace, GodotObject instance, string methodName, params Variant[] args)
        {
            Instance = instance;
            MethodName = methodName;
            Args = args;
            this.stackTrace = stackTrace;
            if (!Instance.HasMethod(MethodName) && Instance.GetType().GetMethod(methodName) == null)
                throw new MissingMethodException($"The method '{MethodName}' not exist on loaded scene.");
        }

        private string MethodName { get; }

        private GodotObject Instance { get; }

        private Variant[] Args { get; }

        public Task<IGodotMethodAwaitable<TVariant>> IsEqual(TVariant expected)
            => CallAndWaitIsFinished(current => AssertThat(current).IsEqual(expected));

        public Task<IGodotMethodAwaitable<TVariant>> IsNull()
            => CallAndWaitIsFinished(current => AssertThat(current).IsNull());

        public Task<IGodotMethodAwaitable<TVariant>> IsNotNull()
            => CallAndWaitIsFinished(current => AssertThat(current).IsNotNull());

        private Task<IGodotMethodAwaitable<TVariant>> CallAndWaitIsFinished(Action<object?> assertion)
        {
            var signalCancellationToken = new CancellationTokenSource();
            var continuation = Task.Run<IGodotMethodAwaitable<TVariant>>(async () =>
            {
                // sync to the main thread
                _ = await GodotObjectExtensions.SyncProcessFrame;
                var invoke = GodotObjectExtensions.Invoke(Instance, MethodName, Args);
                while (!invoke.IsCompleted)
                {
                    _ = await GodotObjectExtensions.SyncProcessFrame;
                    if (signalCancellationToken.IsCancellationRequested)
                        throw new ExecutionTimeoutException("CallAndWaitIsFinished cancel requested", stackTrace);
                }

                var value = await invoke.ConfigureAwait(false);

                assertion(value);
                return this;
            });
            GodotSignalCollector.TaskCancellations[continuation.Id] = signalCancellationToken;
            _ = continuation.ContinueWith(
                _ =>
                {
                    if (GodotSignalCollector.TaskCancellations.TryRemove(continuation.Id, out var cts))
                        cts.Dispose();
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            return continuation;
        }
    }
}

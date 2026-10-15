// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core;

using System.Diagnostics;
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

internal sealed class SceneRunner : ISceneRunner
{
    private readonly ICollection<string> actionOnPress = new HashSet<string>();
    private readonly Node currentScene;
    private readonly ICollection<Key> keyOnPress = new HashSet<Key>();
    private readonly ICollection<MouseButton> mouseButtonOnPress = new HashSet<MouseButton>();

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
        SceneTree.Root.AddChild(this.currentScene);
        SavedIterationsPerSecond = Engine.PhysicsTicksPerSecond;
        _ = SetTimeFactor();
    }

    private SceneTree SceneTree { get; }

    private bool Verbose { get; }

    private bool SceneAutoFree { get; }

    private double TimeFactor { get; set; }

    private int SavedIterationsPerSecond { get; }

    private InputEvent? LastInputEvent { get; set; }

    private bool IsDisposed { get; set; }

    public ISceneRunner SimulateActionPress(string action)
    {
        var inputEvent = new InputEventAction
        {
            Pressed = true,
            Action = action
        };
        actionOnPress.Add(action);
        return HandleInputEvent(inputEvent);
    }

    public ISceneRunner SimulateActionPressed(string action)
    {
        _ = SimulateActionPress(action);
        _ = SimulateActionRelease(action);
        return this;
    }

    public ISceneRunner SimulateActionRelease(string action)
    {
        var inputEvent = new InputEventAction
        {
            Pressed = false,
            Action = action
        };
        _ = actionOnPress.Remove(action);
        return HandleInputEvent(inputEvent);
    }

    public ISceneRunner SimulateKeyPress(Key keyCode, bool shiftPressed = false, bool controlPressed = false)
    {
        PrintCurrentFocus();
        var inputEvent = new InputEventKey
        {
            Pressed = true,
            Keycode = keyCode,
            PhysicalKeycode = keyCode,
            Unicode = (long)keyCode,
            AltPressed = keyCode == Key.Alt,
            ShiftPressed = shiftPressed || keyCode == Key.Shift,
            CtrlPressed = controlPressed || keyCode == Key.Ctrl
        };
        ApplyInputModifiers(inputEvent);
        keyOnPress.Add(keyCode);
        return HandleInputEvent(inputEvent);
    }

    public ISceneRunner SimulateKeyPressed(Key keyCode, bool shift = false, bool control = false)
    {
        _ = SimulateKeyPress(keyCode, shift, control);
        _ = SimulateKeyRelease(keyCode, shift, control);
        return this;
    }

    public ISceneRunner SimulateKeyRelease(Key keyCode, bool shiftPressed = false, bool controlPressed = false)
    {
        PrintCurrentFocus();
        var inputEvent = new InputEventKey
        {
            Pressed = false,
            Keycode = keyCode,
            PhysicalKeycode = keyCode,
            Unicode = (long)keyCode,
            AltPressed = keyCode == Key.Alt,
            ShiftPressed = shiftPressed || keyCode == Key.Shift,
            CtrlPressed = controlPressed || keyCode == Key.Ctrl
        };
        ApplyInputModifiers(inputEvent);
        _ = keyOnPress.Remove(keyCode);
        return HandleInputEvent(inputEvent);
    }

    public ISceneRunner SetMousePos(Vector2 position)
    {
        var inputEvent = new InputEventMouseMotion
        {
            Position = position,
            GlobalPosition = GetGlobalMousePosition()
        };
        ApplyInputModifiers(inputEvent);
        return HandleInputEvent(inputEvent);
    }

    public Vector2 GetMousePosition()
    {
        if (LastInputEvent is InputEventMouse me)
            return me.Position;
        return currentScene.GetViewport().GetMousePosition();
    }

    public Vector2 GetGlobalMousePosition() =>
        SceneTree.Root.GetMousePosition();

    public ISceneRunner SimulateMouseMove(Vector2 position)
    {
        var inputEvent = new InputEventMouseMotion
        {
            Position = position,
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

    public ISceneRunner SetTimeFactor(double timeFactor = 1.0)
    {
        TimeFactor = Math.Min(9.0, timeFactor);
        ActivateTimeFactor();

        Print("set time factor: {0}", TimeFactor);
        Print("set physics iterations_per_second: {0}", SavedIterationsPerSecond * TimeFactor);
        return this;
    }

    public async Task SimulateFrames(uint frames, uint deltaPeerFrame)
    {
        for (var frame = 0; frame <= frames; frame++)
        {
            await AwaitMillis(deltaPeerFrame)
                .ConfigureAwait(true);
        }
    }

    public async Task SimulateFrames(uint frames)
    {
        var timeShiftFrames = Math.Max(1, frames / TimeFactor);
        for (var frame = 0; frame <= timeShiftFrames; frame++)
            _ = await ISceneRunner.SyncProcessFrame;
    }

    public Node? Scene()
        => IsDisposed ? null : currentScene;

    public IGodotMethodAwaitable<TVariant> AwaitMethod<[MustBeVariant] TVariant>(string methodName)
        where TVariant : notnull
        => new GodotMethodAwaitable<TVariant>(new StackTrace(true), currentScene, methodName);

    public async Task AwaitMillis(uint timeMillis)
    {
        using var tokenSource = new CancellationTokenSource();
        await Task.Delay(TimeSpan.FromMilliseconds(timeMillis), tokenSource.Token)
            .ConfigureAwait(true);
    }

    public async Task AwaitIdleFrame() => await ISceneRunner.SyncProcessFrame;

    public Variant Invoke(string name, params Variant[] args)
        => GodotObjectExtensions.Invoke(currentScene, name, args)
            .GetAwaiter()
            .GetResult()
            .ToVariant();

    public async Task<Variant> InvokeAsync(string name, params Variant[] args)
    {
        var result = await GodotObjectExtensions.Invoke(currentScene, name, args)
            .ConfigureAwait(true);
        return result.ToVariant();
    }

    public dynamic? GetProperty(string name)
    {
        if (!PropertyExists(name))
            throw new MissingFieldException($"The property '{name}' not exist on loaded scene.");
        return currentScene.Get(name).UnboxVariant();
    }

    public T? GetProperty<T>(string name) => GetProperty(name);

    public void SetProperty(string name, Variant value)
    {
        if (!PropertyExists(name))
            throw new MissingFieldException($"The property '{name}' not exist on loaded scene.");
        currentScene.Set(name, value);
    }

    public Node FindChild(string name, bool recursive = true, bool owned = false) =>
        currentScene.FindChild(name, recursive, owned);

    public void MaximizeView()
    {
        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        DisplayServer.WindowMoveToForeground();
    }

    public void Dispose()
    {
        if (IsDisposed)
            return;
        DeactivateTimeFactor();
        ResetInputToDefault();
        SceneTree.Root.RemoveChild(currentScene);
        if (SceneAutoFree && GodotObject.IsInstanceValid(currentScene))
            currentScene.Free();

        IsDisposed = true;
    }

    public Task<ISignalConstraint> AwaitSignal(string signal, params Variant[] args) =>
        new SignalAssert(currentScene)
            .IsEmitted(signal, args);

    internal static MouseButtonMask ToMouseButtonMask(MouseButton button)
    {
        var button_mask = 1 << ((int)button - 1);
        return (MouseButtonMask)Enum.ToObject(typeof(MouseButtonMask), button_mask);
    }

    private static Node LoadScene(string resourcePath)
    {
        if (!ResourceLoader.Exists(resourcePath))
            throw new FileNotFoundException($"GdUnitSceneRunner: Can't load scene by given resource path: '{resourcePath}'. The resource does not exists.");
        if (!resourcePath.EndsWith(".tscn") && !resourcePath.EndsWith(".scn") && !resourcePath.StartsWith("uid://"))
            throw new ArgumentException($"GdUnitSceneRunner: The given resource: '{resourcePath}' is not a scene.");

        return ((PackedScene)ResourceLoader.Load(resourcePath)).Instantiate();
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
            ie.Position = lastInputEvent.Position;
    }

    /// <summary>
    ///     for handling read https://docs.godotengine.org/en/stable/tutorials/inputs/inputevent.html?highlight=inputevent#how-does-it-work.
    /// </summary>
    private SceneRunner HandleInputEvent(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouse mouseEvent)
            Input.WarpMouse(mouseEvent.Position);
        Input.ParseInputEvent(inputEvent);
        if (inputEvent is InputEventAction actionEvent)
            _ = HandleActionEvent(actionEvent);
        Input.FlushBufferedEvents();

        if (GodotObject.IsInstanceValid(currentScene))
        {
            Print($"	process event {currentScene} ({SceneName()}) <- {inputEvent.AsText()}");
            if (currentScene.HasMethod("_gui_input"))
                _ = currentScene.Call("_gui_input", inputEvent);
            if (currentScene.HasMethod("_unhandled_input"))
                _ = currentScene.Call("_unhandled_input", inputEvent);
            currentScene.GetViewport().SetInputAsHandled();
        }

        // save the last input event needs to be merged with the next InputEventMouseButton
        LastInputEvent = inputEvent;
        return this;
    }

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

    private void PrintCurrentFocus()
    {
        if (!Verbose)
            return;
        var focusedNode = (currentScene as Control)?.Owner; // .GetFocusOwner();

        if (focusedNode != null)
            Console.WriteLine("	focus on {0}", focusedNode);
        else
            Console.WriteLine("	no focus set");
    }

    private string SceneName()
    {
        if (currentScene.GetScript().Obj is not GDScript sceneScript)
            return currentScene.Name.ToString();
        return sceneScript.ResourceName.GetBaseName();
    }

    private bool PropertyExists(string name)
        => currentScene.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null
           || currentScene.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Any(field => field.Name.Equals(name, StringComparison.Ordinal))
           || currentScene.GetPropertyList().Any(p => p["name"].VariantEquals(name));

    private sealed class GodotMethodAwaitable<[MustBeVariant] TVariant> : IGodotMethodAwaitable<TVariant>
        where TVariant : notnull
    {
        private readonly StackTrace stackTrace;

        public GodotMethodAwaitable(StackTrace stackTrace, Node instance, string methodName, params Variant[] args)
        {
            Instance = instance;
            MethodName = methodName;
            Args = args;
            this.stackTrace = stackTrace;
            if (!Instance.HasMethod(MethodName) && Instance.GetType().GetMethod(methodName) == null)
                throw new MissingMethodException($"The method '{MethodName}' not exist on loaded scene.");
        }

        private string MethodName { get; }

        private Node Instance { get; }

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

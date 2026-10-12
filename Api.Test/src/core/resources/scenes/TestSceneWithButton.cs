namespace GdUnit4.Tests.Core.Resources.Scenes;

using System;
using System.Diagnostics;

using Godot;

using Timer = Godot.Timer;

public partial class TestSceneWithButton : Control
{
    [Signal]
    public delegate void GameExitedEventHandler();

    [Signal]
    public delegate void GameStartedEventHandler();

    [Signal]
    public delegate void GameStoppedEventHandler();

    public enum GState
    {
        Initializing,
        Started,
        Stopped,
        Running,
        Exiting
    }

    public Vector2 LastMousePosition { get; private set; }

    public MouseButton LastMouseButton { get; private set; }

    public Key LastKeyPressed { get; private set; }

    private bool LetsThrowAnException { get; set; }

    public GState GameState { get; set; } = GState.Initializing;

    public override void _Ready()
    {
        GD.Print("_Ready");
        Connect(SignalName.GameStarted, Callable.From(StartGame));
    }

    public override void _Input(InputEvent @event)
    {
        Debug.Assert(@event != null, nameof(@event) + " != null");
        GD.PrintS(@event.AsText());
        if (@event is InputEventMouseMotion mouseMotion)
        {
            LastMousePosition = mouseMotion.Position;
            return;
        }

        if (@event is InputEventMouseButton mouseButton)
        {
            LastMousePosition = mouseButton.Position;
            LastMouseButton = mouseButton.ButtonIndex;
            return;
        }

        if (@event is InputEventKey keyEvent)
        {
            if (keyEvent.Pressed)
            {
                LastKeyPressed = keyEvent.Keycode;
                if (keyEvent.Keycode == Key.Space)
                    EmitSignal(SignalName.GameStarted);
                if (keyEvent.Keycode == Key.E)
                    LetsThrowAnException = true;
            }
        }
    }

    public override async void _Process(double delta)
    {
        var deltaMs = delta * 1000.0;
        GD.Print($" Delta time: {deltaMs:F2} ms");

        if (LetsThrowAnException)
        {
            ThrowTestException();
            LetsThrowAnException = false;
        }

        if (GameState == GState.Started)
        {
            GameState = GState.Running;
            GD.PrintS("Try Game stopping");

            // We wait 100ms before we emit game stopped signal
            var timer = GetTree().CreateTimer(.2);
            await ToSignal(timer, Timer.SignalName.Timeout);
            GD.PrintS("Game stopped");
            GameState = GState.Stopped;
            EmitSignal(SignalName.GameStopped);
        }
    }

    public void OnButtonPressed()
    {
        GD.Print("OnButtonPressed");
        EmitSignal(SignalName.GameStarted);
    }

    public void ThrowTestException()
        => throw new InvalidOperationException("Method execution failed");

    private void StartGame()
    {
        GameState = GState.Started;
        GD.PrintS("Game started");
    }
}

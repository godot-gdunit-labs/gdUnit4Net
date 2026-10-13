namespace GdUnit4.Tests.Core;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using GdUnit4.Asserts;
using GdUnit4.Core.Execution.Exceptions;

using Godot;

using Resources.Scenes;

using static Assertions;

[RequireGodotRuntime]
[TestSuite]
public sealed class SceneRunnerCSharpSceneTest
{
#nullable disable
    private ISceneRunner sceneRunner;
#nullable enable

    [Before]
    public void Setup() =>

        // use a dedicated FPS because we calculate frames by time
        Engine.PhysicsTicksPerSecond = 60;

    [After]
    public void TearDown()
        => Engine.PhysicsTicksPerSecond = 60;

    [BeforeTest]
    public void BeforeTest()
    {
        sceneRunner = ISceneRunner.Load("res://src/core/resources/scenes/TestSceneCSharp.tscn", true);
        AssertThat(sceneRunner).IsNotNull();
    }

    [TestCase]
    public void LoadSceneInvalidResource()
    {
        AssertThrown(() => ISceneRunner.Load("res://src/core/resources/scenes/NotExistingScene.tscn", true))
            .IsInstanceOf<FileNotFoundException>()
            .HasMessage("GdUnitSceneRunner: Can't load scene by given resource path: 'res://src/core/resources/scenes/NotExistingScene.tscn'. The resource does not exists.");
        AssertThrown(() => ISceneRunner.Load("res://src/core/resources/scenes/TestScene.gd", true))
            .IsInstanceOf<ArgumentException>()
            .HasMessage("GdUnitSceneRunner: The given resource: 'res://src/core/resources/scenes/TestScene.gd' is not a scene.");
    }

    [TestCase]
    public void LoadSceneByUid()
    {
        using var runner = ISceneRunner.Load("uid://cn8ucy2rheu0f", true);
        AssertThat(runner.Scene())
            .IsInstanceOf<Node2D>()
            .IsNotNull();
        AssertThrown(() => ISceneRunner.Load("uid://invalid_uid", true))
            .IsInstanceOf<FileNotFoundException>()
            .HasMessage("GdUnitSceneRunner: Can't load scene by given resource path: 'uid://invalid_uid'. The resource does not exists.");
    }

    [TestCase]
    public void LoadSceneTscnFormat()
    {
        using var runner = ISceneRunner.Load("res://src/core/resources/scenes/SimpleScene.tscn", true);
        AssertThat(runner.Scene())
            .IsInstanceOf<Node2D>()
            .IsNotNull();
    }

    [TestCase]
    public void LoadSceneBinaryFormat()
    {
        using var runner = ISceneRunner.Load("res://src/core/resources/scenes/SimpleScene.scn", true);
        AssertThat(runner.Scene())
            .IsInstanceOf<Node2D>()
            .IsNotNull();
    }

    [TestCase]
    public void InitializeSceneBeforeAddingToSceneTree()
    {
        var currentScene = ((PackedScene)ResourceLoader.Load("res://src/core/resources/scenes/TestSceneWithInitialization.tscn")).Instantiate<TestSceneWithInitialization>();

        currentScene.Initialize();

        AssertThat(currentScene.MethodCalls.Count).IsEqual(1);
        AssertThat(currentScene.MethodCalls[0]).IsEqual("Initialize");

        using var runner = ISceneRunner.Load(currentScene, true);
        AssertThat(runner.Scene())
            .IsInstanceOf<TestSceneWithInitialization>()
            .IsSame(currentScene);

        // ReSharper disable once NullableWarningSuppressionIsUsed
        var actualScene = (runner.Scene() as TestSceneWithInitialization)!;
        AssertThat(actualScene.MethodCalls.Count).IsEqual(2);
        AssertThat(actualScene.MethodCalls[0]).IsEqual("Initialize");
        AssertThat(actualScene.MethodCalls[1]).IsEqual("_Ready");
    }

    [TestCase]
    public void GetProperty()
    {
        // try access to a public property
        AssertObject(sceneRunner.GetProperty("Box1")).IsInstanceOf<ColorRect>().IsNotNull();

        // try access to a private property
        AssertObject(sceneRunner.GetProperty("Box2")).IsInstanceOf<ColorRect>().IsNotNull();
        AssertObject(sceneRunner.GetProperty<ColorRect>("Box1")).IsInstanceOf<ColorRect>().IsNotNull();
        AssertObject(sceneRunner.GetProperty("initial_color")).IsEqual(Colors.Red);
        AssertObject(sceneRunner.GetProperty("nullable")).IsNull();
        AssertThrown(() => sceneRunner.GetProperty("_invalid"))
            .IsInstanceOf<MissingFieldException>()
            .HasMessage("The property '_invalid' not exist on loaded scene.");
    }

    [TestCase]
    public void SetProperty()
    {
        sceneRunner.SetProperty("initial_color", Colors.Red);
        AssertObject(sceneRunner.GetProperty("initial_color")).IsEqual(Colors.Red);
        AssertThrown(() => sceneRunner.SetProperty("_invalid", 42))
            .IsInstanceOf<MissingFieldException>()
            .HasMessage("The property '_invalid' not exist on loaded scene.");
    }

    [TestCase]
    public void InvokeSceneMethod()
    {
        AssertString(sceneRunner.Invoke("Add", 10, 12).ToString()).IsEqual("22");
        AssertThrown(() => sceneRunner.Invoke("Sub", 12, 10))
            .IsInstanceOf<MissingMethodException>()
            .HasMessage("The method 'Sub' not exist on this instance.");
    }

    [TestCase(Timeout = 1200)]
    public async Task AwaitForMilliseconds()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        await sceneRunner.AwaitMillis(1000);
        stopwatch.Stop();

        // verify we wait around 1000 ms (using 100ms offset because timing is not 100% accurate)
        AssertInt((int)stopwatch.ElapsedMilliseconds).IsBetween(900, 1100);
    }

    [TestCase(Timeout = 2000)]
    public async Task SimulateFrames()
    {
        // cap the frame rate, the color cycle runs on timers, so the frames must take a known minimum time
        var maxFps = Engine.MaxFps;
        Engine.MaxFps = 100;
        try
        {
            var box1 = sceneRunner.GetProperty<ColorRect>("Box1")!;

            // initial is white
            AssertObject(box1.Color).IsEqual(Colors.White);

            // start color cycle by invoke the function 'StartColorCycle'
            sceneRunner.Invoke("StartColorCycle");

            // we wait for 10 frames
            await sceneRunner.SimulateFrames(10);

            // after 10 frame is still white
            AssertObject(box1.Color).IsEqual(Colors.White);

            // we wait 90 more frames
            await sceneRunner.SimulateFrames(90);

            // after 100 frames the box one should be changed the color
            AssertObject(box1.Color).IsNotEqual(Colors.White);
        }
        finally
        {
            Engine.MaxFps = maxFps;
        }
    }

    [TestCase(Timeout = 1000)]
    public async Task SimulateFramesWithDelay()
    {
        var box1 = sceneRunner.GetProperty<ColorRect>("Box1")!;

        // initial is white
        AssertObject(box1.Color).IsEqual(Colors.White);

        // start color cycle by invoke the function 'StartColorCycle'
        sceneRunner.Invoke("StartColorCycle");

        // we wait for 10 frames each with a 50ms delay
        await sceneRunner.SimulateFrames(10, 50);

        // after 10 frame and in sum 500ms it should be changed to red
        AssertObject(box1.Color).IsEqual(Colors.Red);
    }

    [TestCase(Description = "Example to test a scene with do a color cycle on box one each 500ms", Timeout = 4000)]
    public async Task RunSceneColorCycle()
    {
        sceneRunner.MaximizeView();

        var box1 = sceneRunner.GetProperty<ColorRect>("Box1")!;

        // verify initial color
        AssertObject(box1.Color).IsEqual(Colors.White);

        // start color cycle by invoke the function 'StartColorCycle'
        sceneRunner.Invoke("StartColorCycle");

        // await for each color cycle is emitted
        await sceneRunner.AwaitSignal(TestScene.SignalName.PanelColorChange, box1, Colors.Red);
        AssertObject(box1.Color).IsEqual(Colors.Red);
        await sceneRunner.AwaitSignal(TestScene.SignalName.PanelColorChange, box1, Colors.Blue);
        AssertObject(box1.Color).IsEqual(Colors.Blue);
        await sceneRunner.AwaitSignal(TestScene.SignalName.PanelColorChange, box1, Colors.Green);
        AssertObject(box1.Color).IsEqual(Colors.Green);

        // AwaitOnSignal must fail after a maximum timeout of 500ms because no signal 'panel_color_change' with given args color=Yellow is emitted
        await AssertThrown(sceneRunner.AwaitSignal(TestScene.SignalName.PanelColorChange, box1, Colors.Yellow).WithTimeout(700))
            .ContinueWith(result => result.Result?
                .IsInstanceOf<TestFailedException>()
                .HasMessage("""
                    Expecting do emitting signal:
                        "PanelColorChange([$colorRectId, (1, 1, 0, 1)])"
                     by
                        $sceneId
                    """
                    .Replace("$colorRectId", AssertFailures.AsObjectId(box1))
                    .Replace("$sceneId", AssertFailures.AsObjectId(sceneRunner.Scene()))));

        // verify the box is still green
        AssertObject(box1.Color).IsEqual(Colors.Green);
    }

    [TestCase(Description = "Example to simulate the enter key is pressed to shoot a spell", Timeout = 2000)]
    public async Task RunSceneSimulateKeyPressed()
    {
        // initially no spell is fired
        AssertObject(sceneRunner.FindChild("Spell")).IsNull();

        // fire spell be pressing an enter key
        sceneRunner.SimulateKeyPressed(Key.Enter);

        // wait until the next frame
        await sceneRunner.AwaitInputProcessed();

        // verify a spell is created
        AssertObject(sceneRunner.FindChild("Spell")).IsNotNull();

        // wait until the spell is exploded after around 1 s
        var spell = sceneRunner.FindChild("Spell");

        // wait spell is exploded
        await spell.AwaitSignal(Spell.SignalName.SpellExplode, spell.GetInstanceId()).WithTimeout(1100);

        // verify spell is removed when is exploded
        AssertObject(sceneRunner.FindChild("Spell")).IsNull();
    }

    [TestCase(Description = "Example to simulate mouse pressed on buttons", Timeout = 20000)]
    public async Task RunSceneSimulateMouseEvents()
    {
        sceneRunner.MaximizeView();
        await sceneRunner.AwaitInputProcessed();

        var box1 = sceneRunner.GetProperty<ColorRect>("Box1")!;
        var box2 = sceneRunner.GetProperty<ColorRect>("Box2")!;
        var box3 = sceneRunner.GetProperty<ColorRect>("Box3")!;

        // verify initial colors
        AssertObject(box1.Color).IsEqual(Colors.White);
        AssertObject(box2.Color).IsEqual(Colors.White);
        AssertObject(box3.Color).IsEqual(Colors.White);

        // set mouse position to button one and simulate is pressed
        sceneRunner.SetMousePos(new Vector2(60, 20))
            .SimulateMouseButtonPressed(MouseButton.Left);

        // wait until all input events processed
        await sceneRunner.AwaitInputProcessed();

        // verify box one is changed to Gray
        AssertObject(box1.Color).IsEqual(Colors.Gray);
        AssertObject(box2.Color).IsEqual(Colors.White);
        AssertObject(box3.Color).IsEqual(Colors.White);

        // set mouse position to button two and simulate is pressed
        sceneRunner.SetMousePos(new Vector2(160, 20))
            .SimulateMouseButtonPressed(MouseButton.Left);

        // verify box two is changed to Gray
        AssertObject(box1.Color).IsEqual(Colors.Gray);
        AssertObject(box2.Color).IsEqual(Colors.Gray);
        AssertObject(box3.Color).IsEqual(Colors.White);

        // set mouse position to button three and simulate is pressed
        sceneRunner.SetMousePos(new Vector2(260, 20))
            .SimulateMouseButtonPressed(MouseButton.Left);

        // verify box three is changed to red and after around 1s to Gray
        AssertObject(box3.Color).IsEqual(Colors.Red);
        await sceneRunner.AwaitSignal(TestScene.SignalName.PanelColorChange, box3, Colors.Gray).WithTimeout(1100);
        AssertObject(box3.Color).IsEqual(Colors.Gray);
    }

    [TestCase(Description = "Example to wait for a specific method result", Timeout = 5000)]
    public async Task AwaitMethod()
    {
        // wait until 'ColorCycle()' returns 'black'
        await sceneRunner.AwaitMethod<string>("ColorCycle").IsEqual("black");

        // wait until 'ColorCycle()' and expect be return `red` but should fail because it ends with `black`
        await AssertThrown(sceneRunner.AwaitMethod<string>("ColorCycle").IsEqual("red"))
            .ContinueWith(result => result.Result?.HasMessage(
                """
                Expecting be equal:
                    "red"
                 but is
                    "black"
                """));

        // wait again for returns 'red' but with using a custom timeout of 500ms and expect is interrupted after 500ms
        await AssertThrown(sceneRunner.AwaitMethod<string>("ColorCycle").IsEqual("red").WithTimeout(500))
            .ContinueWith(result => result.Result?.HasMessage("Assertion: Timed out after 500ms."));
    }

    [TestCase(Description = "Example to wait for a specific method result and used time factor of 10", Timeout = 1000)]
    public async Task AwaitMethodWithTimeFactor()
    {
        sceneRunner.SetTimeFactor(10);

        // wait until 'ColorCycle()' returns 'black' (using small timeout we expect the method will now process 10 times faster)
        await sceneRunner.AwaitMethod<string>("ColorCycle").IsEqual("black").WithTimeout(300);

        // wait for returns 'red' but will never happen and expect is interrupted after 150ms
        await AssertThrown(sceneRunner.AwaitMethod<string>("ColorCycle").IsEqual("red").WithTimeout(150))
            .ContinueWith(result
                => result.Result?
                    .HasMessage("Assertion: Timed out after 150ms.")
                    .HasFileLineNumber(343));
    }

    [TestCase]
    public async Task AwaitSignal()
    {
        var box1 = sceneRunner.GetProperty<ColorRect>("Box1")!;

        // Set max time factor to minimize waiting time checked `SceneRunner.wait_func`
        sceneRunner.Invoke("StartColorCycle");

        await sceneRunner.AwaitSignal(TestScene.SignalName.PanelColorChange, box1, new Color(1, 0, 0)); // Red
        await sceneRunner.AwaitSignal(TestScene.SignalName.PanelColorChange, box1, new Color(0, 0, 1)); // Blue
        await sceneRunner.AwaitSignal(TestScene.SignalName.PanelColorChange, box1, new Color(0, 1, 0)); // Green
    }

    [TestCase]
    public async Task SceneAwaitSignal()
    {
        var runner = ISceneRunner.Load("res://src/core/resources/scenes/TestSceneWithButton.tscn", true);
        var scene = runner.Scene() as TestSceneWithButton;
        Debug.Assert(scene != null, nameof(scene) + " != null");

        await ISceneRunner.SyncProcessFrame;

        // Trigger start game
        AssertThat(scene.GameState).IsEqual(TestSceneWithButton.GState.Initializing);
        scene.OnButtonPressed();

        // The game will stop after around 300ms
        await scene.AwaitSignal(TestSceneWithButton.SignalName.GameStopped).WithTimeout(300);
    }

    [TestCase]
    public async Task MonitorSignalSequence()
    {
        var runner = ISceneRunner.Load("res://src/core/resources/scenes/TestSceneWithButton.tscn", true);
        var scene = runner.Scene() as TestSceneWithButton;
        Debug.Assert(scene != null, nameof(scene) + " != null");
        var monitor = AssertSignal(scene).StartMonitoring();

        AssertThat(scene.GameState).IsEqual(TestSceneWithButton.GState.Initializing);

        // Trigger game start
        scene.OnButtonPressed();

        // game start signal should be emitted and the state is running
        await monitor.IsEmitted(TestSceneWithButton.SignalName.GameStarted).WithTimeout(1);
        AssertThat(scene.GameState).IsEqual(TestSceneWithButton.GState.Running);

        // the game will run about 500ms, so no stop signal is emitted yet
        await monitor.IsNotEmitted(TestSceneWithButton.SignalName.GameStopped).WithTimeout(1);
        AssertThat(scene.GameState).IsEqual(TestSceneWithButton.GState.Running);

        // The game will stop after around 300ms
        await monitor.IsEmitted(TestSceneWithButton.SignalName.GameStopped).WithTimeout(300);
        AssertThat(scene.GameState).IsEqual(TestSceneWithButton.GState.Stopped);
    }

    [TestCase]
    public async Task AwaitSignalOnSpell()
    {
        // fire spell be pressing an enter key
        sceneRunner.SimulateKeyPressed(Key.Enter);

        // wait until the next frame
        await sceneRunner.AwaitInputProcessed();

        var spell = sceneRunner.FindChild("Spell");

        // wait until the spell is exploded after around 1 s
        await ISceneRunner.AwaitSignalOn(spell, Spell.SignalName.SpellExplode, spell.GetInstanceId()).WithTimeout(1100);

        // fire next spell be pressing an enter key
        sceneRunner.SimulateKeyPressed(Key.Enter);

        // wait until the next frame
        await sceneRunner.AwaitInputProcessed();
        spell = sceneRunner.FindChild("Spell");

        // use global AwaitSignalOn
        await AwaitSignalOn(spell, Spell.SignalName.SpellExplode, spell.GetInstanceId()).WithTimeout(1100);
    }
}

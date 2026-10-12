namespace GdUnit4.Tests.Core;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using GdUnit4.Asserts;
using GdUnit4.Core.Execution.Exceptions;

using Godot;

using static Assertions;

[RequireGodotRuntime]
[TestSuite]
public class SceneRunnerGDScriptSceneTest
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
        sceneRunner = ISceneRunner.Load("res://src/core/resources/scenes/TestSceneGDScript.tscn", true);
        AssertThat(sceneRunner).IsNotNull();
        sceneRunner.SetMousePos(new Vector2(600, 600));
    }

    [TestCase]
    public void LoadSceneInvalidResource()
        => AssertThrown(() => ISceneRunner.Load("res://src/core/resources/scenes/NotExistingScene.tscn", true))
            .IsInstanceOf<FileNotFoundException>()
            .HasMessage("GdUnitSceneRunner: Can't load scene by given resource path: 'res://src/core/resources/scenes/NotExistingScene.tscn'. The resource does not exists.");

    [TestCase]
    public void GetProperty()
    {
        AssertObject(sceneRunner.GetProperty("_box1")).IsInstanceOf<ColorRect>().IsNotNull();
        AssertObject(sceneRunner.GetProperty<ColorRect>("_box1")).IsInstanceOf<ColorRect>().IsNotNull();
        AssertObject(sceneRunner.GetProperty("_initial_color")).IsEqual(Colors.Red);
        AssertObject(sceneRunner.GetProperty("_nullable")).IsNull();
        AssertThrown(() => sceneRunner.GetProperty("_invalid"))
            .IsInstanceOf<MissingFieldException>()
            .HasMessage("The property '_invalid' not exist on loaded scene.");
    }

    [TestCase]
    public void SetProperty()
    {
        sceneRunner.SetProperty("_initial_color", Colors.Red);
        AssertObject(sceneRunner.GetProperty("_initial_color")).IsEqual(Colors.Red);
        AssertThrown(() => sceneRunner.SetProperty("_invalid", 42))
            .IsInstanceOf<MissingFieldException>()
            .HasMessage("The property '_invalid' not exist on loaded scene.");
    }

    [TestCase]
    public void InvokeSceneMethod()
    {
        AssertString(sceneRunner.Invoke("add", 10, 12).ToString()).IsEqual("22");
        AssertThrown(() => sceneRunner.Invoke("sub", 12, 10))
            .IsInstanceOf<MissingMethodException>()
            .HasMessage("The method 'sub' not exist on this instance.");
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
        var box1 = sceneRunner.GetProperty<ColorRect>("_box1")!;

        // initial is white
        AssertObject(box1.Color).IsEqual(Colors.White);

        // start color cycle by invoke the function 'start_color_cycle'
        sceneRunner.Invoke("start_color_cycle");

        // we wait for 10 frames
        await sceneRunner.SimulateFrames(10);

        // after 10 frame is still white
        AssertObject(box1.Color).IsEqual(Colors.White);

        // we wait 90 more frames
        await sceneRunner.SimulateFrames(90);

        // after 100 frames the box one should be changed the color
        AssertObject(box1.Color).IsNotEqual(Colors.White);
    }

    [TestCase(Timeout = 1000)]
    public async Task SimulateFramesWithDelay()
    {
        var box1 = sceneRunner.GetProperty<ColorRect>("_box1")!;

        // initial is white
        AssertObject(box1.Color).IsEqual(Colors.White);

        // start color cycle by invoke the function 'start_color_cycle'
        sceneRunner.Invoke("start_color_cycle");

        // we wait for 10 frames each with a 50ms delay
        await sceneRunner.SimulateFrames(10, 50);

        // after 10 frame and in sum 500ms is should be changed to red
        AssertObject(box1.Color).IsEqual(Colors.Red);
    }

    [TestCase(Description = "Example to test a scene with do a color cycle on box one each 500ms", Timeout = 4000)]
    public async Task RunSceneColorCycle()
    {
        sceneRunner.MaximizeView();

        var box1 = sceneRunner.GetProperty<ColorRect>("_box1")!;

        // verify initial color
        AssertObject(box1.Color).IsEqual(Colors.White);

        // start color cycle by invoke the function 'start_color_cycle'
        sceneRunner.Invoke("start_color_cycle");

        // await for each color cycle is emitted
        await sceneRunner.AwaitSignal("panel_color_change", box1, Colors.Red);
        AssertObject(box1.Color).IsEqual(Colors.Red);
        await sceneRunner.AwaitSignal("panel_color_change", box1, Colors.Blue);
        AssertObject(box1.Color).IsEqual(Colors.Blue);
        await sceneRunner.AwaitSignal("panel_color_change", box1, Colors.Green);
        AssertObject(box1.Color).IsEqual(Colors.Green);

        // AwaitOnSignal must fail after an maximum timeout of 500ms because no signal '"panel_color_change"' with given args color=Yellow is emitted
        await AssertThrown(sceneRunner.AwaitSignal("panel_color_change", box1, Colors.Yellow).WithTimeout(700))
            .ContinueWith(result => result.Result?
                .IsInstanceOf<TestFailedException>()
                .HasMessage("""
                    Expecting do emitting signal:
                        "panel_color_change([$colorRectId, (1, 1, 0, 1)])"
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
        // initial no spell is fired
        AssertObject(sceneRunner.FindChild("Spell")).IsNull();

        // fire spell be pressing enter key
        sceneRunner.SimulateKeyPressed(Key.Enter);

        // wait until next frame
        await sceneRunner.AwaitInputProcessed();

        // verify a spell is created
        AssertObject(sceneRunner.FindChild("Spell")).IsNotNull();

        // wait until spell is explode after around 1s
        var spell = sceneRunner.FindChild("Spell");

        // wait spell is exploded
        await spell.AwaitSignal("spell_explode", spell).WithTimeout(1100);

        // verify spell is removed when is explode
        AssertObject(sceneRunner.FindChild("Spell")).IsNull();
    }

    [TestCase(Description = "Example to simulate mouse pressed on buttons", Timeout = 2000)]
    public async Task RunSceneSimulateMouseEvents()
    {
        sceneRunner.MaximizeView();
        await sceneRunner.AwaitInputProcessed();

        var box1 = sceneRunner.GetProperty<ColorRect>("_box1")!;
        var box2 = sceneRunner.GetProperty<ColorRect>("_box2")!;
        var box3 = sceneRunner.GetProperty<ColorRect>("_box3")!;

        // verify initial colors
        AssertObject(box1.Color).IsEqual(Colors.White);
        AssertObject(box2.Color).IsEqual(Colors.White);
        AssertObject(box3.Color).IsEqual(Colors.White);

        // set mouse position to button one and simulate is pressed
        sceneRunner.SetMousePos(new Vector2(60, 20))
            .SimulateMouseButtonPressed(MouseButton.Left);

        // wait until next frame
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
        await sceneRunner.AwaitSignal("panel_color_change", box3, Colors.Gray).WithTimeout(1100);
        AssertObject(box3.Color).IsEqual(Colors.Gray);
    }

    [TestCase(Description = "Example to wait for a specific method result", Timeout = 5000)]
    public async Task AwaitMethod()
    {
        // wait until 'color_cycle()' returns 'black'
        await sceneRunner.AwaitMethod<string>("color_cycle").IsEqual("black");

        // wait until 'color_cycle()' and expect be return `red` but should fail because it ends with `black`
        await AssertThrown(sceneRunner.AwaitMethod<string>("color_cycle").IsEqual("red"))
            .ContinueWith(result => result.Result?.HasMessage(
                """
                Expecting be equal:
                    "red"
                 but is
                    "black"
                """));

        // wait again for returns 'red' but with using a custom timeout of 500ms and expect is interrupted after 500ms
        await AssertThrown(sceneRunner.AwaitMethod<string>("color_cycle").IsEqual("red").WithTimeout(500))
            .ContinueWith(result => result.Result?.HasMessage("Assertion: Timed out after 500ms."));
    }

    [TestCase(Description = "Example to wait for a specific method result and used time factor of 10", Timeout = 1000)]
    public async Task AwaitMethodWithTimeFactor()
    {
        sceneRunner.SetTimeFactor(10);

        // wait until 'color_cycle()' returns 'black' (using small timeout we expect the method will now processes 10 times faster)
        await sceneRunner.AwaitMethod<string>("color_cycle").IsEqual("black").WithTimeout(300);

        // wait for returns 'red' but will never happen and expect is interrupted after 150ms
        await AssertThrown(sceneRunner.AwaitMethod<string>("color_cycle").IsEqual("red").WithTimeout(150))
            .ContinueWith(result
                => result.Result?
                    .HasMessage("Assertion: Timed out after 150ms.")
                    .HasFileLineNumber(271));
    }

    [TestCase]
    public async Task AwaitSignal()
    {
        var box1 = sceneRunner.GetProperty<ColorRect>("_box1")!;

        // Set max time factor to minimize waiting time checked `SceneRunner.wait_func`
        sceneRunner.Invoke("start_color_cycle");

        await sceneRunner.AwaitSignal("panel_color_change", box1, new Color(1, 0, 0)); // Red
        await sceneRunner.AwaitSignal("panel_color_change", box1, new Color(0, 0, 1)); // Blue
        await sceneRunner.AwaitSignal("panel_color_change", box1, new Color(0, 1, 0)); // Green
    }
}

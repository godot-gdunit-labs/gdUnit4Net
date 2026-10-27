namespace GdUnit4.Tests.Core;

using System.Threading.Tasks;

using Godot;

using static Assertions;

[RequireGodotRuntime]
[TestSuite]
public sealed class SceneRunnerLiveCycleTest
{
#nullable disable
    private ISceneRunner sceneRunner;
#nullable enable

    [Before]
    public void Setup() => AssertThat(sceneRunner).IsNull();

    [After]
    public void TearDown()
    {
        AssertThat(sceneRunner).IsNotNull();
        AssertThrown(() => sceneRunner.Scene()).IsInstanceOf<ObjectDisposedException>();
    }

    [BeforeTest]
    public void BeforeTest()
    {
        sceneRunner = ISceneRunner.Load("res://src/core/resources/scenes/TestSceneCSharp.tscn", true);
        AssertThat(sceneRunner).IsNotNull();
        AssertThat(sceneRunner.Scene()).IsNotNull();
    }

    [AfterTest]
    public void AfterTest()
    {
        AssertThat(sceneRunner).IsNotNull();
        AssertThat(sceneRunner.Scene()).IsNotNull();
    }

    [TestCase]
    public void LoadAdditionalScene()
    {
        using var runner = ISceneRunner.Load("uid://cn8ucy2rheu0f", true);
        AssertThat(runner.Scene())
            .IsInstanceOf<Node2D>()
            .IsNotNull();

        // verify scene is still valid
        AssertThat(sceneRunner.Scene()).IsNotNull();

        // verify it fails when try to load a scene using null argument
#pragma warning disable CS8625, CS8600 // Converting null literal or possible null value to non-nullable type.
        AssertThrown(() => ISceneRunner.Load((Node)null, true))
            .HasMessage("SceneRunner requires a valid scene instance, but received null");
#pragma warning restore CS8625, CS8600 // Converting null literal or possible null value to non-nullable type.
    }

    [TestCase]
    public async Task DisposeSceneRunner()
    {
        var runner = ISceneRunner.Load("res://src/core/resources/scenes/TestSceneCSharp.tscn", true);
        var tree = (SceneTree)Engine.GetMainLoop();

        var currentScene = runner.Scene();
        var nodePath = currentScene?.GetPath();

        // check scene is loaded and added to the root node
        AssertThat(GodotObject.IsInstanceValid(currentScene)).IsTrue();
        AssertThat(tree.Root.GetNodeOrNull(nodePath)).IsNotNull();

        await ISceneRunner.SyncProcessFrame;
        runner.Dispose();

        await ISceneRunner.SyncProcessFrame;

        // check scene is freed and removed from the root node
        AssertThat(GodotObject.IsInstanceValid(currentScene)).IsFalse();
        AssertThat(tree.Root.GetNodeOrNull(nodePath)).IsNull();
        AssertThrown(() => runner.Scene()).IsInstanceOf<ObjectDisposedException>();
    }

    [TestCase]
    public async Task DisposeSceneRunnerAfterSceneFreedItself()
    {
        var runner = ISceneRunner.Load(new Control(), true, true);
        runner.SimulateKeyPress(Key.A);
        runner.Scene().QueueFree();
        await ISceneRunner.SyncProcessFrame;

        runner.Dispose();

        AssertThrown(() => runner.Scene()).IsInstanceOf<ObjectDisposedException>();
    }

    [TestCase]
    public void DisposeSceneRunnerAfterSceneReparented()
    {
        var parent = AutoFree(new Node());
        var scene = AutoFree(new Node2D());
        var runner = ISceneRunner.Load(scene);
        scene.Reparent(parent);

        runner.Dispose();

        AssertThat(scene.GetParent()).IsSame(parent);
    }

    [TestCase]
    public void LoadTypedSceneByInstance()
    {
        var currentScene = new Node2D();
        using var runner = ISceneRunner.Load(currentScene, true);

        AssertThat(runner.Scene()).IsSame(currentScene);
        AssertThat(runner.Scene().Position).IsEqual(Vector2.Zero);
    }

    [TestCase]
    public void LoadTypedSceneByPath()
    {
        using var runner = ISceneRunner.Load<TestScene>("res://src/core/resources/scenes/TestSceneCSharp.tscn", true);

        AssertThat(runner.Scene().IsInsideTree()).IsTrue();
    }

    [TestCase]
    public void LoadTypedSceneByPathWithWrongType()
    {
        var orphanNodes = Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount);

        AssertThrown(() => ISceneRunner.Load<Node2D>("res://src/core/resources/scenes/TestSceneCSharp.tscn", true))
            .IsInstanceOf<ArgumentException>()
            .HasMessage("GdUnitSceneRunner: The scene 'res://src/core/resources/scenes/TestSceneCSharp.tscn' has the root type 'TestScene', expected 'Node2D'.");
        AssertThat(Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount)).IsEqual(orphanNodes);
    }

    [TestCase]
    public void TypedSceneRunnerKeepsSceneTypeOnChainedCalls()
    {
        var currentScene = new Node2D();
        using var runner = ISceneRunner.Load(currentScene, true);

        var scene = runner
            .SetTimeFactor()
            .SimulateKeyPressed(Key.A)
            .SimulateMouseButtonPressed(MouseButton.Left)
            .Scene();

        AssertThat(scene.Position).IsEqual(Vector2.Zero);
        AssertThat(scene).IsSame(currentScene);
    }

    [TestCase]
    public void DisposeTypedSceneRunner()
    {
        var runner = ISceneRunner.Load(new Node2D(), true);

        runner.Dispose();

        AssertThrown(() => runner.Scene()).IsInstanceOf<ObjectDisposedException>()
            .HasPropertyValue("ObjectName", "GdUnit4.ISceneRunner");
    }

    [TestCase]
    public async Task DisposedSceneRunnerThrowsOnSceneAccess()
    {
        var runner = ISceneRunner.Load("res://src/core/resources/scenes/TestSceneCSharp.tscn", true);
        runner.SimulateMouseMove(new Vector2(10, 10));

        runner.Dispose();

        AssertThrown(() => runner.GetMousePosition()).IsInstanceOf<ObjectDisposedException>();
        AssertThrown(() => runner.SimulateMouseMove(new Vector2(20, 20))).IsInstanceOf<ObjectDisposedException>();
        AssertThrown(() => runner.SetMousePos(new Vector2(20, 20))).IsInstanceOf<ObjectDisposedException>();
        AssertThrown(() => runner.SimulateMouseButtonPress(MouseButton.Left)).IsInstanceOf<ObjectDisposedException>();
        AssertThrown(() => runner.SimulateMouseButtonRelease(MouseButton.Left)).IsInstanceOf<ObjectDisposedException>();
        AssertThrown(() => runner.SimulateKeyPress(Key.A)).IsInstanceOf<ObjectDisposedException>();
        AssertThrown(() => runner.SimulateKeyRelease(Key.A)).IsInstanceOf<ObjectDisposedException>();
        AssertThrown(() => runner.SimulateKeyPressed(Key.A)).IsInstanceOf<ObjectDisposedException>();
        AssertThrown(() => runner.SimulateActionPress("ui_accept")).IsInstanceOf<ObjectDisposedException>();
        AssertThrown(() => runner.SimulateActionRelease("ui_accept")).IsInstanceOf<ObjectDisposedException>();
        AssertThrown(() => runner.SimulateActionPressed("ui_accept")).IsInstanceOf<ObjectDisposedException>();
        AssertThrown(() => runner.SetTimeFactor(5)).IsInstanceOf<ObjectDisposedException>();
        AssertThat(Engine.TimeScale).IsEqual(1.0);
        AssertThrown(() => runner.Invoke("ColorCycle")).IsInstanceOf<ObjectDisposedException>();
        AssertThrown(() => runner.GetProperty("_box1")).IsInstanceOf<ObjectDisposedException>();
        AssertThrown(() => runner.SetProperty("_box1", default)).IsInstanceOf<ObjectDisposedException>();
        AssertThrown(() => runner.FindChild("Box1")).IsInstanceOf<ObjectDisposedException>();
        AssertThrown(() => runner.AwaitMethod<string>("ColorCycle")).IsInstanceOf<ObjectDisposedException>();
        AssertThrown(() => runner.AwaitSignal("panel_color_change")).IsInstanceOf<ObjectDisposedException>();

        var invokeAsync = await AssertThrown(runner.InvokeAsync("ColorCycle"));
        AssertObject(invokeAsync).IsNotNull();
        invokeAsync!.IsInstanceOf<ObjectDisposedException>();

        var simulateFrames = await AssertThrown(runner.SimulateFrames(10));
        AssertObject(simulateFrames).IsNotNull();
        simulateFrames!.IsInstanceOf<ObjectDisposedException>();

        var simulateFramesWithDelta = await AssertThrown(runner.SimulateFrames(10, 100));
        AssertObject(simulateFramesWithDelta).IsNotNull();
        simulateFramesWithDelta!.IsInstanceOf<ObjectDisposedException>();
    }
}

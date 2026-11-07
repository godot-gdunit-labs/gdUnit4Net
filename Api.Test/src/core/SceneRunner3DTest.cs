namespace GdUnit4.Tests.Core;

using System.Diagnostics;
using System.Threading.Tasks;

using Godot;

using Resources.Scenes;

using static Assertions;

[RequireGodotRuntime]
[TestSuite]
public sealed class SceneRunner3DTest
{
#nullable disable
    private ISceneRunner<Game3DTestScene> runner;
#nullable enable

    [BeforeTest]
    public async Task BeforeTest()
    {
        runner = ISceneRunner.Load<Game3DTestScene>("res://src/core/resources/scenes/Game3D/Game3DTestScene.tscn", true);
        await runner.AwaitInputProcessed();
    }

    [TestCase]
    public void SceneUsesOwnCamera()
        => AssertThat(runner.Scene().GetViewport().GetCamera3D()).IsSame(runner.FindChild("Camera3D"));

    [TestCase]
    public async Task PlayerFallsOntoFloor()
        => AssertThat(await AwaitPlayerOnFloor(2000)).IsTrue();

    [TestCase]
    public async Task MovePlayerByAction()
    {
        var startX = Player().Position.X;

        runner.SimulateActionPress("ui_right");
        await runner.SimulateFrames(10);
        runner.SimulateActionRelease("ui_right");

        AssertThat(Player().Position.X).IsGreater(startX);
    }

    [TestCase]
    public async Task JumpPlayerByKey()
    {
        AssertThat(await AwaitPlayerOnFloor(2000)).IsTrue();
        var monitor = AssertSignal(runner.Scene()).StartMonitoring();

        runner.SimulateKeyPress(Key.Space);
        await monitor.IsEmitted(Game3DTestScene.SignalName.Jumped).WithTimeout(500);
        runner.SimulateKeyRelease(Key.Space);

        AssertThat(Player().Velocity.Y).IsGreater(0.0f);
    }

    [TestCase]
    public async Task HoverTargetByMouse()
    {
        var monitor = AssertSignal(runner.Scene()).StartMonitoring();

        runner.SimulateMouseMove(ScreenPosition("Target"));

        await monitor.IsEmitted(Game3DTestScene.SignalName.TargetHovered).WithTimeout(500);
    }

    [TestCase]
    public async Task ClickTargetByMouse()
    {
        var monitor = AssertSignal(runner.Scene()).StartMonitoring();

        runner.SetMousePosition(ScreenPosition("Target"));
        runner.SimulateMouseButtonPressed(MouseButton.Left);

        await monitor.IsEmitted(Game3DTestScene.SignalName.TargetClicked).WithTimeout(500);
        AssertThat(runner.Scene().TargetClickPosition).IsNotNull();
    }

    [TestCase]
    public async Task ClickHudButtonOver3DScene()
    {
        var button = (Button)runner.FindChild("StartButton");
        var monitor = AssertSignal(runner.Scene()).StartMonitoring();

        runner.SetMousePosition(button.GetGlobalRect().GetCenter());
        runner.SimulateMouseButtonPressed(MouseButton.Left);

        await monitor.IsEmitted(Game3DTestScene.SignalName.StartPressed).WithTimeout(500);
    }

    private CharacterBody3D Player() => (CharacterBody3D)runner.FindChild("Player");

    private Vector2 ScreenPosition(string nodeName)
    {
        var node = (Node3D)runner.FindChild(nodeName);
        return runner.Scene().GetViewport().GetCamera3D().UnprojectPosition(node.GlobalPosition);
    }

    private async Task<bool> AwaitPlayerOnFloor(int timeoutMillis)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!Player().IsOnFloor() && stopwatch.ElapsedMilliseconds < timeoutMillis)
            await ISceneRunner.SyncProcessFrame;
        return Player().IsOnFloor();
    }
}

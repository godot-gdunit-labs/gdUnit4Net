namespace GdUnit4.Tests.E2E;

using System.IO;
using System.Threading.Tasks;

using Core;

using Godot;

using static Assertions;

/// <summary>
///     End-to-end tests that need rendered output, they only pass with a display and run in their own CI job.
/// </summary>
[RequireGodotRuntime]
[TestSuite]
public sealed class SceneRunnerRenderingE2ETest
{
    [TestCase(Timeout = 5000)]
    public async Task ShaderOutput()
    {
        using var runner = ISceneRunner.Load(SceneRunnerRenderingTest.CreateShaderScene(), true);

        await runner.SimulateFrames(5);
        await AwaitFramePostDraw();

        var image = runner.Scene().GetViewport().GetTexture().GetImage();
        AssertThat(image.GetPixel(32, 32)).IsEqual(Colors.Green);
    }

    [TestCase(Timeout = 5000)]
    public async Task VisibleOnScreenNotifier()
    {
        var scene = new Node2D();
        var notifier = new VisibleOnScreenNotifier2D { Position = new Vector2(100, 100) };
        scene.AddChild(notifier);
        using var runner = ISceneRunner.Load(scene, true);

        await runner.SimulateFrames(5);

        AssertThat(notifier.IsOnScreen()).IsTrue();
    }

    [TestCase(Timeout = 5000)]
    public async Task SceneScreenshot()
    {
        using var runner = ISceneRunner.Load("res://src/core/resources/scenes/TestSceneCSharp.tscn", true);
        await runner.SimulateFrames(1);
        await AwaitFramePostDraw();

        var image = runner.Scene().GetViewport().GetTexture().GetImage();
        AssertThat(image.IsEmpty()).IsFalse();
    }

    [TestCase(Timeout = 5000)]
    public async Task ScreenCapture()
    {
        var scene = new Control();
        scene.AddChild(new ColorRect
        {
            Color = Colors.Blue,
            Position = new Vector2(100, 100),
            Size = new Vector2(200, 100)
        });
        using var runner = ISceneRunner.Load(scene, true);
        runner.SetMousePosition(new Vector2(150, 150));

        await runner.SimulateFrames(5);
        await AwaitFramePostDraw();

        var captureFile = Path.Combine(Utils.CreateTempDir("screen_capture"), "capture.png");
        AssertThat(runner.Scene().GetViewport().GetTexture().GetImage().SavePng(captureFile)).IsEqual(Error.Ok);
        var capture = Image.LoadFromFile(captureFile);
        AssertThat(capture.GetSize()).IsEqual(new Vector2I(
            ProjectSettings.GetSetting("display/window/size/viewport_width").AsInt32(),
            ProjectSettings.GetSetting("display/window/size/viewport_height").AsInt32()));
        AssertThat(capture.GetPixel(250, 180)).IsEqual(Colors.Blue);

        // the simulated mouse cursor is drawn on the window only, the capture shows the scene below it
        AssertThat(capture.GetPixel(152, 160)).IsEqual(Colors.Blue);
    }

    private static async Task AwaitFramePostDraw()
        => await RenderingServer.Singleton.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
}

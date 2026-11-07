namespace GdUnit4.Tests.Core;

using System.Threading.Tasks;

using Godot;

using static Assertions;

/// <summary>
///     Verifies which rendering dependent features work in a scene runner with a display and in headless mode.
///     Headless Godot uses a dummy renderer, it does not render anything.
/// </summary>
[RequireGodotRuntime]
[TestSuite]
public sealed class SceneRunnerRenderingTest
{
    internal const string GreenShader = """
                                        shader_type canvas_item;

                                        void fragment() {
                                            COLOR = vec4(0.0, 1.0, 0.0, 1.0);
                                        }
                                        """;

    private static bool IsHeadless => DisplayServer.GetName() == "headless";

    [TestCase]
    public async Task VisibleOnScreenNotifier2DInScene()
    {
        var scene = AutoFree(new Node2D());
        var notifier = new VisibleOnScreenNotifier2D { Position = new Vector2(100, 100) };
        scene.AddChild(notifier);
        var monitor = AssertSignal(notifier).StartMonitoring();
        using var runner = ISceneRunner.Load(scene);

        await runner.SimulateFrames(5);

        AssertThat(notifier.IsOnScreen()).IsEqual(!IsHeadless);
        if (IsHeadless)
            await monitor.IsNotEmitted(VisibleOnScreenNotifier2D.SignalName.ScreenEntered).WithTimeout(200);
        else
            await monitor.IsEmitted(VisibleOnScreenNotifier2D.SignalName.ScreenEntered).WithTimeout(200);
    }

    [TestCase]
    public async Task VisibleOnScreenNotifier3DInScene()
    {
        var scene = AutoFree(new Node3D());
        scene.AddChild(new Camera3D { Position = new Vector3(0, 0, 5) });
        var notifier = new VisibleOnScreenNotifier3D();
        scene.AddChild(notifier);
        var monitor = AssertSignal(notifier).StartMonitoring();
        using var runner = ISceneRunner.Load(scene);

        await runner.SimulateFrames(5);

        AssertThat(notifier.IsOnScreen()).IsEqual(!IsHeadless);
        if (IsHeadless)
            await monitor.IsNotEmitted(VisibleOnScreenNotifier3D.SignalName.ScreenEntered).WithTimeout(200);
        else
            await monitor.IsEmitted(VisibleOnScreenNotifier3D.SignalName.ScreenEntered).WithTimeout(200);
    }

    [TestCase]
    public async Task ShaderOutput()
    {
        using var runner = ISceneRunner.Load(CreateShaderScene(), true);

        await runner.SimulateFrames(5);
        if (IsHeadless)
        {
            AssertThat(RenderingServer.GetVideoAdapterName()).IsEmpty();
            return;
        }

        await RenderingServer.Singleton.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);

        var image = runner.Scene().GetViewport().GetTexture().GetImage();
        AssertThat(image.GetPixel(32, 32)).IsEqual(Colors.Green);
    }

    internal static Control CreateShaderScene()
    {
        var scene = new Control();
        var rect = new ColorRect
        {
            Color = Colors.Red,
            Size = new Vector2(64, 64),
            Material = new ShaderMaterial { Shader = new Shader { Code = GreenShader } }
        };
        scene.AddChild(rect);
        return scene;
    }
}

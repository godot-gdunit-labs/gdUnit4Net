namespace GdUnit4.Tests.Core;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using GdUnit4.Core;
using GdUnit4.Core.Extensions;

using Godot;

using Resources.Scenes;

using static Assertions;

[RequireGodotRuntime]
[TestSuite]
public sealed class SceneRunnerViewportTest
{
    private const string SimpleScene = "res://src/core/resources/scenes/TestSceneCSharp.tscn";

    private static readonly string[] OverriddenSettings =
    [
        "display/mouse_cursor/custom_image",
        "display/mouse_cursor/custom_image_hotspot",
        "display/window/size/window_width_override",
        "display/window/size/window_height_override"
    ];

    private readonly Dictionary<string, Variant> savedSettings = [];

    private static bool IsHeadless => DisplayServer.GetName() == "headless";

    private static Window Root => ((SceneTree)Engine.GetMainLoop()).Root;

    [BeforeTest]
    public void BeforeTest()
    {
        foreach (var setting in OverriddenSettings)
            savedSettings[setting] = ProjectSettings.GetSetting(setting);
    }

    [AfterTest]
    public void AfterTest()
    {
        foreach (var (setting, value) in savedSettings)
            ProjectSettings.SetSetting(setting, value);
    }

    [TestCase]
    public void SceneIsDisplayedInWindow()
    {
        var runner = ISceneRunner.Load(SimpleScene, true);
        var viewportTexture = runner.Scene().GetViewport().GetTexture();

        AssertThat(FindViews(viewportTexture)).HasSize(IsHeadless ? 0 : 1);

        runner.Dispose();
        AssertThat(FindViews(viewportTexture)).IsEmpty();
    }

    [TestCase]
    public async Task SceneWithOwnCursor()
    {
        using var runner = ISceneRunner.Load<CustomCursorTestScene>("res://src/core/resources/scenes/CustomCursor/CustomCursorTestScene.tscn", true);
        var cursor = (Sprite2D)runner.FindChild("Cursor");

        runner.SetMousePosition(new Vector2(200, 150));
        await runner.SimulateFrames(2);

        AssertThat(cursor.Position).IsEqual(new Vector2(200, 150));
        if (!IsHeadless)
            AssertThat(SceneRunnerView.IsCursorVisible(Input.MouseMode)).IsFalse();
    }

    [TestCase]
    public async Task SceneWithProjectCustomCursor()
    {
        const string cursorImage = "res://src/core/resources/scenes/CustomCursor/cursor.png";
        ProjectSettings.SetSetting("display/mouse_cursor/custom_image", cursorImage);
        ProjectSettings.SetSetting("display/mouse_cursor/custom_image_hotspot", new Vector2(1, 1));
        using var runner = ISceneRunner.Load(SimpleScene, true);

        runner.SetMousePosition(new Vector2(200, 150));
        await runner.SimulateFrames(2);

        var views = Root.GetChildren().OfType<SceneRunnerView>().ToList();
        if (IsHeadless)
        {
            AssertThat(views).IsEmpty();
            return;
        }

        AssertThat(views).HasSize(1);
        AssertThat(views[0].CursorImage).IsEqual(ResourceLoader.Load<Texture2D>(cursorImage));
        AssertThat(views[0].CursorHotspot).IsEqual(new Vector2(1, 1));
        AssertThat(SceneRunnerView.IsCursorVisible(Input.MouseMode)).IsTrue();
    }

    [TestCase]
    public async Task ScenePopupIsEmbeddedInSceneViewport()
    {
        var scene = new Control();
        var option = new OptionButton
        {
            Position = new Vector2(100, 100),
            Size = new Vector2(120, 30)
        };
        option.AddItem("first");
        option.AddItem("second");
        scene.AddChild(option);
        using var runner = ISceneRunner.Load(scene, true);
        await runner.SimulateFrames(10);

        runner.SetMousePosition(new Vector2(150, 115));
        runner.SimulateMouseButtonPressed(MouseButton.Left);
        await runner.SimulateFrames(2);

        var popup = option.GetPopup();
        AssertThat(popup.Visible).IsTrue();
        AssertThat(popup.IsEmbedded()).IsTrue();

        runner.SetMousePosition(popup.Position + (popup.Size * new Vector2(0.5f, 0.75f)));
        runner.SimulateMouseButtonPressed(MouseButton.Left);
        await runner.SimulateFrames(2);

        AssertThat(option.Selected).IsEqual(1);
        AssertThat(popup.Visible).IsFalse();
    }

    [TestCase]
    public void SceneViewportTakesOverAllRootViewportProperties()
    {
        using var runner = ISceneRunner.Load(SimpleScene, true);
        var viewport = runner.Scene().GetViewport();

        var storedProperties = ClassDB.ClassGetPropertyList("Viewport", true)
            .Where(property => (property["usage"].AsInt64() & (long)PropertyUsageFlags.Storage) != 0)
            .Select(property => property["name"].AsStringName())
            .Where(propertyName => propertyName != Viewport.PropertyName.GuiEmbedSubwindows)
            .ToList();

        AssertThat(storedProperties).IsNotEmpty();
        AssertThat(storedProperties.Where(propertyName => !viewport.Get(propertyName).VariantEquals(Root.Get(propertyName)))).IsEmpty();
        AssertThat(viewport.GuiEmbedSubwindows).IsTrue();
    }

    [TestCase]
    public void SceneReceivesTouchEmulatedFromMouse()
    {
        var emulateTouch = Input.EmulateTouchFromMouse;
        Input.EmulateTouchFromMouse = true;
        try
        {
            var scene = new TouchRecorderNode();
            using var runner = ISceneRunner.Load(scene, true);

            runner.SetMousePosition(new Vector2(100, 100));
            runner.SimulateMouseButtonPress(MouseButton.Left);
            runner.SimulateMouseMove(new Vector2(120, 100));
            runner.SimulateMouseButtonRelease(MouseButton.Left);

            AssertThat(scene.TouchEvents.Select(e => (e.GetClass(), e.Get("position").AsVector2()))).ContainsExactly(
                ("InputEventScreenTouch", new Vector2(100, 100)),
                ("InputEventScreenDrag", new Vector2(120, 100)),
                ("InputEventScreenTouch", new Vector2(120, 100)));
        }
        finally
        {
            Input.EmulateTouchFromMouse = emulateTouch;
        }
    }

    [TestCase]
    public void SceneReceivesTouchOnceWithTouchAndMouseEmulation()
    {
        var emulateTouch = Input.EmulateTouchFromMouse;
        var emulateMouse = ProjectSettings.GetSetting("input_devices/pointing/emulate_mouse_from_touch");
        Input.EmulateTouchFromMouse = true;
        ProjectSettings.SetSetting("input_devices/pointing/emulate_mouse_from_touch", true);
        try
        {
            var scene = new TouchRecorderNode();
            using var runner = ISceneRunner.Load(scene, true);

            runner
                .SimulateScreenTouchPress(0, new Vector2(100, 100))
                .SimulateScreenTouchRelease(0);

            AssertThat(scene.TouchEvents.Select(e => (e.GetClass(), e.IsPressed()))).ContainsExactly(
                ("InputEventScreenTouch", true),
                ("InputEventScreenTouch", false));
        }
        finally
        {
            Input.EmulateTouchFromMouse = emulateTouch;
            ProjectSettings.SetSetting("input_devices/pointing/emulate_mouse_from_touch", emulateMouse);
        }
    }

    [TestCase]
    public async Task MoveWindowToForeground()
    {
        using var runner = ISceneRunner.Load(SimpleScene, true);
        var windowMode = DisplayServer.WindowGetMode();

        runner.MoveWindowToBackground();
        await runner.SimulateFrames(1);
        if (IsHeadless)
        {
            // there is no window in headless mode, the window stays as it is
            AssertThat(DisplayServer.WindowGetMode()).IsEqual(windowMode);
            return;
        }

        // Linux keeps the window in windowed mode, see https://github.com/godotengine/godot/issues/111119
        AssertThat(DisplayServer.WindowGetMode()).IsEqual(OS.GetName() == "Linux" ? DisplayServer.WindowMode.Windowed : DisplayServer.WindowMode.Minimized);

        runner.MoveWindowToForeground();
        await runner.SimulateFrames(1);
        AssertThat(DisplayServer.WindowGetMode()).IsEqual(DisplayServer.WindowMode.Windowed);
    }

    [TestCase]
    public void GlobalMousePositionIsInWindowCoordinates()
    {
        using var runner = ISceneRunner.Load(SimpleScene, true);

        runner.SetMousePosition(new Vector2(100, 50));

        var view = Root.GetChildren().OfType<SceneRunnerView>().SingleOrDefault();
        var expected = view == null ? new Vector2(100, 50) : view.Position + view.ToViewPosition(new Vector2(100, 50));
        AssertThat(runner.GetGlobalMousePosition()).IsEqual(expected);
        AssertThat(runner.GetMousePosition()).IsEqual(new Vector2(100, 50));
    }

    [TestCase]
    public void MaximizeViewShowsWindow()
    {
        using var runner = ISceneRunner.Load(SimpleScene, true);
        var windowMode = DisplayServer.WindowGetMode();

        runner.MaximizeView();

        // there is no window in headless mode, the view stays as it is
        AssertThat(DisplayServer.WindowGetMode()).IsEqual(IsHeadless ? windowMode : DisplayServer.WindowMode.Windowed);
    }

    [TestCase]
    public async Task LoadSceneWithAudioPlayer()
    {
        var scene = new Node();
        scene.AddChild(new AudioStreamPlayer { Autoplay = true, Stream = new AudioStreamGenerator() });
        using var runner = ISceneRunner.Load(scene, true);

        await runner.SimulateFrames(1);

        AssertThat(runner.Scene().IsInsideTree()).IsTrue();
    }

    [TestCase]
    public void SceneViewportSizeFromContentScaleSize()
    {
        var window = AutoFree(new Window
        {
            ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems,
            ContentScaleSize = new Vector2I(320, 180)
        });

        AssertThat(SceneRunner.ViewportSize(window)).IsEqual(new Vector2I(320, 180));
    }

    [TestCase]
    public void SceneViewportSizeWithoutContentScaleSize()
    {
        var window = AutoFree(new Window
        {
            ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems,
            ContentScaleSize = Vector2I.Zero
        });
        ProjectSettings.SetSetting("display/window/size/window_width_override", 800);
        ProjectSettings.SetSetting("display/window/size/window_height_override", 600);

        AssertThat(SceneRunner.ViewportSize(window)).IsEqual(new Vector2I(800, 600));
    }

    [TestCase]
    public void SceneViewportSizeFromWindowOverride()
    {
        var window = AutoFree(new Window());
        ProjectSettings.SetSetting("display/window/size/window_width_override", 800);
        ProjectSettings.SetSetting("display/window/size/window_height_override", 600);

        AssertThat(SceneRunner.ViewportSize(window)).IsEqual(new Vector2I(800, 600));
    }

    [TestCase]
    public void SceneViewportSizeFromProjectViewportSize()
    {
        var window = AutoFree(new Window());
        ProjectSettings.SetSetting("display/window/size/window_width_override", 0);
        ProjectSettings.SetSetting("display/window/size/window_height_override", 0);

        AssertThat(SceneRunner.ViewportSize(window)).IsEqual(new Vector2I(
            ProjectSettings.GetSetting("display/window/size/viewport_width").AsInt32(),
            ProjectSettings.GetSetting("display/window/size/viewport_height").AsInt32()));
    }

    private static List<Node> FindViews(Texture2D viewportTexture)
        => Root.GetChildren()
            .Where(node => node is TextureRect textureRect && textureRect.Texture == viewportTexture)
            .ToList();
}

public partial class TouchRecorderNode : Node
{
    public List<InputEvent> TouchEvents { get; } = [];

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventScreenTouch or InputEventScreenDrag)
            TouchEvents.Add(@event);
    }
}

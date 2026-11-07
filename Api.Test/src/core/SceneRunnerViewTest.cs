namespace GdUnit4.Tests.Core;

using GdUnit4.Core;

using Godot;

using static Assertions;

[RequireGodotRuntime]
[TestSuite]
public sealed class SceneRunnerViewTest
{
    private const string CursorImage = "res://src/core/resources/scenes/CustomCursor/cursor.png";

    [AfterTest]
    public void AfterTest()
    {
        ProjectSettings.SetSetting("display/mouse_cursor/custom_image", string.Empty);
        ProjectSettings.SetSetting("display/mouse_cursor/custom_image_hotspot", Vector2.Zero);
    }

    #region ToViewPosition

    [TestCase]
    public void ToViewPositionSameSize()
    {
        var view = CreateView(new Vector2(1152, 648));

        AssertThat(view.ToViewPosition(new Vector2(100, 50))).IsEqual(new Vector2(100, 50));
    }

    [TestCase]
    public void ToViewPositionScaled()
    {
        var view = CreateView(new Vector2(576, 324));

        AssertThat(view.ToViewPosition(new Vector2(100, 50))).IsEqual(new Vector2(50, 25));
    }

    [TestCase]
    public void ToViewPositionCentered()
    {
        var view = CreateView(new Vector2(1152, 748));

        AssertThat(view.ToViewPosition(new Vector2(100, 50))).IsEqual(new Vector2(100, 100));
    }

    #endregion

    #region NeedsRedraw

    [TestCase]
    public void NeedsRedrawBeforeFirstDraw()
    {
        var view = CreateView(new Vector2(1152, 648));

        AssertThat(view.NeedsRedraw()).IsTrue();
    }

    [TestCase]
    public void NeedsRedrawUnchanged()
    {
        var view = CreateView(new Vector2(1152, 648));
        view.DrawnMousePosition = view.Viewport.GetMousePosition();
        view.DrawnMouseMode = Input.MouseMode;

        AssertThat(view.NeedsRedraw()).IsFalse();
    }

    [TestCase]
    public void NeedsRedrawOnMouseMove()
    {
        var view = CreateView(new Vector2(1152, 648));
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(view.Viewport);
        view.DrawnMousePosition = view.Viewport.GetMousePosition();
        view.DrawnMouseMode = Input.MouseMode;

        using var mouseMotion = new InputEventMouseMotion { Position = view.DrawnMousePosition + new Vector2(10, 10) };
        view.Viewport.PushInput(mouseMotion);

        AssertThat(view.NeedsRedraw()).IsTrue();
    }

    [TestCase]
    public void NeedsRedrawOnMouseModeChange()
    {
        var view = CreateView(new Vector2(1152, 648));
        view.DrawnMousePosition = view.Viewport.GetMousePosition();
        view.DrawnMouseMode = Input.MouseMode != Input.MouseModeEnum.Captured ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;

        AssertThat(view.NeedsRedraw()).IsTrue();
    }

    #endregion

    #region IsCursorVisible

    [TestCase]
    public void IsCursorVisible()
    {
        AssertThat(SceneRunnerView.IsCursorVisible(Input.MouseModeEnum.Visible)).IsTrue();
        AssertThat(SceneRunnerView.IsCursorVisible(Input.MouseModeEnum.Confined)).IsTrue();
    }

    [TestCase]
    public void IsCursorVisibleHiddenOrCaptured()
    {
        AssertThat(SceneRunnerView.IsCursorVisible(Input.MouseModeEnum.Hidden)).IsFalse();
        AssertThat(SceneRunnerView.IsCursorVisible(Input.MouseModeEnum.ConfinedHidden)).IsFalse();
        AssertThat(SceneRunnerView.IsCursorVisible(Input.MouseModeEnum.Captured)).IsFalse();
    }

    #endregion

    #region CursorImage

    [TestCase]
    public void CursorImageDefault()
    {
        var view = CreateView(new Vector2(1152, 648));

        AssertThat(view.CursorImage).IsNull();
        AssertThat(view.CursorHotspot).IsEqual(Vector2.Zero);
    }

    [TestCase]
    public void CursorImageFromProjectSettings()
    {
        ProjectSettings.SetSetting("display/mouse_cursor/custom_image", CursorImage);
        ProjectSettings.SetSetting("display/mouse_cursor/custom_image_hotspot", new Vector2(4, 6));

        var view = CreateView(new Vector2(1152, 648));

        AssertThat(view.CursorImage).IsEqual(ResourceLoader.Load<Texture2D>(CursorImage));
        AssertThat(view.CursorHotspot).IsEqual(new Vector2(4, 6));
    }

    #endregion

    private static SceneRunnerView CreateView(Vector2 viewSize)
    {
        var viewport = AutoFree(new SubViewport { Size = new Vector2I(1152, 648) });
        var view = AutoFree(new SceneRunnerView(viewport));
        view.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        view.Size = viewSize;
        return view;
    }
}

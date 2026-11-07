// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core;

using Godot;

/// <summary>
///     Displays the scene runner viewport in the window and draws the simulated mouse cursor on top of it.
///     The cursor is drawn on the view only, so it never appears in the scene viewport texture.
/// </summary>
internal sealed partial class SceneRunnerView : TextureRect
{
    private static readonly Vector2[] ArrowPolygon =
    [
        new(0, 0),
        new(0, 17),
        new(4, 13),
        new(7, 20),
        new(10, 19),
        new(7, 12),
        new(12, 12)
    ];

    public SceneRunnerView(SubViewport viewport)
    {
        Viewport = viewport;
        Name = "GdUnitSceneRunnerView";
        Texture = viewport.GetTexture();
        MouseFilter = MouseFilterEnum.Ignore;
        ExpandMode = ExpandModeEnum.IgnoreSize;
        StretchMode = StretchModeEnum.KeepAspectCentered;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var customImage = ProjectSettings.GetSetting("display/mouse_cursor/custom_image", string.Empty).AsString();
        if (!string.IsNullOrEmpty(customImage) && ResourceLoader.Exists(customImage))
        {
            CursorImage = ResourceLoader.Load<Texture2D>(customImage);
            CursorHotspot = ProjectSettings.GetSetting("display/mouse_cursor/custom_image_hotspot", Vector2.Zero).AsVector2();
        }
    }

    internal SubViewport Viewport { get; }

    internal Texture2D? CursorImage { get; }

    internal Vector2 CursorHotspot { get; }

    internal Vector2 DrawnMousePosition { get; set; } = Vector2.Inf;

    internal Input.MouseModeEnum? DrawnMouseMode { get; set; }

    public override void _Process(double delta)
    {
        if (NeedsRedraw())
            QueueRedraw();
    }

    public override void _Draw()
    {
        DrawnMousePosition = Viewport.GetMousePosition();
        DrawnMouseMode = Input.MouseMode;
        if (!IsCursorVisible(Input.MouseMode))
            return;
        var cursorPosition = ToViewPosition(DrawnMousePosition);
        if (CursorImage != null)
        {
            DrawTexture(CursorImage, cursorPosition - CursorHotspot);
            return;
        }

        var arrow = ArrowPolygon.Select(point => point + cursorPosition).ToArray();
        DrawColoredPolygon(arrow, Colors.White);
        DrawPolyline([.. arrow, arrow[0]], Colors.Black);
    }

    /// <summary>
    ///     The cursor is hidden when the scene hides or captures the mouse, e.g. to draw its own cursor node.
    /// </summary>
    /// <param name="mouseMode">The current mouse mode.</param>
    /// <returns>True when the cursor is drawn.</returns>
    internal static bool IsCursorVisible(Input.MouseModeEnum mouseMode)
        => mouseMode is not (Input.MouseModeEnum.Hidden or Input.MouseModeEnum.ConfinedHidden or Input.MouseModeEnum.Captured);

    /// <summary>
    ///     The drawn cursor stays on screen until the mouse position or mouse mode changes, a resize redraws the view by itself.
    /// </summary>
    /// <returns>True when the cursor must be drawn again.</returns>
    internal bool NeedsRedraw()
        => Viewport.GetMousePosition() != DrawnMousePosition || Input.MouseMode != DrawnMouseMode;

    /// <summary>
    ///     Maps a position in the scene viewport to the view, where the viewport texture is scaled and centered.
    /// </summary>
    /// <param name="viewportPosition">The position in the scene viewport.</param>
    /// <returns>The position in the view.</returns>
    internal Vector2 ToViewPosition(Vector2 viewportPosition)
    {
        var viewportSize = (Vector2)Viewport.Size;
        var scaleFactor = Math.Min(Size.X / viewportSize.X, Size.Y / viewportSize.Y);
        var offset = (Size - (viewportSize * scaleFactor)) / 2.0f;
        return offset + (viewportPosition * scaleFactor);
    }
}

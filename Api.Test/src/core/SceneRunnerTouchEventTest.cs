namespace GdUnit4.Tests.Core;

using System;
using System.Linq;
using System.Threading.Tasks;

using Godot;

using static Assertions;

[RequireGodotRuntime]
[TestSuite]
public sealed class SceneRunnerTouchEventTest
{
    private const string DragAndDropScene = "res://src/core/resources/scenes/DragAndDrop/DragAndDropTestScene.tscn";

#nullable disable
    private ISceneRunner<TouchRecorderNode> runner;
#nullable enable

    [BeforeTest]
    public async Task BeforeTest()
    {
        runner = ISceneRunner.Load(new TouchRecorderNode(), true);
        await runner.AwaitInputProcessed();
    }

    [AfterTest]
    public void AfterTest()
        => runner.Dispose();

    [TestCase]
    public async Task SimulateScreenTouchPress()
    {
        runner.SimulateScreenTouchPress(0, new Vector2(683, 339));
        await runner.AwaitInputProcessed();

        AssertThat(runner.GetScreenTouchDragPosition(0)).IsEqual(new Vector2(683, 339));
        AssertTouchEvents(("InputEventScreenTouch", 0, new Vector2(683, 339), true, false));
    }

    [TestCase]
    public async Task SimulateScreenTouchPressDoubleTap()
    {
        runner.SimulateScreenTouchPress(0, new Vector2(683, 339), true);
        await runner.AwaitInputProcessed();

        AssertThat(runner.GetScreenTouchDragPosition(0)).IsEqual(new Vector2(683, 339));
        AssertTouchEvents(("InputEventScreenTouch", 0, new Vector2(683, 339), true, true));
    }

    [TestCase]
    public async Task SimulateScreenTouchPressed()
    {
        runner.SimulateScreenTouchPressed(0, new Vector2(683, 339));
        await runner.AwaitInputProcessed();

        AssertThat(runner.GetScreenTouchDragPosition(0)).IsEqual(new Vector2(683, 339));
        AssertTouchEvents(
            ("InputEventScreenTouch", 0, new Vector2(683, 339), true, false),
            ("InputEventScreenTouch", 0, new Vector2(683, 339), false, false));
    }

    [TestCase]
    public async Task SimulateScreenTouchPressedDoubleTap()
    {
        runner.SimulateScreenTouchPressed(0, new Vector2(683, 339), true);
        await runner.AwaitInputProcessed();

        AssertThat(runner.GetScreenTouchDragPosition(0)).IsEqual(new Vector2(683, 339));
        AssertTouchEvents(
            ("InputEventScreenTouch", 0, new Vector2(683, 339), true, true),
            ("InputEventScreenTouch", 0, new Vector2(683, 339), false, false));
    }

    [TestCase]
    public async Task SimulateScreenTouchRelease()
    {
        runner
            .SimulateScreenTouchPress(0, new Vector2(683, 339))
            .SimulateScreenTouchRelease(0);
        await runner.AwaitInputProcessed();

        AssertThat(runner.GetScreenTouchDragPosition(0)).IsEqual(new Vector2(683, 339));
        AssertTouchEvents(
            ("InputEventScreenTouch", 0, new Vector2(683, 339), true, false),
            ("InputEventScreenTouch", 0, new Vector2(683, 339), false, false));
    }

    [TestCase]
    public async Task SimulateScreenTouchReleaseDoubleTap()
    {
        runner
            .SimulateScreenTouchPress(0, new Vector2(683, 339), true)
            .SimulateScreenTouchRelease(0, true);
        await runner.AwaitInputProcessed();

        AssertThat(runner.GetScreenTouchDragPosition(0)).IsEqual(new Vector2(683, 339));
        AssertTouchEvents(
            ("InputEventScreenTouch", 0, new Vector2(683, 339), true, true),
            ("InputEventScreenTouch", 0, new Vector2(683, 339), false, true));
    }

    [TestCase]
    public void SimulateScreenTouchGetDragPosition()
    {
        runner
            .SimulateScreenTouchPress(0, new Vector2(300, 100))
            .SimulateScreenTouchPress(1, new Vector2(300, 200));

        AssertThat(runner.GetScreenTouchDragPosition(0)).IsEqual(new Vector2(300, 100));
        AssertThat(runner.GetScreenTouchDragPosition(1)).IsEqual(new Vector2(300, 200));
    }

    [TestCase]
    public void GetScreenTouchDragPositionWithoutTouch()
        => AssertThrown(() => runner.GetScreenTouchDragPosition(3))
            .IsInstanceOf<InvalidOperationException>()
            .HasMessage("No touch drag position for index '3' is set.");

    [TestCase]
    public void SimulateScreenTouchDrag()
    {
        runner.SimulateScreenTouchDrag(0, new Vector2(300, 100));
        AssertThat(runner.GetScreenTouchDragPosition(0)).IsEqual(new Vector2(300, 100));

        runner.SimulateScreenTouchDrag(0, new Vector2(400, 100));
        AssertThat(runner.GetScreenTouchDragPosition(0)).IsEqual(new Vector2(400, 100));

        var drags = runner.Scene().TouchEvents.OfType<InputEventScreenDrag>().ToList();
        AssertThat(drags.Select(e => (e.Index, e.Position, e.Relative, e.Pressure))).ContainsExactly(
            (0, new Vector2(300, 100), Vector2.Zero, 1.0f),
            (0, new Vector2(400, 100), new Vector2(300, 100) - new Vector2(400, 100), 1.0f));
    }

    [TestCase]
    public void SimulateScreenTouchGesturePress()
    {
        runner
            .SimulateScreenTouchPress(0, new Vector2(300, 100))
            .SimulateScreenTouchPress(1, new Vector2(300, 200));

        AssertTouchEvents(
            ("InputEventScreenTouch", 0, new Vector2(300, 100), true, false),
            ("InputEventScreenTouch", 1, new Vector2(300, 200), true, false));
    }

    [TestCase]
    public void SimulateScreenTouchGestureRelease()
    {
        runner
            .SimulateScreenTouchPress(0, new Vector2(300, 100))
            .SimulateScreenTouchPress(1, new Vector2(300, 200))
            .SimulateScreenTouchRelease(0)
            .SimulateScreenTouchRelease(1);

        AssertTouchEvents(
            ("InputEventScreenTouch", 0, new Vector2(300, 100), true, false),
            ("InputEventScreenTouch", 1, new Vector2(300, 200), true, false),
            ("InputEventScreenTouch", 0, new Vector2(300, 100), false, false),
            ("InputEventScreenTouch", 1, new Vector2(300, 200), false, false));
    }

    [TestCase]
    public void SimulateScreenTouchGestureZoomOut()
    {
        var fingerOne = new Vector2(300, 200);
        var fingerTwo = new Vector2(300, 250);
        runner
            .SimulateScreenTouchPress(0, fingerOne)
            .SimulateScreenTouchPress(1, fingerTwo);

        for (var y = 0; y <= 100; y += 5)
        {
            runner
                .SimulateScreenTouchDrag(0, fingerOne - new Vector2(0, y))
                .SimulateScreenTouchDrag(1, fingerTwo + new Vector2(0, y));
        }

        AssertThat(runner.GetScreenTouchDragPosition(0)).IsEqual(new Vector2(300, 100));
        AssertThat(runner.GetScreenTouchDragPosition(1)).IsEqual(new Vector2(300, 350));
    }

    [TestCase]
    public async Task TouchDragAndDropRelative()
    {
        using var dragRunner = ISceneRunner.Load(DragAndDropScene, true);
        var (slotLeft, slotRight) = Slots(dragRunner);
        var dragStart = slotLeft.GlobalPosition + new Vector2(50, 50);

        dragRunner.SimulateScreenTouchPress(0, dragStart);
        await dragRunner.AwaitIdleFrame();
        AssertThat(Input.IsMouseButtonPressed(MouseButton.Left)).IsTrue();
        AssertThat(dragRunner.GetMousePosition()).IsEqual(dragStart);

        await dragRunner.SimulateScreenTouchDragRelative(0, new Vector2(140, 0));

        AssertThat(slotRight.Texture).IsEqual(slotLeft.Texture);
        AssertThat(dragRunner.GetMousePosition()).IsEqual(dragStart + new Vector2(140, 0));
        AssertThat(Input.IsMouseButtonPressed(MouseButton.Left)).IsFalse();
    }

    [TestCase]
    public async Task TouchDragAndDropAbsolute()
    {
        using var dragRunner = ISceneRunner.Load(DragAndDropScene, true);
        var (slotLeft, slotRight) = Slots(dragRunner);
        var dragStart = slotLeft.GlobalPosition + new Vector2(50, 50);
        var dragEnd = dragStart + new Vector2(140, 0);

        dragRunner.SimulateScreenTouchPress(0, dragStart);
        await dragRunner.AwaitIdleFrame();
        AssertThat(Input.IsMouseButtonPressed(MouseButton.Left)).IsTrue();

        await dragRunner.SimulateScreenTouchDragAbsolute(0, dragEnd);

        AssertThat(slotRight.Texture).IsEqual(slotLeft.Texture);
        AssertThat(dragRunner.GetMousePosition()).IsEqual(dragEnd);
        AssertThat(dragRunner.GetScreenTouchDragPosition(0)).IsEqual(dragEnd);
        AssertThat(Input.IsMouseButtonPressed(MouseButton.Left)).IsFalse();
    }

    [TestCase]
    public async Task TouchDragAndDrop()
    {
        using var dragRunner = ISceneRunner.Load(DragAndDropScene, true);
        var (slotLeft, slotRight) = Slots(dragRunner);
        var dragStart = slotLeft.GlobalPosition + new Vector2(50, 50);
        var dragEnd = dragStart + new Vector2(140, 0);

        await dragRunner.SimulateScreenTouchDragDrop(0, dragStart, dragEnd);

        AssertThat(slotRight.Texture).IsEqual(slotLeft.Texture);
        AssertThat(dragRunner.GetMousePosition()).IsEqual(dragEnd);
        AssertThat(Input.IsMouseButtonPressed(MouseButton.Left)).IsFalse();
    }

    private static (TextureRect Left, TextureRect Right) Slots(ISceneRunner dragRunner)
        => (dragRunner.Scene().GetNode<TextureRect>("left/TextureRect"), dragRunner.Scene().GetNode<TextureRect>("right/TextureRect"));

    private void AssertTouchEvents(params (string Type, int Index, Vector2 Position, bool Pressed, bool DoubleTap)[] expected)
        => AssertThat(runner.Scene().TouchEvents
                .OfType<InputEventScreenTouch>()
                .Select(e => (e.GetClass(), e.Index, e.Position, e.Pressed, e.DoubleTap)))
            .ContainsExactly(expected.Select(e => ((string)e.Type, e.Index, e.Position, e.Pressed, e.DoubleTap)));
}

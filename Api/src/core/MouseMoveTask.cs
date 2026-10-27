// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text
namespace GdUnit4.Core;

using System.Diagnostics.CodeAnalysis;

using Godot;

/// <summary>
///     A helper to simulate a mouse moving from a source to the final position.
/// </summary>
internal partial class MouseMoveTask : Node, IDisposable
{
    public MouseMoveTask(Vector2 currentPosition, Vector2 finalPosition)
    {
        CurrentMousePosition = currentPosition;
        FinalMousePosition = finalPosition;
    }

    private Vector2 CurrentMousePosition { get; set; }

    private Vector2 FinalMousePosition { get; }

    public new void Dispose()
    {
        QueueFree();
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    [SuppressMessage(
        "Style",
        "IDE0058:Expression value is never used",
        Justification = "Method called for side effects only, return value intentionally ignored")]
    public async Task WaitOnFinalPosition(ISceneRunner sceneRunner, double time, Tween.TransitionType transitionType)
    {
        using var tween = sceneRunner.Scene().CreateTween();
        tween.TweenProperty(this, "CurrentMousePosition", FinalMousePosition, time).SetTrans(transitionType);
        tween.Play();

        while (!sceneRunner.GetMousePosition().IsEqualApprox(FinalMousePosition))
        {
            sceneRunner.SimulateMouseMove(CurrentMousePosition);
            await ISceneRunner.SyncProcessFrame;
        }

        sceneRunner.SimulateMouseMove(FinalMousePosition);
        await ISceneRunner.SyncProcessFrame;
    }
}

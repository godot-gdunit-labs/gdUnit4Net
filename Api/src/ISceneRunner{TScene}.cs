// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4;

using Godot;

/// <summary>
///     Scene runner with typed access to the loaded scene.
/// </summary>
/// <typeparam name="TScene">The type of the scene root node.</typeparam>
/// <example>
///     <code>
///     var runner = ISceneRunner.Load(new Player(), true);
///     var player = runner.Scene();
///
///     var loadedRunner = ISceneRunner.Load&lt;Player&gt;("res://player.tscn", true);
///     </code>
/// </example>
public interface ISceneRunner<out TScene> : ISceneRunner
    where TScene : Node
{
    /// <summary>
    ///     Access to the current running scene, typed as the scene root node.
    /// </summary>
    /// <returns>The current scene instance.</returns>
    /// <exception cref="ObjectDisposedException">The SceneRunner has been disposed.</exception>
    /// <seealso cref="ISceneRunner.Scene" />
    new TScene Scene();

    /// <inheritdoc cref="ISceneRunner.SimulateActionPressed" />
    new ISceneRunner<TScene> SimulateActionPressed(string action);

    /// <inheritdoc cref="ISceneRunner.SimulateActionPress" />
    new ISceneRunner<TScene> SimulateActionPress(string action);

    /// <inheritdoc cref="ISceneRunner.SimulateActionRelease" />
    new ISceneRunner<TScene> SimulateActionRelease(string action);

    /// <inheritdoc cref="ISceneRunner.SimulateKeyPressed" />
    new ISceneRunner<TScene> SimulateKeyPressed(Key keyCode, bool shift = false, bool control = false);

    /// <inheritdoc cref="ISceneRunner.SimulateKeyPress" />
    new ISceneRunner<TScene> SimulateKeyPress(Key keyCode, bool shift = false, bool control = false);

    /// <inheritdoc cref="ISceneRunner.SimulateKeyRelease" />
    new ISceneRunner<TScene> SimulateKeyRelease(Key keyCode, bool shift = false, bool control = false);

    /// <inheritdoc cref="ISceneRunner.SimulateMouseMove" />
    new ISceneRunner<TScene> SimulateMouseMove(Vector2 position);

    /// <inheritdoc cref="ISceneRunner.SimulateMouseButtonPressed" />
    new ISceneRunner<TScene> SimulateMouseButtonPressed(MouseButton button, bool doubleClick = false);

    /// <inheritdoc cref="ISceneRunner.SimulateMouseButtonPress" />
    new ISceneRunner<TScene> SimulateMouseButtonPress(MouseButton button, bool doubleClick = false);

    /// <inheritdoc cref="ISceneRunner.SimulateMouseButtonRelease" />
    new ISceneRunner<TScene> SimulateMouseButtonRelease(MouseButton button);

    /// <inheritdoc cref="ISceneRunner.SetMousePos" />
    new ISceneRunner<TScene> SetMousePos(Vector2 position);

    /// <inheritdoc cref="ISceneRunner.SetTimeFactor" />
    new ISceneRunner<TScene> SetTimeFactor(double timeFactor = 1.0);
}

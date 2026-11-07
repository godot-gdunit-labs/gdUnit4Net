// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core;

using System.Runtime.Loader;

using Godot;

internal sealed class SceneRunner<TScene> : SceneRunner, ISceneRunner<TScene>
    where TScene : Node
{
    public SceneRunner(string resourcePath, bool autoFree = false, bool verbose = false)
        : base(LoadTypedScene(resourcePath), autoFree, verbose)
    {
    }

    public SceneRunner(TScene currentScene, bool autoFree = false, bool verbose = false)
        : base(currentScene, autoFree, verbose)
    {
    }

    public new TScene Scene()
        => (TScene)base.Scene();

    public new ISceneRunner<TScene> SimulateActionPressed(string action, int eventIndex = -1)
    {
        _ = base.SimulateActionPressed(action, eventIndex);
        return this;
    }

    public new ISceneRunner<TScene> SimulateActionPress(string action, int eventIndex = -1)
    {
        _ = base.SimulateActionPress(action, eventIndex);
        return this;
    }

    public new ISceneRunner<TScene> SimulateActionRelease(string action, int eventIndex = -1)
    {
        _ = base.SimulateActionRelease(action, eventIndex);
        return this;
    }

    public new ISceneRunner<TScene> SimulateKeyPressed(Key keyCode)
    {
        _ = base.SimulateKeyPressed(keyCode);
        return this;
    }

    public new ISceneRunner<TScene> SimulateKeyPress(Key keyCode)
    {
        _ = base.SimulateKeyPress(keyCode);
        return this;
    }

    public new ISceneRunner<TScene> SimulateKeyRelease(Key keyCode)
    {
        _ = base.SimulateKeyRelease(keyCode);
        return this;
    }

    public new ISceneRunner<TScene> SimulateMouseMove(Vector2 position)
    {
        _ = base.SimulateMouseMove(position);
        return this;
    }

    public new ISceneRunner<TScene> SimulateMouseButtonPressed(MouseButton button, bool doubleClick = false)
    {
        _ = base.SimulateMouseButtonPressed(button, doubleClick);
        return this;
    }

    public new ISceneRunner<TScene> SimulateMouseButtonPress(MouseButton button, bool doubleClick = false)
    {
        _ = base.SimulateMouseButtonPress(button, doubleClick);
        return this;
    }

    public new ISceneRunner<TScene> SimulateMouseButtonRelease(MouseButton button)
    {
        _ = base.SimulateMouseButtonRelease(button);
        return this;
    }

    public new ISceneRunner<TScene> SetMousePosition(Vector2 position)
    {
        _ = base.SetMousePosition(position);
        return this;
    }

    public new ISceneRunner<TScene> SetTimeFactor(double timeFactor = 1.0)
    {
        _ = base.SetTimeFactor(timeFactor);
        return this;
    }

    public new ISceneRunner<TScene> SimulateScreenTouchPressed(int index, Vector2 position, bool doubleTap = false)
    {
        _ = base.SimulateScreenTouchPressed(index, position, doubleTap);
        return this;
    }

    public new ISceneRunner<TScene> SimulateScreenTouchPress(int index, Vector2 position, bool doubleTap = false)
    {
        _ = base.SimulateScreenTouchPress(index, position, doubleTap);
        return this;
    }

    public new ISceneRunner<TScene> SimulateScreenTouchRelease(int index, bool doubleTap = false)
    {
        _ = base.SimulateScreenTouchRelease(index, doubleTap);
        return this;
    }

    public new ISceneRunner<TScene> SimulateScreenTouchDrag(int index, Vector2 position)
    {
        _ = base.SimulateScreenTouchDrag(index, position);
        return this;
    }

    public new ISceneRunner<TScene> MoveWindowToForeground()
    {
        _ = base.MoveWindowToForeground();
        return this;
    }

    public new ISceneRunner<TScene> MoveWindowToBackground()
    {
        _ = base.MoveWindowToBackground();
        return this;
    }

    private static TScene LoadTypedScene(string resourcePath)
    {
        var scene = LoadScene(resourcePath);

        // a null scene is rejected by the base constructor with the common message
        if (scene is null or TScene)
            return (TScene)scene!;

        var actualType = scene.GetType();
        var expectedType = typeof(TScene);
        scene.Free();
        throw new ArgumentException(
            $"GdUnitSceneRunner: The scene '{resourcePath}' has the root type '{TypeName(actualType, expectedType)}', expected '{TypeName(expectedType, actualType)}'.");
    }

    private static string TypeName(Type type, Type other)
    {
        if (type.Name != other.Name)
            return type.Name;
        if (type.FullName != other.FullName)
            return type.FullName ?? type.Name;

        // the same type loaded twice, e.g. by two assembly load contexts
        return $"{type.AssemblyQualifiedName} (load context: {AssemblyLoadContext.GetLoadContext(type.Assembly)?.Name})";
    }
}

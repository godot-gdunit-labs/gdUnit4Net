// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Runners;

using System.Diagnostics.CodeAnalysis;

using Godot;

using Logging;

using Projects;

/// <summary>
///     Starts the GdUnit4Net test runner in the runner scene installed into the test project.
/// </summary>
public static class GdUnit4TestRunnerHost
{
    /// <summary>
    ///     Mounts the referenced Godot projects and starts the test runner on the given scene tree.
    /// </summary>
    /// <param name="sceneTree">The scene tree of the installed runner scene.</param>
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "TestRunner disposal is managed by Godot internals.")]
    public static void Initialize(SceneTree sceneTree)
    {
        ArgumentNullException.ThrowIfNull(sceneTree);
        try
        {
            GodotProjectMounter.MountReferencedProjects(sceneTree.Root, new GodotLogger());
            sceneTree.Root.AddChild(new GdUnit4TestRunnerNode());
        }

        // Godot callback boundary: an exception escaping the scene tree initialization does not end the Godot process, so it exits with an error code instead
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            GD.PrintErr("Exception", e.Message);
            sceneTree.Quit(100); // Exit with error code
        }
    }
}

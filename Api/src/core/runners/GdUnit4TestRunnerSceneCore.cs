// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Runners;

using System.Diagnostics.CodeAnalysis;

using Api;

using Godot;

using Logging;

using Projects;

/// <summary>
///     The GdUnit4Net test runner scene.
/// </summary>
public partial class GdUnit4TestRunnerSceneCore : SceneTree
{
    /// <inheritdoc />
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "TestRunner disposal is managed by Godot internals.")]
    public override void _Initialize()
    {
        try
        {
            GodotProjectMounter.MountReferencedProjects(Root, new GodotLogger());
            Root.AddChild(new TestRunner());
        }

        // Godot callback boundary: an exception escaping the scene tree initialization does not end the Godot process, so it exits with an error code instead
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            GD.PrintErr("Exception", e.Message);
            Quit(100); // Exit with error code
        }
    }

    // ReSharper disable once PartialTypeWithSinglePart
    private sealed partial class TestRunner : Node
    {
        public TestRunner()
        {
            Logger = new GodotLogger();
            Server = new GodotGdUnit4RestServer(Logger, ResolvePipeName());
        }

        private ITestEngineLogger Logger { get; }

        private GodotGdUnit4RestServer Server { get; }

        public override void _Ready() => _ = Server.Start();

        public override void _Process(double delta) => _ = Server.Process();

        public override void _Notification(int what)
        {
            if (what == NotificationPredelete)
                Server.Stop();
        }

        private static string ResolvePipeName()
        {
            var args = OS.GetCmdlineArgs();
            var index = Array.IndexOf(args, "--pipe-name");
            if (index < 0 || index >= args.Length - 1)
                throw new InvalidOperationException("Missing required '--pipe-name' argument.");
            return args[index + 1];
        }
    }
}

// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Runners;

using Api;

using Godot;

using Logging;

// ReSharper disable once PartialTypeWithSinglePart
internal sealed partial class GdUnit4TestRunnerNode : Node
{
    public GdUnit4TestRunnerNode()
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

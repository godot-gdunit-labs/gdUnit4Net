// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Signals;

using System.Collections.Concurrent;

using Extensions;

using Godot;

using static Console;

using Array = Godot.Collections.Array;
using Error = Godot.Error;

internal sealed partial class GodotSignalCollector : RefCounted
{
    internal static readonly ConcurrentDictionary<int, CancellationTokenSource> TaskCancellations = new();

    public static GodotSignalCollector Instance { get; } = new();

    internal ConcurrentDictionary<GodotObject, ConcurrentDictionary<string, ConcurrentBag<Variant[]>>> CollectedSignals { get; } = new();

    public void RegisterEmitter(GodotObject emitter)
    {
        // do not register the same emitter at twice
        if (!CollectedSignals.TryAdd(emitter, new ConcurrentDictionary<string, ConcurrentBag<Variant[]>>()))
            return; // Already registered

        // connect to 'TreeExiting' of the emitter to finally release all acquired resources/connections.
        var action = UnregisterEmitter;
        if (emitter is Node node && !node.IsConnected(Node.SignalName.TreeExiting, Callable.From(action)))
            node.TreeExiting += () => UnregisterEmitter(this, emitter);

        ConnectAllSignals(emitter);
    }

    internal static (bool NeedsCallProcessing, bool NeedsCallPhysicsProcessing) DoesNodeProcessing(GodotObject emitter)
    {
        // We don't need to call manually the frame/physic processing on non node emitters
        if (emitter is not Node node)
            return (false, false);

        // If the emitter is attached to scene tre we don't need to manually process frame/physics
        if (node.IsInsideTree())
            return (false, false);

        // In NOT we need to check if the emitter have implemented the `_Process` or `_PhysicsProcess`. to call manually process frame/physics
        return (
            node.HasMethod("_Process"),
            node.HasMethod("_PhysicsProcess"));
    }

    internal int Count(GodotObject emitter, string signalName, Variant[] args)
        => IsSignalCollecting(emitter, signalName)
            ? CollectedSignals[emitter][signalName].Count(signalArgs => signalArgs.VariantEquals(args))
            : 0;

    internal bool IsSignalCollecting(GodotObject emitter, string signalName)
        => CollectedSignals.ContainsKey(emitter) && CollectedSignals[emitter].ContainsKey(signalName);

    internal Task<bool> IsEmitted(CancellationTokenSource cancellationTokenSource, GodotObject emitter, string signal, Variant[] args)
        => Task.Run(
            () =>
            {
                try
                {
                    var (needsCallProcessing, needsCallPhysicsProcessing) = DoesNodeProcessing(emitter);
                    var delta = 10.0d;

                    while (IsInstanceValid(emitter) && !Match(emitter, signal, args))
                    {
                        var ticks = Time.GetTicksUsec() / 1000.0;

                        if (needsCallProcessing && IsInstanceValid(emitter))
                            _ = emitter.Call("_Process", delta);

                        if (needsCallPhysicsProcessing && IsInstanceValid(emitter))
                            _ = emitter.Call("_PhysicsProcess", delta);

                        delta = (Time.GetTicksUsec() / 1000.0) - ticks;

                        // ReSharper disable once AccessToDisposedClosure
                        if (cancellationTokenSource.IsCancellationRequested)
                            return false;
                    }

                    return true;
                }

                // User test code boundary: the loop calls `_Process` and `_PhysicsProcess` of the user's emitter node, its exception ends the wait as not emitted
#pragma warning disable CA1031
                catch (Exception e)
#pragma warning restore CA1031
                {
                    WriteLine(e.Message);
                    WriteLine(e.StackTrace);
                    return false;
                }
                finally
                {
                    ResetCollectedSignals(emitter);
                }
            },
            cancellationTokenSource.Token);

    internal void Clean()
    {
        CollectedSignals.Keys
            .ToList()
            .ForEach(emitter => UnregisterEmitter(this, emitter));
        CollectedSignals.Clear();
    }

    private void ConnectAllSignals(GodotObject emitter)
    {
        var emitterSignals = CollectedSignals[emitter];

        foreach (var signalDef in emitter.GetSignalList())
        {
            var signalName = (string)signalDef["name"];
            var args = (Array)signalDef["args"];
            var error = emitter.Connect(signalName, BuildCallable(emitter, signalName, args.Count));
            if (error != Error.Ok)
                WriteLine($"Error on connecting signal {signalName}, Error: {error}");

            _ = emitterSignals.TryAdd(signalName, []);
        }
    }

    private Callable BuildCallable(GodotObject emitter, string signalName, int argumentCount)
        => argumentCount switch
        {
            0 => Callable.From(() => CollectSignal(emitter, signalName)),
            1 => Callable.From<Variant>(arg0 => CollectSignal(emitter, signalName, arg0)),
            2 => Callable.From<Variant, Variant>((arg0, arg1) => CollectSignal(emitter, signalName, arg0, arg1)),
            3 => Callable.From<Variant, Variant, Variant>((arg0, arg1, arg2) => CollectSignal(emitter, signalName, arg0, arg1, arg2)),
            4 => Callable.From<Variant, Variant, Variant, Variant>((arg0, arg1, arg2, arg3) => CollectSignal(emitter, signalName, arg0, arg1, arg2, arg3)),
            5 => Callable.From<Variant, Variant, Variant, Variant, Variant>((arg0, arg1, arg2, arg3, arg4) => CollectSignal(emitter, signalName, arg0, arg1, arg2, arg3, arg4)),
            6 => Callable.From<Variant, Variant, Variant, Variant, Variant, Variant>((arg0, arg1, arg2, arg3, arg4, arg5)
                => CollectSignal(emitter, signalName, arg0, arg1, arg2, arg3, arg4, arg5)),
            7 => Callable.From<Variant, Variant, Variant, Variant, Variant, Variant, Variant>((arg0, arg1, arg2, arg3, arg4, arg5, arg6)
                => CollectSignal(emitter, signalName, arg0, arg1, arg2, arg3, arg4, arg5, arg6)),
            8 => Callable.From<Variant, Variant, Variant, Variant, Variant, Variant, Variant, Variant>((arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7)
                => CollectSignal(emitter, signalName, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7)),
            _ => throw new NotImplementedException()
        };

    private void CollectSignal(GodotObject emitter, string signalName, params Variant[] signalArgs)
    {
        if (IsSignalCollecting(emitter, signalName))
            CollectedSignals[emitter][signalName].Add(signalArgs);
    }

    // unregister all acquired resources/connections, otherwise it ends up in orphans
    // is called when the emitter is removed from the parent
    private void UnregisterEmitter(GodotSignalCollector collector, GodotObject emitter)
    {
        if (IsInstanceValid(collector))
        {
            foreach (var connection in collector.GetIncomingConnections())
            {
                var source = (GodotObject)connection["source"];
                var signalName = (string)connection["signal_name"];
                var methodName = (string)connection["method_name"];
                source.Disconnect(signalName, new Callable(collector, methodName));
            }
        }

        if (IsInstanceValid(emitter))
            _ = CollectedSignals.TryRemove(emitter, out _);
    }

    private void ResetCollectedSignals(GodotObject emitter)
    {
        if (!CollectedSignals.TryGetValue(emitter, out var value))
            return;
        foreach (var signalName in value.Keys)
            value[signalName].Clear();
    }

    private bool Match(GodotObject emitter, string signalName, params Variant[] args)
    {
        if (!IsInstanceValid(emitter))
        {
            WriteLine($"Test match signal '{signalName}', the emitter is disposed.");
            return false;
        }

        if (!CollectedSignals.TryGetValue(emitter, out var emitterSignals))
            return false;

        return emitterSignals.TryGetValue(signalName, out var signalBag)
               && signalBag.Any(receivedArgs => receivedArgs.VariantEquals(args));
    }
}

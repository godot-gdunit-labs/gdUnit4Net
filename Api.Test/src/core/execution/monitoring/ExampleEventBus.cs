namespace GdUnit4.Tests.Core.Execution.Monitoring;

using Godot;

public partial class ExampleEventBus : Node
{
    [Signal]
    public delegate void OnMyEventEventHandler();

    public void Emit() => EmitSignal(SignalName.OnMyEvent);

    public void Connect(Callable callback) => Connect(SignalName.OnMyEvent, callback);
}

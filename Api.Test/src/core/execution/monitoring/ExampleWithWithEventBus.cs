namespace GdUnit4.Tests.Core.Execution.Monitoring;

using System;

using Godot;

public partial class ExampleWithWithEventBus : Node
{
    public void Register(ExampleEventBus bus)
        => bus.Connect(new Callable(this, nameof(MyCallback)));

#pragma warning disable CA2201
    private void MyCallback() => throw new NullReferenceException("Nope");
#pragma warning restore CA2201
}

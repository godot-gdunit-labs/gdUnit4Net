namespace GdUnit4.Asserts;

// GdUnit generated TestSuite
using System;

internal sealed partial class ExampleNode : Godot.Node, IEquatable<ExampleNode>
{
    private int Value { get; set; }

    private string Msg { get; set; }

    public ExampleNode(string msg, int value)
    {
        Msg = msg;
        Value = value;
    }

    public override bool Equals(object? obj)
        => obj is ExampleNode example
            && Value == example.Value
            && Msg == example.Msg;

    public bool Equals(ExampleNode? obj)
        => obj is ExampleNode example
            && Value == example.Value
            && Msg == example.Msg;

    public override int GetHashCode() => HashCode.Combine(Value, Msg);
}

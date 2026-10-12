namespace GdUnit4.Tests.Core.Resources.Scenes;

using System.Collections.Generic;
using Godot;

public partial class TestSceneWithInitialization : Node2D
{
    public List<string> MethodCalls { get; } = new();

    public void Initialize() => MethodCalls.Add("Initialize");

    public override void _Ready() => MethodCalls.Add("_Ready");
}

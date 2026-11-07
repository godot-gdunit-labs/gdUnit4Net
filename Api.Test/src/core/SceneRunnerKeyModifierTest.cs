namespace GdUnit4.Tests.Core;

using System.Collections.Generic;
using System.Threading.Tasks;

using Godot;

using static Assertions;

[RequireGodotRuntime]
[TestSuite]
public sealed class SceneRunnerKeyModifierTest
{
    [TestCase]
    public async Task ModifierKeyAppliesToNextKey()
    {
        using var runner = ISceneRunner.Load(new KeyModifierRecorderNode(), true);

        runner
            .SimulateKeyPress(Key.Shift)
            .SimulateKeyPress(Key.Ctrl)
            .SimulateKeyPressed(Key.A);
        await runner.AwaitInputProcessed();

        AssertThat(runner.Scene().KeyEvents).ContainsExactly(
            "Shift pressed=True shift=True ctrl=False",
            "Ctrl pressed=True shift=True ctrl=True",
            "A pressed=True shift=True ctrl=True",
            "A pressed=False shift=True ctrl=True");
    }

    [TestCase]
    public async Task KeyWithoutModifier()
    {
        using var runner = ISceneRunner.Load(new KeyModifierRecorderNode(), true);

        runner.SimulateKeyPressed(Key.A);
        await runner.AwaitInputProcessed();

        AssertThat(runner.Scene().KeyEvents).ContainsExactly(
            "A pressed=True shift=False ctrl=False",
            "A pressed=False shift=False ctrl=False");
    }
}

public partial class KeyModifierRecorderNode : Node
{
    public List<string> KeyEvents { get; } = [];

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey { Echo: false } key)
            KeyEvents.Add($"{key.Keycode} pressed={key.Pressed} shift={key.ShiftPressed} ctrl={key.CtrlPressed}");
    }
}

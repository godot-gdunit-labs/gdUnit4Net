namespace GdUnit4.Tests;

using System;
using System.Threading.Tasks;

using Godot;

public partial class TestScene : Control
{
    [Signal]
    public delegate void PanelColorChangeEventHandler(ColorRect box, Color color);

    // private static readonly Color[] ColorCycle = { Colors.RoyalBlue, Colors.Chartreuse, Colors.YellowGreen };
    private Color initial_color = Colors.Red;

    private RefCounted? nullable;
    private bool player_jump_action;

    public override void _Ready()
    {
        Box1 = GetNode<ColorRect>("VBoxContainer/PanelContainer/HBoxContainer/Panel1");
        Box2 = GetNode<ColorRect>("VBoxContainer/PanelContainer/HBoxContainer/Panel2");
        Box3 = GetNode<ColorRect>("VBoxContainer/PanelContainer/HBoxContainer/Panel3");
        Connect(SignalName.PanelColorChange, Callable.From<ColorRect, Color>(OnPanelColorChanged));
        OnlyOneTimeCall();
    }

    private void OnlyOneTimeCall()
    {
    }

    public async void OnTestPressed(long buttonId)
    {
        // Console.WriteLine($"pressed {buttonId}");
        ColorRect box;
        switch (buttonId)
        {
            case 1:
                box = Box1;
                break;
            case 2:
                box = Box2;
                break;
            case 3:
                box = Box3;
                break;
            default:
                return;
        }

        EmitSignal(SignalName.PanelColorChange, box, Colors.Red);
        if (buttonId == 3)
        {
            var timer = GetTree().CreateTimer(1);
            await ToSignal(timer, Timer.SignalName.Timeout);
        }

        EmitSignal(SignalName.PanelColorChange, box, Colors.Gray);
    }

    private void OnGrayTimeout(ColorRect box)
        => EmitSignal(SignalName.PanelColorChange, box, Colors.Gray);

    private Timer CreateTimer(float timeout)
    {
        var timer = new Timer();
        AddChild(timer);
        timer.Connect(Timer.SignalName.Timeout, Callable.From(() => OnTimeout(timer)));
        timer.OneShot = true;
        timer.WaitTime = timeout;
        timer.Start();
        return timer;
    }

    private void OnTimeout(Timer timer)
    {
        RemoveChild(timer);
        timer.QueueFree();
    }

    public async Task<string> ColorCycle()
    {
        GD.Print("color_cycle initial");
        await ToSignal(CreateTimer(0.5f), Timer.SignalName.Timeout);
        EmitSignal(SignalName.PanelColorChange, Box1, Colors.Red);
        GD.Print("changed to RED");
        await ToSignal(CreateTimer(0.5f), Timer.SignalName.Timeout);
        EmitSignal(SignalName.PanelColorChange, Box1, Colors.Blue);
        GD.Print("changed to BLUE");
        await ToSignal(CreateTimer(0.5f), Timer.SignalName.Timeout);
        EmitSignal(SignalName.PanelColorChange, Box1, Colors.Green);
        GD.Print("changed to GREEN");
        return "black";
    }

    public async void StartColorCycle()
        => await ColorCycle();

    public Spell CreateSpell()
    {
        var spell = new Spell();
        spell.Connect(Spell.SignalName.SpellExplode, Callable.From<ulong>(DestroySpell));
        return spell;
    }

    private void DestroySpell(ulong spellId)
        => Console.WriteLine($"Spell destroyed: {spellId}");

    // spell.QueueFree();
    public override void _Input(InputEvent @event)
    {
        if (InputMap.HasAction("player_jump"))
            player_jump_action = Input.IsActionJustReleased("player_jump", true);
        if (@event.IsActionReleased("ui_accept"))
            AddChild(CreateSpell());
    }

    public int Add(int a, int b)
        => a + b;

    private void OnPanelColorChanged(ColorRect box, Color color)
        => box.Color = color;
#pragma warning disable CS8618

    public ColorRect Box1 { get; set; }

    private ColorRect Box2 { get; set; }

    private ColorRect Box3 { get; set; }

#pragma warning restore CS8618
}

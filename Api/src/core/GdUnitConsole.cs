// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core;

using System.Drawing;

internal sealed class GdUnitConsole
{
    private const int BOLD = 0x1;
    private const int ITALIC = 0x2;
    private const int UNDERLINE = 0x4;
    private const string CSI_BOLD = "\u001b[1m";
    private const string CSI_ITALIC = "\u001b[3m";
    private const string CSI_UNDERLINE = "\u001b[4m";
    private static readonly object LockObj = new();
    private readonly Dictionary<string, (int Left, int Top)> savedCursorsByName = [];

    public GdUnitConsole PrintError(string message)
    {
        lock (LockObj)
        {
            return BeginColor(ConsoleColor.DarkRed)
                .WriteLine(message)
                .EndColor()
                .NewLine();
        }
    }

    public GdUnitConsole Print(string message, ConsoleColor color = ConsoleColor.White, int flags = 0)
    {
        lock (LockObj)
        {
            return BeginColor(color)
                .Bold((flags & BOLD) == BOLD)
                .Italic((flags & ITALIC) == ITALIC)
                .Underline((flags & UNDERLINE) == UNDERLINE)
                .Write(message)
                .EndColor();
        }
    }

    public GdUnitConsole Println(string message, ConsoleColor color = ConsoleColor.White, int flags = 0)
    {
        lock (LockObj)
        {
            return BeginColor(color)
                .Bold((flags & BOLD) == BOLD)
                .Italic((flags & ITALIC) == ITALIC)
                .Underline((flags & UNDERLINE) == UNDERLINE)
                .WriteLine(message)
                .EndColor();
        }
    }

    internal void SaveCursor(string name) =>
        savedCursorsByName[name] = Console.GetCursorPosition();

    internal void RestoreCursor(string name)
    {
        if (savedCursorsByName.TryGetValue(name, out var position))
        {
            Console.SetCursorPosition(position.Left, position.Top);
            _ = savedCursorsByName.Remove(name);
        }
    }

    private GdUnitConsole NewLine()
    {
        Console.WriteLine();
        return this;
    }

    private GdUnitConsole BeginColor(ConsoleColor color)
    {
        var c = ToColor(color);
        Console.Write("\u001b[38;2;{0};{1};{2}m", c.R, c.G, c.B);
        return this;
    }

    private GdUnitConsole EndColor()
    {
        Console.Write("\u001b[0m");
        return this;
    }

    private GdUnitConsole Underline(bool enable)
    {
        if (enable)
            Console.Write(CSI_UNDERLINE);
        return this;
    }

    private GdUnitConsole Bold(bool enable)
    {
        if (enable)
            Console.Write(CSI_BOLD);
        return this;
    }

    private GdUnitConsole Italic(bool enable)
    {
        if (enable)
            Console.Write(CSI_ITALIC);
        return this;
    }

    private Color ToColor(ConsoleColor color)
    {
        var colorName = Enum.GetName(color);
        return Color.FromName(colorName!);
    }

    private GdUnitConsole Write(string message)
    {
        Console.Write(message);
        return this;
    }

    private GdUnitConsole WriteLine(string message)
    {
        Console.WriteLine(message);
        return this;
    }
}

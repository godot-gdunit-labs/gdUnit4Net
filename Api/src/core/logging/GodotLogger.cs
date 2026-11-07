// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Logging;

using Api;

using Godot;

internal sealed class GodotLogger : ITestEngineLogger
{
    /// <inheritdoc />
    void ITestEngineLogger.SendMessage(LogLevel logLevel, string message)
    {
        switch (logLevel)
        {
            case LogLevel.Debug:
                GD.PrintS("[DEBUG]:", message);
                break;
            case LogLevel.Informational:
                GD.PrintS(message);
                break;
            case LogLevel.Warning:
                GD.PrintS(message);
                break;
            case LogLevel.Error:
                GD.PrintErr(message);
                break;
            default:
                GD.PrintS(message);
                break;
        }
    }
}

// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Hooks;

using System;

internal static class StdOutHookFactory
{
    public static IStdOutHook CreateStdOutHook()
    {
        if (OperatingSystem.IsWindows())
            return new WindowsStdOutHook();
        if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
            return new UnixStdOutHook();
        throw new PlatformNotSupportedException("Unsupported operating system");
    }
}

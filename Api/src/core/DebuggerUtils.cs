// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core;

using System.Reflection;

using Godot.NativeInterop;

internal static class DebuggerUtils
{
    private static readonly MethodInfo? DebuggerIsActiveMethod = IsDebuggerUtils();

    public static bool IsDebuggerActive()
    {
        if (DebuggerIsActiveMethod == null)
            return false;

        var isDebuggerActive = (godot_bool)DebuggerIsActiveMethod.Invoke(null, null)!;
        return isDebuggerActive.ToBool();
    }

    private static MethodInfo? IsDebuggerUtils()
    {
        var nativeFuncType = typeof(NativeFuncs);
        return nativeFuncType.GetMethod(
            "godotsharp_internal_script_debugger_is_active",
            BindingFlags.NonPublic | BindingFlags.Static);
    }
}

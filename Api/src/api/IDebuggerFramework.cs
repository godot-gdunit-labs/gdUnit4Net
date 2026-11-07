// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Api;

using System.Diagnostics;

/// <summary>
/// Describes the functionality of the interface for managing debugger operations.
/// </summary>
public interface IDebuggerFramework
{
    /// <summary>
    /// Gets a value indicating whether indicates whether a debugger process is currently executing.
    /// </summary>
    bool IsDebugProcess { get; }

    /// <summary>
    /// Gets a value indicating whether indicates whether a debugger is attached to a running process.
    /// </summary>
    bool IsDebugAttach { get; }

    /// <summary>
    /// Launches a new process with a debugger attached, based on the provided process start information.
    /// </summary>
    /// <param name="processStartInfo">The start information of the process.</param>
    /// <returns>Returns the process started with the attached debugger.</returns>
    Process LaunchProcessWithDebuggerAttached(ProcessStartInfo processStartInfo);

    /// <summary>
    /// Attaches a debugger to an already-running process.
    /// </summary>
    /// <param name="process">The process to which the debugger should be attached.</param>
    /// <returns>Returns <c>true</c> if the debugger was successfully attached; otherwise, <c>false</c>.</returns>
    bool AttachDebuggerToProcess(Process process);
}

// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.TestAdapter;

using System;
using System.Diagnostics;

using Api;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

internal class RiderDebuggerFramework : IDebuggerFramework
{
    public RiderDebuggerFramework(IFrameworkHandle frameworkHandle)
        => FrameworkHandle = frameworkHandle;

    public IFrameworkHandle FrameworkHandle { get; }

    public bool IsDebugProcess { get; } = Debugger.IsAttached;

    public bool IsDebugAttach => false;

    public Process LaunchProcessWithDebuggerAttached(ProcessStartInfo processStartInfo)
    {
        var processId = FrameworkHandle
            .LaunchProcessWithDebuggerAttached(
                processStartInfo.FileName,
                processStartInfo.WorkingDirectory,
                processStartInfo.Arguments,
                processStartInfo.Environment);
        return Process.GetProcessById(processId);
    }

    public bool AttachDebuggerToProcess(Process process)
        => throw new NotImplementedException();
}

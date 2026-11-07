// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.TestAdapter;

using System;
using System.Diagnostics;

using Api;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

internal class DefaultDebuggerFramework : IDebuggerFramework
{
    private readonly IFrameworkHandle frameworkHandle;

    public DefaultDebuggerFramework(IFrameworkHandle frameworkHandle)
        => this.frameworkHandle = frameworkHandle;

    public bool IsDebugProcess => false;

    public bool IsDebugAttach => Debugger.IsAttached;

    public Process LaunchProcessWithDebuggerAttached(ProcessStartInfo processStartInfo)
        => throw new NotImplementedException();

    public bool AttachDebuggerToProcess(Process process)
    {
        if (frameworkHandle is IFrameworkHandle2 fh2)
            return fh2.AttachDebuggerToProcess(process.Id);

        return false;
    }
}

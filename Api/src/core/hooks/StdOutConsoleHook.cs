// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Hooks;

using System;
using System.IO;
using System.Text;

internal sealed class StdOutConsoleHook : IStdOutHook
{
    private readonly CaptureWriter captureWriter = new();
    private readonly TextWriter originalOutput = Console.Out;

    public void StartCapture()
    {
        captureWriter.Clear();
        Console.SetOut(captureWriter);
    }

    public void StopCapture() => Console.SetOut(originalOutput);

    public string GetCapturedOutput() => captureWriter.GetCapturedOutput();

    public void Dispose()
    {
        captureWriter.Dispose();
        originalOutput.Dispose();
    }

    private class CaptureWriter : TextWriter
    {
        private readonly StringBuilder capturedOutput = new();

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value) => capturedOutput.Append(value);

        public string GetCapturedOutput() => capturedOutput.ToString();

        public void Clear() => capturedOutput.Clear();
    }
}

// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Runners;

/// <summary>
///     Keeps the last lines of a process output stream, so they can be reported when the process exits abnormally.
///     Lines are added from the process output thread and read after the exit, so all access is synchronized.
/// </summary>
/// <param name="capacity">The maximum number of lines kept.</param>
internal sealed class ProcessOutputTail(int capacity)
{
    private readonly Queue<string> lines = new();

    /// <summary>
    ///     Adds a line, removing the oldest one when the capacity is reached. Blank lines are ignored.
    /// </summary>
    /// <param name="line">The output line.</param>
    public void Add(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        lock (lines)
        {
            if (lines.Count == capacity)
                _ = lines.Dequeue();
            lines.Enqueue(line.TrimEnd());
        }
    }

    /// <summary>
    ///     Removes all lines.
    /// </summary>
    public void Clear()
    {
        lock (lines)
            lines.Clear();
    }

    /// <summary>
    ///     Returns a copy of the kept lines, oldest first.
    /// </summary>
    /// <returns>The kept lines.</returns>
    public string[] ToArray()
    {
        lock (lines)
            return [.. lines];
    }
}

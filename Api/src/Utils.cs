// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4;

using System.Diagnostics;

using Godot;

/// <summary>
///     Provides utility methods for common GdUnit4Net operations.
/// </summary>
/// <remarks>
///     This static class contains helper methods for various testing scenarios,
///     including asynchronous timing operations, temporary file management, and
///     Godot error handling. These utilities simplify test setup and execution
///     when working with Godot-based tests.
/// </remarks>
public static class Utils
{
    /// <summary>
    ///     Asynchronously waits for the specified timeout period and returns the actual elapsed time.
    /// </summary>
    /// <param name="timeout">The timeout duration in milliseconds.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the actual elapsed time in milliseconds.
    /// </returns>
    public static async Task<long> DoWait(long timeout)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        using (var tokenSource = new CancellationTokenSource())
        {
            await Task
                .Delay(TimeSpan.FromMilliseconds(timeout), tokenSource.Token)
                .ConfigureAwait(true);
        }

        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }

    /// <summary>
    ///     Creates a temporary folder under Godot managed user directory.
    /// </summary>
    /// <param name="path">a relative path.</param>
    /// <returns>the full path to the created temp directory.</returns>
    public static string CreateTempDir(string path)
    {
        var tempFolder = Path.Combine(GodotTempDir(), path);
        if (!new FileInfo(tempFolder).Exists)
            _ = Directory.CreateDirectory(tempFolder);
        return tempFolder;
    }

    /// <summary>
    ///     Deletes the GdUnit temp directory recursively.
    /// </summary>
    public static void ClearTempDir()
    {
        var tempFolder = GodotTempDir();
        if (Directory.Exists(tempFolder))
            Directory.Delete(tempFolder, true);
    }

    /// <summary>
    ///     Maps Godot error number to a readable error message. See at ERROR
    ///     https://docs.godotengine.org/de/stable/classes/class_@globalscope.html#enum-globalscope-error.
    /// </summary>
    /// <param name="error">The Godot error.</param>
    /// <returns>The error as a string presentation if available.</returns>
    public static string ErrorAsString(Error error) => error.ToString();

    /// <summary>
    ///     Maps Godot error number to a readable error message. See at ERROR
    ///     https://docs.godotengine.org/de/stable/classes/class_@globalscope.html#enum-globalscope-error.
    /// </summary>
    /// <param name="error">The Godot error.</param>
    /// <returns>The error as a string presentation if available.</returns>
    public static string ErrorAsString(int error) => !Enum.IsDefined(typeof(Error), Convert.ToInt64(error))
        ? $"The error: {error} is not defined in Godot."
        : ErrorAsString((Error)Enum.ToObject(typeof(Error), error));

    internal static string GodotTempDir() => Path.Combine(Path.GetTempPath(), "gdUnit4Net");
}

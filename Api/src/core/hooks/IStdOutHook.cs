// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Hooks;

using System;

using Godot;

/// <summary>
///     Defines a contract for capturing and managing standard output (stdout) during test execution.
/// </summary>
/// <remarks>
///     <para>
///         This interface enables test frameworks to intercept and capture console output written during
///         test execution. This is particularly useful for:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Preventing test output from cluttering the test runner console</description>
///         </item>
///         <item>
///             <description>Capturing output for assertions or validation purposes</description>
///         </item>
///         <item>
///             <description>Isolating output between different test cases</description>
///         </item>
///         <item>
///             <description>Providing clean test execution reports</description>
///         </item>
///     </list>
///     <para>
///         Implementations should be thread-safe when used in parallel test execution scenarios
///         and must properly restore the original console output when disposed.
///     </para>
/// </remarks>
/// <seealso cref="Console" />
/// <seealso cref="TextWriter" />
/// <seealso cref="GD.PrintS" />
internal interface IStdOutHook : IDisposable
{
    /// <summary>
    ///     Begins capturing standard output written to the console.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         After calling this method, all related console output methods will be
    ///         intercepted and stored internally instead of being written to the actual console.
    ///     </para>
    ///     <para>
    ///         Multiple calls to <see cref="StartCapture" /> without intervening <see cref="StopCapture" />
    ///         calls should be handled gracefully, typically by continuing the existing capture session.
    ///     </para>
    ///     <para>
    ///         This method should be thread-safe and work correctly in parallel test execution scenarios.
    ///     </para>
    /// </remarks>
    void StartCapture();

    /// <summary>
    ///     Stops capturing standard output and restores normal console output behavior.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         After calling this method, console output will resume being written to the actual
    ///         console as normal. The captured output remains available via <see cref="GetCapturedOutput" />
    ///         until the next <see cref="StartCapture" /> call or until the hook is disposed.
    ///     </para>
    ///     <para>
    ///         Calling <see cref="StopCapture" /> when capture is not active should be handled
    ///         gracefully without throwing exceptions.
    ///     </para>
    ///     <para>
    ///         This method should be thread-safe and properly restore console output in all scenarios.
    ///     </para>
    /// </remarks>
    void StopCapture();

    /// <summary>
    ///     Retrieves all output that has been captured since the last <see cref="StartCapture" /> call.
    /// </summary>
    /// <returns>
    ///     A string containing all console output that was captured. Returns an empty string
    ///     if no output was captured or if capture has not been started.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method can be called multiple times and will return the same captured content
    ///         until a new capture session is started with <see cref="StartCapture" />.
    ///     </para>
    ///     <para>
    ///         The returned string includes all formatting characters such as newlines (\n)
    ///         and carriage returns (\r) exactly as they were written to the console.
    ///     </para>
    ///     <para>
    ///         This method should be thread-safe and return consistent results even when
    ///         called concurrently during or after capture operations.
    ///     </para>
    /// </remarks>
    string GetCapturedOutput();
}

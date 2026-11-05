// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Api;

using System.Collections.Concurrent;

using Core.Logging;

/// <summary>
///     A thread-safe test utility that captures log messages emitted by specific classes,
///     allowing tests to assert on log output without noise from unrelated components.
/// </summary>
/// <remarks>
///     Watch one or more classes, then assert on the collected messages:
///     <code>
///     using var capture = LogCapture.Watch&lt;ClassA&gt;();
///
///     // exercise code under test
///
///     AssertThat(capture.Count(LogLevel.Informational)).IsEqual(1);
///     </code>
///     Multiple classes can be watched in a single scope:
///     <code>
///     using var capture = LogCapture.Watch&lt;ClassA, ClassB&gt;();
///     </code>
///     <see cref="Dispose" /> automatically stops watching so parallel tests do not interfere.
/// </remarks>
/// <remarks>
///     <b>Limitation:</b> only classes that obtain their logger via
///     <c>LoggerFactory.GetLogger&lt;T&gt;()</c> are captured. Classes that receive an
///     <see cref="ITestEngineLogger" /> through constructor injection bypass this mechanism.
/// </remarks>
public sealed class LogCapture : IDisposable
{
    // Type → set of active captures (thread-safe, O(1) removal via TryRemove)
    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<LogCapture, byte>> Registry = new();

    private readonly ConcurrentQueue<LogEntry> captured = new();
    private readonly Type[] registeredTypes;

    private LogCapture(Type[] types)
    {
        registeredTypes = types;
        foreach (var type in types)
            _ = Registry.GetOrAdd(type, _ => new ConcurrentDictionary<LogCapture, byte>()).TryAdd(this, 0);
    }

    /// <summary>Gets all log entries captured so far, in the order they were received.</summary>
    public IReadOnlyList<LogEntry> Entries => [.. captured];

    /// <summary>
    ///     Watches log messages emitted by <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The class whose log output should be captured.</typeparam>
    /// <returns>A <see cref="LogCapture" /> scope that must be disposed at the end of the test.</returns>
    public static LogCapture Watch<T>() => Watch(typeof(T));

    /// <summary>
    ///     Watches log messages emitted by <typeparamref name="T1" /> and <typeparamref name="T2" />.
    /// </summary>
    /// <typeparam name="T1">The first class whose log output should be captured.</typeparam>
    /// <typeparam name="T2">The second class whose log output should be captured.</typeparam>
    /// <returns>A <see cref="LogCapture" /> scope that must be disposed at the end of the test.</returns>
    public static LogCapture Watch<T1, T2>() => Watch(typeof(T1), typeof(T2));

    /// <summary>
    ///     Watches log messages emitted by <typeparamref name="T1" />, <typeparamref name="T2" />, and
    ///     <typeparamref name="T3" />.
    /// </summary>
    /// <typeparam name="T1">The first class whose log output should be captured.</typeparam>
    /// <typeparam name="T2">The second class whose log output should be captured.</typeparam>
    /// <typeparam name="T3">The third class whose log output should be captured.</typeparam>
    /// <returns>A <see cref="LogCapture" /> scope that must be disposed at the end of the test.</returns>
    public static LogCapture Watch<T1, T2, T3>() => Watch(typeof(T1), typeof(T2), typeof(T3));

    /// <summary>
    ///     Watches log messages emitted by all specified <paramref name="types" />.
    /// </summary>
    /// <param name="types">The classes whose log output should be captured.</param>
    /// <returns>A <see cref="LogCapture" /> scope that must be disposed at the end of the test.</returns>
    public static LogCapture Watch(params Type[] types) => new(types ?? throw new ArgumentNullException(nameof(types)));

    /// <summary>Stops watching and removes this capture from the registry so no further messages are routed to it.</summary>
    public void Dispose()
    {
        foreach (Type type in registeredTypes)
        {
            if (Registry.TryGetValue(type, out var captures))
                _ = captures.TryRemove(this, out _);
        }
    }

    /// <summary>Discards all entries collected so far, resetting the capture to an empty state.</summary>
    public void Clear() => captured.Clear();

    /// <summary>
    ///     Returns the number of captured messages at the given <paramref name="logLevel" />.
    /// </summary>
    /// <param name="logLevel">The severity level to count.</param>
    /// <returns>The number of captured messages matching <paramref name="logLevel" />.</returns>
    public int Count(LogLevel logLevel) => captured.Count(e => e.Level == logLevel);

    /// <summary>
    ///     Returns all captured entries at the given <paramref name="logLevel" />, in the order they were received.
    /// </summary>
    /// <param name="logLevel">The severity level to filter by.</param>
    /// <returns>All captured <see cref="LogEntry" /> instances matching <paramref name="logLevel" />.</returns>
    public IReadOnlyList<LogEntry> EntriesOf(LogLevel logLevel)
        => [.. captured.Where(e => e.Level == logLevel)];

    /// <summary>Returns all active captures watching <paramref name="type" />.</summary>
    /// <param name="type">The type to look up.</param>
    /// <returns>All active <see cref="LogCapture" /> instances watching <paramref name="type" />.</returns>
    internal static ICollection<LogCapture> GetCaptures(Type type)
        => Registry.TryGetValue(type, out var captures) ? captures.Keys : [];

    /// <summary>Records a single log entry. Called by <see cref="TypedLogger" />.</summary>
    /// <param name="level">The severity level.</param>
    /// <param name="message">The message text.</param>
    /// <param name="source">The class that emitted the message.</param>
    internal void Capture(LogLevel level, string message, Type source)
        => captured.Enqueue(new LogEntry(level, message, source));
}

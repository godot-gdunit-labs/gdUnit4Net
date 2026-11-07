// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Logging;

using System.Runtime.CompilerServices;

using Api;

/// <summary>
///     Manages the logging pipeline for a single engine instance: holds the root logger and
///     task-local scope registry, and provides class-scoped logger instances for internal use.
/// </summary>
/// <remarks>
///     Create and install the singleton via the builder at engine startup:
///     <code>
///     var factory = LoggerFactory.WithRootLogger(myLogger).Build();
///     </code>
///     Obtain a class-scoped logger (typically as a static field):
///     <code>
///     private static readonly ITestEngineLogger Logger = LoggerFactory.GetLogger&lt;MyClass&gt;();
///     </code>
/// </remarks>
internal sealed class LoggerFactory : IDisposable
{
    private readonly AsyncLocal<ScopeLogger?> currentScope = new();

    private LoggerFactory(ITestEngineLogger root) => RootLogger = root;

    /// <summary>Gets the current singleton instance. Before <see cref="Builder.Build" /> is called, routes to a no-op logger.</summary>
    internal static LoggerFactory Instance { get; private set; } = new(NoOpTestEngineLogger.Instance);

    /// <summary>Gets the engine-wide root logger. Never null; defaults to a no-op sentinel.</summary>
    internal ITestEngineLogger RootLogger
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        private set;
    }

    /// <summary>Gets the active logger for the current task: the scope logger if one is active, otherwise <see cref="RootLogger" />.</summary>
    internal ITestEngineLogger Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => currentScope.Value ?? RootLogger;
    }

    /// <summary>Resets the root logger to the no-op sentinel, releasing the reference to the engine logger.</summary>
    public void Dispose()
        => RootLogger = NoOpTestEngineLogger.Instance;

    /// <summary>Returns a class-scoped logger for <typeparamref name="T" /> that delegates to <see cref="Instance" />.<see cref="Current" /> at dispatch time.</summary>
    internal static ITestEngineLogger GetLogger<T>() => new TypedLogger(typeof(T));

    /// <summary>Returns a class-scoped logger for <paramref name="type" /> that delegates to <see cref="Instance" />.<see cref="Current" /> at dispatch time.</summary>
    internal static ITestEngineLogger GetLogger(Type type) => new TypedLogger(type);

    /// <summary>Returns a <see cref="Builder" /> pre-configured with <paramref name="logger" /> as the root logger.</summary>
    internal static Builder WithRootLogger(ITestEngineLogger logger) => new Builder().WithLogger(logger);

    /// <summary>
    ///     Creates a task-local log scope for the current async execution context.
    ///     The scope logger wraps the immutable <see cref="RootLogger" /> and prefixes every message with
    ///     <c>[<paramref name="scopeId" />]</c>, leaving other parallel tasks unaffected.
    ///     Dispose the returned <see cref="LogScope" /> to end the scope automatically.
    /// </summary>
    internal LogScope CreateScope(string scopeId)
    {
        currentScope.Value = new ScopeLogger(RootLogger, scopeId ?? throw new ArgumentNullException(nameof(scopeId)));
        return new LogScope(this);
    }

    /// <summary>Gets the active scope logger for the current task, or <see langword="null" /> if none is active.</summary>
    internal ScopeLogger? GetScope() => currentScope.Value;

    private void EndScope() => currentScope.Value = null;

    /// <summary>Ends the task-local log scope created by <see cref="CreateScope" /> when disposed.</summary>
    internal readonly struct LogScope(LoggerFactory factory) : IDisposable
    {
        public void Dispose() => factory.EndScope();
    }

    /// <summary>Fluent builder that configures and installs a new <see cref="LoggerFactory" /> singleton.</summary>
    internal sealed class Builder
    {
        private ITestEngineLogger logger = NoOpTestEngineLogger.Instance;

        /// <summary>Sets the root logger used by the factory.</summary>
        internal Builder WithLogger(ITestEngineLogger rootLogger)
        {
            ArgumentNullException.ThrowIfNull(rootLogger);
            logger = rootLogger;
            return this;
        }

        /// <summary>Builds a new <see cref="LoggerFactory" />, atomically installs it as <see cref="Instance" />, and returns it.</summary>
        internal LoggerFactory Build()
        {
            var factory = new LoggerFactory(logger);
            Instance = factory;
            return factory;
        }
    }
}

// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

// ReSharper disable once CheckNamespace
namespace GdUnit4;

/// <summary>
///     Marker interface for GdUnit4 awaitable operations that support timeout functionality.
/// </summary>
/// <remarks>
///     <para>
///         This interface serves as a constraint for the <see cref="GodotAwaiterExtension.WithTimeout{TVariant}" />
///         extension method, ensuring that only appropriate GdUnit4 awaitable types can be used with timeout operations.
///     </para>
///     <para>
///         Types implementing this interface indicate they represent asynchronous operations within the GdUnit4
///         testing framework that can be canceled or timed out gracefully.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     // ISignalAssert implements IGdUnitAwaitable, so this works:
///     await AssertSignal(node).IsEmitted("ready").WithTimeout(5000);
///
///     </code>
/// </example>
/// <seealso cref="GodotAwaiterExtension.WithTimeout{TVariant}" />
/// <seealso cref="Asserts.ISignalAssert" />
#pragma warning disable CA1040
public interface IGdUnitAwaitable
#pragma warning restore CA1040
{
}

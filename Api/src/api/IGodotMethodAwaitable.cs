// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text
namespace GdUnit4.Api;

using System.Threading.Tasks;

using Godot;

/// <summary>
///     Represents an awaitable Godot method call that supports fluent assertion syntax.
/// </summary>
/// <typeparam name="TVariant">The expected return type of the Godot method call. Must be a valid Godot Variant type.</typeparam>
/// <remarks>
///     <para>
///         This interface enables fluent testing of asynchronous Godot method calls by combining
///         method execution with assertion validation in a chainable syntax.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     // Basic method assertion
///     await AwaitMethod(player, "GetHealth")
///         .IsNotNull()
///         .IsEqual(100);
///
///     // With timeout support
///     await AwaitMethod(gameManager, "GetScore")
///         .IsEqual(1500)
///         .WithTimeout(2000);
///
///     // Null checking for optional return values
///     await AwaitMethod(inventory, "FindItem", "sword")
///         .IsNotNull();
///
///     // Testing method that should return null
///     await AwaitMethod(player, "GetWeapon")
///         .IsNull();
///     </code>
/// </example>
/// <seealso cref="IGdUnitAwaitable" />
/// <seealso cref="GodotAwaiterExtension.WithTimeout{TVariant}" />
public interface IGodotMethodAwaitable<[MustBeVariant] TVariant> : IGdUnitAwaitable
{
    /// <summary>
    ///     Asserts that the method result equals the expected value.
    /// </summary>
    /// <param name="expected">The expected value to compare against the method result.</param>
    /// <returns>A task that completes with the same <see cref="IGodotMethodAwaitable{TVariant}" /> instance for method chaining.</returns>
    /// <exception cref="Core.Execution.Exceptions.TestFailedException">Thrown if the method result does not equal the expected value.</exception>
    Task<IGodotMethodAwaitable<TVariant>> IsEqual(TVariant expected);

    /// <summary>
    ///     Asserts that the method result is null.
    /// </summary>
    /// <returns>A task that completes with the same <see cref="IGodotMethodAwaitable{TVariant}" /> instance for method chaining.</returns>
    /// <exception cref="Core.Execution.Exceptions.TestFailedException">Thrown if the method result is not null.</exception>
    Task<IGodotMethodAwaitable<TVariant>> IsNull();

    /// <summary>
    ///     Asserts that the method result is not null.
    /// </summary>
    /// <returns>A task that completes with the same <see cref="IGodotMethodAwaitable{TVariant}" /> instance for method chaining.</returns>
    /// <exception cref="Core.Execution.Exceptions.TestFailedException">Thrown if the method result is null.</exception>
    Task<IGodotMethodAwaitable<TVariant>> IsNotNull();
}

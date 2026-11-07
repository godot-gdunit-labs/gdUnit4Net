// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Execution;

using System.Threading.Tasks;

/// <summary>
///     Defines a contract for execution stages in the test framework pipeline.
/// </summary>
/// <remarks>
///     <para>
///         Execution stages represent discrete phases of test execution that can be composed
///         to form complex execution workflows. Each stage performs a specific responsibility
///         within the overall test execution pipeline.
///     </para>
///     <para>
///         Common execution stages include:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Test suite setup and initialization</description>
///         </item>
///         <item>
///             <description>Individual test method execution</description>
///         </item>
///         <item>
///             <description>Cleanup and resource disposal</description>
///         </item>
///     </list>
///     <para>
///         Implementations should be stateless where possible and handle cancellation
///         appropriately through the provided <see cref="ExecutionContext" />.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     // Example implementation of a test setup stage
///     public class TestSetupStage : IExecutionStage
///     {
///         public async Task Execute(ExecutionContext context)
///         {
///             context.CancellationToken.ThrowIfCancellationRequested();
///
///             // Perform test setup operations
///
///             // Move to next stage in pipeline
///         }
///     }
///     </code>
/// </example>
/// <seealso cref="ExecutionContext" />
internal interface IExecutionStage
{
    /// <summary>
    ///     Executes this stage of the test execution pipeline asynchronously.
    /// </summary>
    /// <param name="context">
    ///     The execution context containing test information, configuration, event listeners,
    ///     cancellation tokens, and other data needed for execution.
    /// </param>
    /// <returns>
    ///     A task representing the asynchronous execution of this stage.
    ///     The task completes when the stage has finished its operations.
    /// </returns>
    /// <exception cref="Exceptions.TestFailedException">
    ///     Thrown when test execution fails and cannot continue to subsequent stages.
    /// </exception>
    Task Execute(ExecutionContext context);
}

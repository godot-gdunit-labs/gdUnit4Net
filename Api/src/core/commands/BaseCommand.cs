// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Commands;

using System.Threading.Tasks;

using Api;

/// <summary>
///     Base class for all test execution commands in the GdUnit4 framework.
/// </summary>
/// <remarks>
///     All command implementations must be serializable to support interprocess communication.
///     Key requirements for derived commands:
///     <list type="bullet">
///         <item>Must provide a parameterless constructor for JSON deserialization, a non-public one marked with [JsonConstructor]</item>
///         <item>Non-public properties must be marked with the [JsonInclude] attribute</item>
///         <item>Properties containing complex types must also be serializable</item>
///         <item>Must be registered as a known message type of the pipe message serializer</item>
///     </list>
///     Commands are serialized when passing between test runner and Godot engine processes.
/// </remarks>
public abstract class BaseCommand
{
    /// <summary>
    ///     Executes the command and returns a response containing the execution result.
    /// </summary>
    /// <param name="testEventListener">Listener that receives test execution events.</param>
    /// <returns>Response indicating command execution status and result.</returns>
    public abstract Task<Response> Execute(ITestEventListener testEventListener);
}

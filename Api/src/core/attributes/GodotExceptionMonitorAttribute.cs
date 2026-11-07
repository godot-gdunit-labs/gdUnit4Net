// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

// ReSharper disable once CheckNamespace
// Need to be placed in the root namespace to be accessible by the test runner.
namespace GdUnit4;

using System;

/// <summary>
///     Indicates that a test method or class should monitor the exceptions and errors Godot catches and logs while the test runs.
///     This attribute enables monitoring of exceptions Godot catches in C# code it calls, such as node callbacks, signal callbacks
///     and deferred calls, and of errors pushed with <c>push_error</c>, which Godot reports through its logger.
/// </summary>
/// <remarks>
///     This attribute is particularly useful for:
///     <list type="bullet">
///         <li>Testing exception handling in Godot node callbacks (_Ready, _Process, etc.)</li>
///         <li>Verifying exceptions during scene tree operations</li>
///         <li>Monitoring exceptions that Godot would otherwise only log</li>
///     </list>
///     Example usage for a test method:
///     <code>
/// [TestCase]
/// [GodotExceptionMonitor]
/// public void TestNodeThrowsInReady()
/// {
///     // This will capture the exception thrown in the node's _Ready method
///     var node = new MyNode();
///     AddChild(node);
/// }
/// </code>
///     Example usage for a test class:
///     <code>
/// [TestSuite]
/// [GodotExceptionMonitor]
/// public class MyNodeTests
/// {
///     // All test methods in this class will monitor Godot exceptions
///     [TestCase]
///     public void TestSceneProcessing()
///     {
///         // Exceptions during scene processing will be monitored
///         var scene = SceneLoader.Load("res://my_scene.tscn");
///     }
/// }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false)]
#pragma warning disable CA1813
public class GodotExceptionMonitorAttribute : Attribute
#pragma warning restore CA1813
{
}

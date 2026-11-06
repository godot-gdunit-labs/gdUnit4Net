// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

// ReSharper disable once CheckNamespace
namespace GdUnit4.TestRunner;

using Core.Runners;

using Godot;

/// <summary>
///     The GdUnit4Net test runner scene.
/// </summary>
public partial class GdUnit4TestRunnerScene : SceneTree
{
    /// <inheritdoc />
    public override void _Initialize() => GdUnit4TestRunnerHost.Initialize(this);
}

// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Projects;

/// <summary>
///     The resource packs of the referenced Godot projects that a test run uses.
///     Disposing releases their in-use markers, a later run can then delete outdated packs.
/// </summary>
internal sealed class GodotProjectPacks : IDisposable
{
    private readonly List<string> files = [];
    private readonly List<string> inUseMarkers = [];

    internal IReadOnlyList<string> Files => files;

    public void Dispose()
    {
        foreach (var inUseMarker in inUseMarkers)
            GodotProjectPacker.TryDelete(inUseMarker);
        inUseMarkers.Clear();
    }

    internal void Add(string packFile, string inUseMarker)
    {
        files.Add(packFile);
        inUseMarkers.Add(inUseMarker);
    }
}

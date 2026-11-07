// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Execution.Monitoring;

using Extensions;

using Godot;

internal class MemoryPool
{
    private static readonly ThreadLocal<MemoryPool?> CurrentPool = new();
    private readonly List<GodotObject> registeredObjects = [];

    public MemoryPool(bool reportOrphanNodesEnabled) => OrphanMonitor = reportOrphanNodesEnabled ? new OrphanNodesMonitor() : null;

    public int OrphanCount => OrphanMonitor?.OrphanCount ?? 0;

    public string Name { get; set; } = "Unknown";

    private OrphanNodesMonitor? OrphanMonitor { get; }

    public static T RegisterForAutoFree<T>(T obj)
        where T : GodotObject
    {
        CurrentPool.Value?.registeredObjects.Add(obj);
        return obj;
    }

    public void SetActive(string name, bool reset = false)
    {
        Name = name;
        CurrentPool.Value = this;
        OrphanMonitor?.Start(reset);
    }

    public async Task Gc()
    {
        var currentPool = CurrentPool.Value;
        currentPool?.registeredObjects.ForEach(FreeInstance);
        currentPool?.registeredObjects.Clear();
        StopMonitoring();
        if (OrphanMonitor != null)
            _ = await GodotObjectExtensions.SyncProcessFrame;
    }

    public void StopMonitoring() => OrphanMonitor?.Stop();

    private void FreeInstance(GodotObject obj)
    {
        // needs to manually exclude JavaClass see https://github.com/godotengine/godot/issues/44932
        if (GodotObject.IsInstanceValid(obj) && obj is not JavaClass)
        {
            if (obj is RefCounted)
                obj.Notification((int)GodotObject.NotificationPredelete);
            else
                obj.Free();
        }
    }
}

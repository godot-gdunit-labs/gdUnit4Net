// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Execution;

using System.Threading.Tasks;

internal class BeforeExecutionStage : ExecutionStage<BeforeAttribute>
{
    public BeforeExecutionStage(TestSuite testSuite)
        : base("Before", testSuite.Instance.GetType())
    {
    }

    public override async Task Execute(ExecutionContext context)
    {
        context.MemoryPool.SetActive(StageName, true);
        await base
            .Execute(context)
            .ConfigureAwait(true);
        context.FireBeforeEvent();
        context.ReportCollector.Clear();
        context.MemoryPool.StopMonitoring();
    }
}

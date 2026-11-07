// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Execution;

using System.Threading.Tasks;

internal class BeforeTestExecutionStage : ExecutionStage<BeforeTestAttribute>
{
    public BeforeTestExecutionStage(TestSuite testSuite)
        : base("BeforeTest", testSuite.Instance.GetType())
    {
    }

    public override async Task Execute(ExecutionContext context)
    {
        context.FireBeforeTestEvent();
        if (!context.IsSkipped)
        {
            context.MemoryPool.SetActive(StageName, true);
            await base
                .Execute(context)
                .ConfigureAwait(true);
            context.MemoryPool.StopMonitoring();
        }
    }
}

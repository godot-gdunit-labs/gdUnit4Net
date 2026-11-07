// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Execution;

using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Asserts;

using Reporting;

using static Api.ReportType;

internal class AfterExecutionStage : ExecutionStage<AfterAttribute>
{
    public AfterExecutionStage(TestSuite testSuite)
        : base("After", testSuite.Instance.GetType())
    {
    }

    public override async Task Execute(ExecutionContext context)
    {
        context.MemoryPool.SetActive(StageName);
        await base
            .Execute(context)
            .ConfigureAwait(true);
        Utils.ClearTempDir();
        await context.MemoryPool
            .Gc()
            .ConfigureAwait(true);
        if (context.MemoryPool.OrphanCount > 0)
            context.ReportCollector.PushFront(new TestReport(Warning, 0, ReportOrphans(context)));
        context.FireAfterEvent();
    }

    private static AfterAttribute? AfterAttribute(ExecutionContext context) => context.TestSuite.Instance
        .GetType()
        .GetMethods()
        .FirstOrDefault(m => m.IsDefined(typeof(AfterAttribute)))
        ?.GetCustomAttribute<AfterAttribute>();

    private static BeforeAttribute? BeforeAttribute(ExecutionContext context) => context.TestSuite.Instance
        .GetType()
        .GetMethods()
        .FirstOrDefault(m => m.IsDefined(typeof(BeforeAttribute)))
        ?.GetCustomAttribute<BeforeAttribute>();

    private static string ReportOrphans(ExecutionContext context)
    {
        var beforeAttribute = BeforeAttribute(context);
        var afterAttributes = AfterAttribute(context);

        if (beforeAttribute != null && afterAttributes != null)
        {
            return $"""
                    {AssertFailures.FormatValue("WARNING:", AssertFailures.WARN_COLOR, false)}
                        Detected <{context.MemoryPool.OrphanCount}> orphan nodes during test suite setup stage!
                        Check [b]{beforeAttribute.Name + ":" + beforeAttribute.Line}[/b] and [b]{afterAttributes.Name + ":" + afterAttributes.Line}[/b] for unfreed instances!
                    """;
        }

        return $"""
                {AssertFailures.FormatValue("WARNING:", AssertFailures.WARN_COLOR, false)}
                    Detected <{context.MemoryPool.OrphanCount}> orphan nodes during test suite setup stage!
                    Check [b]{(beforeAttribute != null ? beforeAttribute.Name + ":" + beforeAttribute.Line : afterAttributes?.Name + ":" + afterAttributes?.Line)}[/b] for unfreed instances!
                """;
    }
}

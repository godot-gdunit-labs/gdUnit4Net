// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Execution;

using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Asserts;

using Reporting;

using Signals;

using static Api.ReportType;

internal class AfterTestExecutionStage : ExecutionStage<AfterTestAttribute>
{
    public AfterTestExecutionStage(TestSuite testSuite)
        : base("AfterTest", testSuite.Instance.GetType())
    {
    }

    public override async Task Execute(ExecutionContext context)
    {
        if (!context.IsSkipped)
        {
            if (context.IsEngineMode)
                GodotSignalCollector.Instance.Clean();
            context.MemoryPool.SetActive(StageName);
            await base
                .Execute(context)
                .ConfigureAwait(true);
            await context.MemoryPool
                .Gc()
                .ConfigureAwait(true);
            if (context.MemoryPool.OrphanCount > 0)
                context.ReportCollector.PushFront(new TestReport(Warning, 0, ReportOrphans(context)));
        }

        context.FireAfterTestEvent();
    }

    private static AfterTestAttribute? AfterTestAttribute(ExecutionContext context) => context.TestSuite.Instance
        .GetType()
        .GetMethods()
        .FirstOrDefault(m => m.IsDefined(typeof(AfterTestAttribute)))
        ?.GetCustomAttribute<AfterTestAttribute>();

    private static BeforeTestAttribute? BeforeTestAttribute(ExecutionContext context) => context.TestSuite.Instance
        .GetType()
        .GetMethods()
        .FirstOrDefault(m => m.IsDefined(typeof(BeforeTestAttribute)))
        ?.GetCustomAttribute<BeforeTestAttribute>();

    private static string ReportOrphans(ExecutionContext context)
    {
        var beforeAttribute = BeforeTestAttribute(context);
        var afterAttributes = AfterTestAttribute(context);
        if (beforeAttribute != null && afterAttributes != null)
        {
            return $"""
                    {AssertFailures.FormatValue("WARNING:", AssertFailures.WARN_COLOR, false)}
                        Detected <{context.MemoryPool.OrphanCount}> orphan nodes during test setup stage!
                        Check [b]{beforeAttribute.Name + ":" + beforeAttribute.Line}[/b] and [b]{afterAttributes.Name + ":" + afterAttributes.Line}[/b] for unfreed instances!
                    """;
        }

        return $"""
                {AssertFailures.FormatValue("WARNING:", AssertFailures.WARN_COLOR, false)}
                    Detected <{context.MemoryPool.OrphanCount}> orphan nodes during test setup stage!
                    Check [b]{(beforeAttribute != null ? beforeAttribute.Name + ":" + beforeAttribute.Line : afterAttributes?.Name + ":" + afterAttributes?.Line)}[/b] for unfreed instances!
                """;
    }
}

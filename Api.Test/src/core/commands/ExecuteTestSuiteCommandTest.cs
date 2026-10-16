namespace GdUnit4.Tests.Core.Commands;

using System;
using System.Reflection;
using System.Threading.Tasks;

using GdUnit4.Api;
using GdUnit4.Core.Commands;
using GdUnit4.Core.Execution;

using static Assertions;

[TestSuite]
public class ExecuteTestSuiteCommandTest
{
    [TestCase]
    public async Task ExecuteSuiteThatCannotBeLoadedThrows()
    {
        var command = new ExecuteTestSuiteCommand(CreateSuiteNode(typeof(FailingConstructorSuite)), false, true);

        (await AssertThrown(command.Execute(new NoInteractTestEventListener())))!
            .IsInstanceOf<TargetInvocationException>()
            .HasPropertyValue("InnerException", FailingConstructorSuite.Failure);
    }

    private static TestSuiteNode CreateSuiteNode(Type suiteType)
    {
        var suiteId = Guid.NewGuid();
        return new TestSuiteNode
        {
            Id = suiteId,
            ParentId = Guid.Empty,
            ManagedType = suiteType.FullName!,
            AssemblyPath = suiteType.Assembly.Location,
            SourceFile = "ExecuteTestSuiteCommandTest.cs",
            Tests =
            [
                new TestCaseNode
                {
                    Id = Guid.NewGuid(),
                    ParentId = suiteId,
                    ManagedMethod = "TestFoo",
                    LineNumber = 1,
                    AttributeIndex = 0,
                    RequireRunningGodotEngine = false
                }
            ]
        };
    }

    private sealed class FailingConstructorSuite
    {
        internal static readonly InvalidOperationException Failure = new("The test suite constructor fails.");

        public FailingConstructorSuite() => throw Failure;
    }
}

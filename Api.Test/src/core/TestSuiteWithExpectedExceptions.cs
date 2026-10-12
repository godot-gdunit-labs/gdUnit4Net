namespace GdUnit4.Tests.Core;

using System;
using System.Threading.Tasks;

using GdUnit4.Core.Execution.Exceptions;

using static Assertions;

[TestSuite]
public class TestSuiteWithExpectedExceptions
{
    [TestCase(Timeout = 100)]
    [ThrowsException(typeof(ExecutionTimeoutException), "The execution has timed out after 100ms.")]
    public async Task ExpectExecutionTimeoutException()
    {
        await Task.Delay(500);

        // will never be executed because the test is interrupted after a timeout of 100ms
        AssertBool(true).IsFalse();
    }

    [TestCase]
    [ThrowsException(typeof(TestFailedException), "Expecting: 'False' but is 'True'")]
    public void ExpectTestFailedException() =>
        AssertBool(true).IsFalse();

    [TestCase]
    [ThrowsException(typeof(TestFailedException), "Expecting: 'False' but is 'True'", 31)]
    public void ExpectTestFailedExceptionWithLineNumber() =>
        AssertBool(true).IsFalse();

    [TestCase]
    [ThrowsException(typeof(ArgumentException), "The argument 'message' is invalid")]
    public void ExpectArgumentException() =>
        throw new ArgumentException("The argument 'message' is invalid");

    [TestCase]
    [ThrowsException(typeof(NullReferenceException))]
    public void ExpectNullException()
    {
        string? value = null;

        // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
        value!.Contains(string.Empty); // This will throw NullReferenceException
    }
}

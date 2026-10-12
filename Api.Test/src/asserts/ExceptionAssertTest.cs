namespace GdUnit4.Tests.Asserts;

using System;
using System.IO;
using System.Threading.Tasks;

using GdUnit4.Asserts;

using static Assertions;

using static Utils;

[TestSuite]
public class ExceptionAssertTest
{
    private static IStringAssert AssertStackTrace(IExceptionAssert? exceptionAssert, int frame)
    {
        var stackTrace = (exceptionAssert as ExceptionAssert<Exception>)?.GetExceptionStackTrace();
        var stackFrames = stackTrace!.Split('\n');

        return AssertThat(stackFrames?[frame]);
    }

    [TestCase]
    [RequireGodotRuntime]
    public async Task TestCaseInnerException()
    {
        AssertThat(true).IsTrue();
        var assertion = AssertThrown(() => AssertThat(true).IsFalse())
            .HasFileLineNumber(29)
            .HasFileName("src/asserts/ExceptionAssertTest.cs");
        AssertStackTrace(assertion, 1)
            .Contains("   at GdUnit4.Tests.Asserts.ExceptionAssertTest.TestCaseInnerException()")
            .Contains("src\\asserts\\ExceptionAssertTest.cs:line 29".Replace('\\', Path.DirectorySeparatorChar));
        AssertThrown(()
                => AssertThat(true)
                    .IsFalse())
            .HasFileLineNumber(36)
            .HasFileName("src/asserts/ExceptionAssertTest.cs");

        // do a context switch
        await DoWait(100);
        AssertThrown(()
                => AssertThat(true)
                    .IsFalse())
            .HasFileLineNumber(44)
            .HasFileName("src/asserts/ExceptionAssertTest.cs");

        // do a Godot frame context switch
        await ISceneRunner.SyncProcessFrame;
        AssertThrown(()
                => AssertThat(true)
                    .IsFalse())
            .HasFileLineNumber(52)
            .HasFileName("src/asserts/ExceptionAssertTest.cs");
    }

    [TestCase]
    [RequireGodotRuntime]
    public async Task TestCaseInnerMethodException()
    {
        await ISceneRunner.SyncProcessFrame;

        // we want to collect the stack trace inclusive sub method calls and this failure line number
        // https://github.com/MikeSchulze/gdUnit4Net/issues/49
        var assertion = AssertThrown(() => AssertInner(0, true, true))
            .HasFileLineNumber(79)
            .HasFileName("src/asserts/ExceptionAssertTest.cs");
        AssertStackTrace(assertion, 0)
            .Contains("src\\asserts\\ExceptionAssertTest.cs:line 79".Replace('\\', Path.DirectorySeparatorChar));
        AssertStackTrace(assertion, 1)
            .Contains("src\\asserts\\ExceptionAssertTest.cs:line 66".Replace('\\', Path.DirectorySeparatorChar));

        static void AssertInner(int val, bool isEven, bool isOdd)
        {
            AssertBool(val % 2 == 0).IsEqual(isEven);

            // this line should be reported as failure
            AssertBool(val % 2 != 0).IsEqual(isOdd);
        }
    }

    private static void DoAssert(int val, bool isEven, bool isOdd)
    {
        AssertBool(val % 2 == 0).IsEqual(isEven);

        // this line should be reported as failure
        AssertBool(val % 2 != 0).IsEqual(isOdd);
    }

    [TestCase]
    public void TestCaseOuterMethodException()
    {
        var assertion = AssertThrown(() => DoAssert(0, true, true))
            .HasFileLineNumber(88) as ExceptionAssert<Exception>;
        AssertStackTrace(assertion, 0)
            .Contains("   at GdUnit4.Tests.Asserts.ExceptionAssertTest.DoAssert(Int32 val, Boolean isEven, Boolean isOdd)")
            .Contains("src\\asserts\\ExceptionAssertTest.cs:line 88".Replace('\\', Path.DirectorySeparatorChar));
        AssertStackTrace(assertion, 2)
            .Contains("at GdUnit4.Tests.Asserts.ExceptionAssertTest.TestCaseOuterMethodException()")
            .Contains("src\\asserts\\ExceptionAssertTest.cs:line 94".Replace('\\', Path.DirectorySeparatorChar));
    }

    [TestCase]
    [RequireGodotRuntime]
    public async Task TestCaseOuterMethodExceptionAndAwait()
    {
        await ISceneRunner.SyncProcessFrame;
        var assertion = AssertThrown(() => DoAssert(0, true, true))
            .HasFileLineNumber(88)
            .HasFileName("src/asserts/ExceptionAssertTest.cs") as ExceptionAssert<Exception>;
        AssertStackTrace(assertion, 0)
            .Contains("   at GdUnit4.Tests.Asserts.ExceptionAssertTest.DoAssert(Int32 val, Boolean isEven, Boolean isOdd)")
            .Contains("src\\asserts\\ExceptionAssertTest.cs:line 88".Replace('\\', Path.DirectorySeparatorChar));
        AssertStackTrace(assertion, 2)
            .Contains("at GdUnit4.Tests.Asserts.ExceptionAssertTest.TestCaseOuterMethodExceptionAndAwait()")
            .Contains("src\\asserts\\ExceptionAssertTest.cs:line 109".Replace('\\', Path.DirectorySeparatorChar));
    }
}

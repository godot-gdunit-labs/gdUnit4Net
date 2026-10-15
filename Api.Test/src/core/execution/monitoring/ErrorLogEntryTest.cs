namespace GdUnit4.Tests.Core.Execution.Monitoring;

using System;

using GdUnit4.Core.Execution.Monitoring;

using static Assertions;

// the entry checks whether the Godot debugger is active, which calls into the Godot runtime
[RequireGodotRuntime]
[TestSuite]
public class ErrorLogEntryTest
{
    #region exception type resolving

    [TestCase]
    public void ExtractExceptionResolvesKnownExceptionType()
    {
        var entry = ErrorLogEntry.ExtractException(["ERROR: System.InvalidOperationException: The operation failed.", "   at Foo.Bar()"], 0);

        AssertThat(entry).IsNotNull();
        AssertThat(entry!.ExceptionType).IsEqual(typeof(InvalidOperationException));
        AssertThat(entry.Message).IsEqual("The operation failed.");
    }

    [TestCase]
    public void ExtractExceptionWithUnknownExceptionType()
    {
        var entry = ErrorLogEntry.ExtractException(["ERROR: Foo.Bar.UnknownException: The operation failed.", "   at Foo.Bar()"], 0);

        AssertThat(entry).IsNotNull();
        AssertThat(entry!.ExceptionType).IsNull();
        AssertThat(entry.Message).IsEqual("The operation failed.");
    }

    #endregion
}

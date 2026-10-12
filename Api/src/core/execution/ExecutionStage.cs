// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Execution;

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.RegularExpressions;

using Exceptions;

using Extensions;

using Monitoring;

using Reporting;

using static Api.ReportType;

internal abstract class ExecutionStage<T> : IExecutionStage
{
    private GodotExceptionMonitor? godotExceptionMonitor;

    protected ExecutionStage(string name, Type type)
    {
        var method = type
            .GetMethods()
            .FirstOrDefault(m => m.IsDefined(typeof(T)));
        InitExecutionAttributes(method?.Name ?? name, method, method?.GetCustomAttribute<TestStageAttribute>()!);
    }

    protected ExecutionStage(string name, MethodInfo method, TestStageAttribute stageAttribute) => InitExecutionAttributes(name, method, stageAttribute);

    internal bool IsMonitoringOnGodotExceptionsEnabled { get; set; }

    protected string StageName { get; private set; } = string.Empty;

    private bool IsAsync { get; set; }

    private bool IsTask { get; set; }

    private int DefaultTimeout { get; } = 30000;

    private MethodInfo? Method { get; set; }

    private TestStageAttribute? StageAttribute { get; set; }

    public virtual async Task Execute(ExecutionContext context)
    {
        // no stage defined?
        if (Method == null)
        {
            await Task.Run(() => { }).ConfigureAwait(true);
            return;
        }

        try
        {
            // if the method is defined asynchronously, the return type must be a Task
            if (IsAsync != IsTask)
            {
                context.ReportCollector.Consume(
                    new TestReport(
                        Failure,
                        ExecutionLineNumber(context),
                        $"Invalid method signature found at: {StageName}.\n You must return a <Task> for an asynchronously specified method."));
                return;
            }

            if (IsMonitoringOnGodotExceptionsEnabled && context.IsEngineMode)
            {
                godotExceptionMonitor = new GodotExceptionMonitor();
                godotExceptionMonitor.Start();
            }

            await ExecuteStage(context).ConfigureAwait(true);
            if (godotExceptionMonitor != null && IsMonitoringOnGodotExceptionsEnabled && context.IsEngineMode)
            {
                await godotExceptionMonitor
                    .StopThrow()
                    .ConfigureAwait(true);
            }

            _ = ValidateForExpectedException(context);
        }
        catch (ExecutionTimeoutException e)
        {
            if (ValidateForExpectedException(context, e))
                return;
            if (context.FailureReporting)
                context.ReportCollector.Consume(new TestReport(Interrupted, e));
        }
        catch (TestFailedException e)
        {
            ReportAsFailure(context, e);
        }
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            if (e.GetBaseException() is TestFailedException ex)
                ReportAsFailure(context, ex);
            else

                // handle unexpected exceptions
                ReportUnexpectedException(context, e);
        }
    }

    internal static string TrimStackTrace(string stackTrace)
    {
        if (stackTrace.Length == 0)
            return stackTrace;

        StringBuilder result = new(stackTrace.Length);
        var stackFrames = Regex.Split(stackTrace, Environment.NewLine);

        foreach (var stackFrame in stackFrames)
        {
            if (string.IsNullOrEmpty(stackFrame) || stackFrame.Contains("Microsoft.VisualStudio.TestTools", StringComparison.Ordinal))
                continue;

            result = result
                .Append(stackFrame)
                .Append(Environment.NewLine);
        }

        return result.ToString();
    }

    private static int ScanFailureLineNumber(StackTrace stack)
    {
        foreach (var frame in stack.GetFrames().Reverse())
        {
            if (frame.GetFileName() == null)
                continue;
            if (frame.GetMethod()?.IsDefined(typeof(TestCaseAttribute)) ?? false)
                return frame.GetFileLineNumber();
        }

        return stack.FrameCount > 1 ? stack.GetFrame(0)!.GetFileLineNumber() : -1;
    }

    private void InitExecutionAttributes(string stageName, MethodInfo? method, TestStageAttribute stageAttribute)
    {
        StageName = stageName;
        Method = method;
        StageAttribute = stageAttribute;
        IsAsync = method?.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null;
        IsTask = method?.ReturnType.IsEquivalentTo(typeof(Task)) ?? false;
        var isClassMonitored = method?.DeclaringType?.GetCustomAttributes<GodotExceptionMonitorAttribute>().Any() ?? false;
        var isMethodMonitored = method?.GetCustomAttributes<GodotExceptionMonitorAttribute>().Any() ?? false;
        IsMonitoringOnGodotExceptionsEnabled = isClassMonitored || isMethodMonitored;
    }

    private bool ValidateForExpectedException(ExecutionContext context, Exception? e = null)
    {
        try
        {
            if (context.IsExpectingToFailWithException(e, Method))
                return true;
        }
        catch (TestFailedException e2)
        {
            context.ReportCollector.Consume(new TestReport(e2));
            return true;
        }

        return false;
    }

    private void ReportAsFailure(ExecutionContext context, TestFailedException e)
    {
        if (ValidateForExpectedException(context, e))
            return;

        if (context.FailureReporting)
            context.ReportCollector.Consume(new TestReport(e));
    }

    private void ReportUnexpectedException(ExecutionContext context, Exception exception)
    {
        if (exception is TargetInvocationException)
        {
            var ei = ExceptionDispatchInfo.Capture(exception.InnerException ?? exception);
            ReportUnexpectedException(context, ei.SourceException);
        }
        else
        {
            if (ValidateForExpectedException(context, exception))
                return;
            var stack = new StackTrace(exception, true);
            var lineNumber = ScanFailureLineNumber(stack);
            context.ReportCollector.Consume(new TestReport(Failure, lineNumber, exception.Message, TrimStackTrace(stack.ToString())));
        }
    }

    private async Task ExecuteStage(ExecutionContext context)
    {
        var timeout = TimeSpan.FromMilliseconds(StageAttribute?.Timeout ?? DefaultTimeout);
        var task = Method?.Invoke(context.TestSuite.Instance, context.MethodArguments) as Task ?? Task.CompletedTask;
        var completedTask = await Task
            .WhenAny(task, Task.Delay(timeout))
            .ConfigureAwait(true);
        if (completedTask == task)
            await task.ConfigureAwait(true); // Propagate exceptions from the original task
        else
            throw new ExecutionTimeoutException($"The execution has timed out after {timeout.Humanize()}.", ExecutionLineNumber(context));
    }

    private int ExecutionLineNumber(ExecutionContext context)
    {
        if (StageAttribute?.Line == -1)
            return context.CurrentTestCase?.Line ?? -1;
        return StageAttribute?.Line ?? -1;
    }
}

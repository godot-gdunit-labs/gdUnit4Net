// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Reporting;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

using Api;

using Execution.Exceptions;

using Extensions;

internal sealed class TestReport : ITestReport, IEquatable<TestReport>
{
    [JsonConstructor]
    public TestReport(ReportType type, int lineNumber, string message, string? stackTrace = null)
    {
        Type = type;
        LineNumber = lineNumber;
        Message = message.UnixFormat();
        StackTrace = stackTrace;
    }

    public TestReport(TestFailedException e)
        : this(ReportType.Failure, e)
    {
    }

    public TestReport(ReportType type, TestFailedException e)
    {
        Type = type;
        LineNumber = e.LineNumber;
        Message = e.Message;
        StackTrace = e.StackTrace;
    }

    public ReportType Type { get; }

    public int LineNumber { get; }

    public string Message { get; }

    public string? StackTrace { get; set; }

    public bool IsError => ErrorTypes.Contains(Type);

    public bool IsFailure => Type == ReportType.Failure;

    public bool IsWarning => Type == ReportType.Warning;

    private IEnumerable<ReportType> ErrorTypes
        => [ReportType.Terminated, ReportType.Interrupted, ReportType.Abort];

    public static bool operator ==(TestReport lhs, TestReport rhs) => lhs.Equals(rhs);

    public static bool operator !=(TestReport lhs, TestReport rhs) => !(lhs == rhs);

    public IDictionary<string, object> Serialize() => new Dictionary<string, object>
    {
        { "type", (int)Type },
        { "line_number", LineNumber },
        { "message", Message }
    };

    public bool Equals(TestReport? other)
        => other is not null
           && Type == other.Type
           && LineNumber == other.LineNumber
           && Message == other.Message
           && IsError == other.IsError
           && IsFailure == other.IsFailure
           && IsWarning == other.IsWarning;

    public override bool Equals(object? obj)
        => obj is TestReport other && Equals(other);

    public bool Equals(ITestReport? other)
        => throw new NotImplementedException();

    public override int GetHashCode() =>
        HashCode.Combine(Type, LineNumber, Message, IsError, IsFailure, IsWarning);

    public override string ToString()
        => $"[color=green]line [/color][color=aqua]{LineNumber}:[/color]\n {Message}";

    public TestReport Deserialize(IDictionary<string, object> serialized)
    {
        var type = (ReportType)Enum.Parse(typeof(ReportType), (string)serialized["type"]);
        var lineNumber = (int)serialized["line_number"];
        var message = (string)serialized["message"];
        return new TestReport(type, lineNumber, message);
    }
}

// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Execution;

using System.Globalization;

using Api;

using Newtonsoft.Json;

using Reporting;

internal class TestEvent : ITestEvent, IEquatable<TestEvent>
{
    // constructor needs to serialize/deserialize by JsonConvert
    [JsonConstructor]
    private TestEvent()
    {
        SuiteName = string.Empty;
        TestName = string.Empty;
        ResourcePath = string.Empty;
        FullyQualifiedName = string.Empty;
    }

    private TestEvent(
        EventType eventType,
        string resourcePath,
        string suiteName,
        string testName,
        int totalCount = 0,
        IDictionary<StatisticKey, object>? statistics = null,
        IEnumerable<ITestReport>? reports = null)
    {
        Type = eventType;
        ResourcePath = resourcePath;
        SuiteName = suiteName;
        TestName = testName;
        Statistics = statistics ?? new Dictionary<StatisticKey, object>();
        Statistics[StatisticKey.TotalCount] = totalCount;
        Reports = reports?.ToList() ?? [];
        FullyQualifiedName = string.Empty;
    }

    private TestEvent(EventType eventType, Guid id, string resourcePath, string suiteName, string testName)
    {
        Type = eventType;
        Id = id;
        ResourcePath = resourcePath;
        SuiteName = suiteName;
        TestName = testName;
        Statistics = new Dictionary<StatisticKey, object> { [StatisticKey.TotalCount] = 0 };
        Reports = [];
        FullyQualifiedName = string.Empty;
    }

    internal enum StatisticKey
    {
        Warnings,
        Failed,
        Errors,
        Skipped,
        ElapsedTime,
        OrphanNodes,
        TotalCount,
        ErrorCount,
        FailedCount,
        SkippedCount
    }

    public Guid Id { get; set; }

    public EventType Type { get; set; }

    public string SuiteName { get; set; }

    public string TestName { get; set; }

    public string ResourcePath { get; set; }

    public string? DisplayName { get; set; }

    public string FullyQualifiedName { get; set; }

    public IDictionary<StatisticKey, object> Statistics { get; private init; } = new Dictionary<StatisticKey, object>();

    [JsonProperty]
    [JsonConverter(typeof(TestReportListConverter))]
    public ICollection<ITestReport> Reports { get; private init; } = [];

    public int TotalCount => GetByKeyOrDefault(StatisticKey.TotalCount, 0);

    public int ErrorCount => GetByKeyOrDefault(StatisticKey.ErrorCount, 0);

    public int FailedCount => GetByKeyOrDefault(StatisticKey.FailedCount, 0);

    public int OrphanCount => GetByKeyOrDefault(StatisticKey.OrphanNodes, 0);

    public int SkippedCount => GetByKeyOrDefault(StatisticKey.SkippedCount, 0);

    public bool IsWarning => GetByKeyOrDefault(StatisticKey.Warnings, false);

    public bool IsSkipped => GetByKeyOrDefault(StatisticKey.Skipped, false);

    public bool IsFailed => GetByKeyOrDefault(StatisticKey.Failed, false);

    public bool IsError => GetByKeyOrDefault(StatisticKey.Errors, false);

    public bool IsSuccess => !IsWarning && !IsFailed && !IsError && !IsSkipped;

    public TimeSpan ElapsedInMs => TimeSpan.FromMilliseconds(GetByKeyOrDefault(StatisticKey.ElapsedTime, 0L));

    public static bool operator ==(TestEvent? lhs, TestEvent? rhs) => lhs?.Equals(rhs) ?? (rhs is null);

    public static bool operator !=(TestEvent? lhs, TestEvent? rhs) => !(lhs == rhs);

    public static IDictionary<StatisticKey, object> BuildStatistics(
        int orphanCount,
        bool isError,
        int errorCount,
        bool isFailure,
        int failureCount,
        bool isWarning,
        bool isSkipped,
        int skippedCount,
        long elapsedSinceMs) => new Dictionary<StatisticKey, object>
    {
        { StatisticKey.OrphanNodes, orphanCount },
        { StatisticKey.ElapsedTime, elapsedSinceMs },
        { StatisticKey.Warnings, isWarning },
        { StatisticKey.Errors, isError },
        { StatisticKey.ErrorCount, errorCount },
        { StatisticKey.Failed, isFailure },
        { StatisticKey.FailedCount, failureCount },
        { StatisticKey.Skipped, isSkipped },
        { StatisticKey.SkippedCount, skippedCount }
    };

    public static TestEvent Before(string resourcePath, string suiteName, int totalCount, IDictionary<StatisticKey, object> statistics, IEnumerable<ITestReport> reports) =>
        new(EventType.SuiteBefore, resourcePath, suiteName, "Before", totalCount, statistics, reports);

    public static TestEvent After(string resourcePath, string suiteName, IDictionary<StatisticKey, object> statistics, IEnumerable<ITestReport> reports) =>
        new(EventType.SuiteAfter, resourcePath, suiteName, "After", 0, statistics, reports);

    public static TestEvent BeforeTest(Guid id, string resourcePath, string suiteName, string testName) =>
        new(EventType.TestBefore, id, resourcePath, suiteName, testName);

    public static TestEvent AfterTest(
        Guid id,
        string resourcePath,
        string suiteName,
        string testName,
        IDictionary<StatisticKey, object>? statistics = null,
        List<ITestReport>? reports = null) =>
        new(EventType.TestAfter, id, resourcePath, suiteName, testName)
        {
            Statistics = statistics ?? new Dictionary<StatisticKey, object>(),
            Reports = reports ?? []
        };

    public bool Equals(TestEvent? other)
    {
        if (other is null)
            return false;

        // Compare reference first for performance
        if (ReferenceEquals(this, other))
            return true;

        // Use EqualityComparer for each field
        return EqualityComparer<EventType>.Default.Equals(Type, other.Type)
               && StringComparer.Ordinal.Equals(ResourcePath, other.ResourcePath)
               && StringComparer.Ordinal.Equals(SuiteName, other.SuiteName)
               && StringComparer.Ordinal.Equals(TestName, other.TestName)
               && EqualityComparer<int>.Default.Equals(TotalCount, other.TotalCount)
               && EqualityComparer<int>.Default.Equals(ErrorCount, other.ErrorCount)
               && EqualityComparer<int>.Default.Equals(FailedCount, other.FailedCount)
               && EqualityComparer<int>.Default.Equals(OrphanCount, other.OrphanCount);
    }

    public override bool Equals(object? obj)
    {
        if (obj is TestEvent other)
            return Equals(other);
        return false;
    }

    public override int GetHashCode() =>

        // ReSharper disable all NonReadonlyMemberInGetHashCode
        HashCode.Combine(
            Type,
            ResourcePath,
            SuiteName,
            TestName,
            TotalCount,
            ErrorCount,
            FailedCount,
            OrphanCount);

    public override string ToString() => $"Event: {Type} {SuiteName}:{TestName}, IsSuccess:{IsSuccess} ";

    internal TestEvent WithStatistic(StatisticKey key, object value)
    {
        Statistics[key] = value;
        return this;
    }

    internal TestEvent WithFullyQualifiedName(string name)
    {
        FullyQualifiedName = name;
        return this;
    }

    internal TestEvent WithDisplayName(string? name)
    {
        DisplayName = name;
        return this;
    }

    internal TestEvent WithReport(ITestReport report)
    {
        Reports.Add(report);
        return this;
    }

#pragma warning disable CA1854
    private T GetByKeyOrDefault<T>(StatisticKey key, T defaultValue) =>
        Statistics.ContainsKey(key) ? (T)Convert.ChangeType(Statistics[key], typeof(T), CultureInfo.InvariantCulture) : defaultValue;

#pragma warning restore CA1854
}

#pragma warning disable SA1402
internal class TestReportListConverter : JsonConverter<List<ITestReport>>
#pragma warning restore SA1402
{
    public override List<ITestReport> ReadJson(JsonReader reader, Type objectType, List<ITestReport>? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var reports = serializer.Deserialize<List<TestReport>>(reader);
        return reports?.Cast<ITestReport>().ToList() ?? [];
    }

    public override void WriteJson(JsonWriter writer, List<ITestReport>? value, JsonSerializer serializer)
        => serializer.Serialize(writer, value?.Cast<TestReport>().ToList());
}

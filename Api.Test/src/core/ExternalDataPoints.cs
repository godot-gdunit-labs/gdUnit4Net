namespace GdUnit4.Tests.Core;

using System.Collections.Generic;

// The private data points are resolved by reflection to test access to non-public members
#pragma warning disable IDE0051
public static class ExternalDataPoints
{
    public static IEnumerable<object[]> PublicArrayDataPointProperty => [[1, 2, 3], [4, 5, 9]];

    private static IEnumerable<object[]> PrivateArrayDataPointProperty => [[1, 2, 3], [4, 5, 9]];

    public static IEnumerable<object[]> PublicArrayDataPointMethod() => [[1, 2, 3], [4, 5, 9]];

    private static IEnumerable<object[]> PrivateArrayDataPointMethod() => [[1, 2, 3], [4, 5, 9]];
}
#pragma warning restore IDE0051

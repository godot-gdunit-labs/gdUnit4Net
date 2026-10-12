namespace GdUnit4.Tests.Core;

using System.Collections.Generic;
using System.Threading.Tasks;

using GdUnit4.Core.Data;

using static Assertions;

/// <summary>
///     The test are similar to https://github.com/microsoft/testfx/blob/cdc374674477f23502f58b5be503ed609ba4057c/docs/RFCs/006-DynamicData-Attribute.md.
/// </summary>
[TestSuite]
public class TestSuiteWithDynamicDataPoints
{
    public static IEnumerable<object[]> ArrayDataPointProperty => [[1, 2, 3], [4, 5, 9]];

    public static IEnumerable<int> SingleDataPointProperty => [1, 2, 3];

    public static IEnumerable<object[]> ArrayDataPointMethod() => [[1, 2, 3], [4, 5, 9]];

    public static IEnumerable<object[]> PublicTestDataFactory(int factor) => [[1 * factor, 1 * factor], [2 * factor, 2 * factor]];

#pragma warning disable CA1859 // #warning directive
    private static IEnumerable<object[]> PrivateArrayDataPointProperty => [[1, 2, 3], [4, 5, 9]];

    private static IEnumerable<object[]> PrivateArrayDataPointMethod() => [[1, 2, 3], [4, 5, 9]];

    private static IEnumerable<object[]> PrivateTestDataFactory(int factor) => [[1 * factor, 1 * factor], [2 * factor, 2 * factor]];
#pragma warning restore CS1030 // #warning directive

    internal static IEnumerable<object[]> YieldedDataPointMethod()
    {
        yield return [1, 2, 3];
        yield return [4, 5, 9];
    }

    #region private_data_points

    [TestCase]
    [DataPoint(nameof(PrivateArrayDataPointProperty))]
    public void OnPrivateArrayDataPointProperty(int a, int b, int expected) => AssertThat(a + b).IsEqual(expected);

    [TestCase]
    [DataPoint(nameof(PrivateArrayDataPointMethod))]
    public void OnPrivateArrayDataPointMethod(int a, int b, int expected) => AssertThat(a + b).IsEqual(expected);

    [TestCase]
    [DataPoint("PrivateArrayDataPointProperty", typeof(ExternalDataPoints))]
    public void OnExternalPrivateArrayDataPointProperty(int a, int b, int expected) => AssertThat(a + b).IsEqual(expected);

    [TestCase]
    [DataPoint("PrivateArrayDataPointMethod", typeof(ExternalDataPoints))]
    public void OnExternalPrivateArrayDataPointMethod(int a, int b, int expected) => AssertThat(a + b).IsEqual(expected);

    [TestCase]
    [DataPoint(nameof(PrivateTestDataFactory), 2)]
    public void ParameterizedPrivateDataPoint(int value, int expected) => AssertThat(value).IsEqual(expected);

    #endregion

    #region public_data_points

    [TestCase]
    [DataPoint(nameof(ArrayDataPointProperty))]
    public void OnPublicArrayDataPointProperty(int a, int b, int expected) => AssertThat(a + b).IsEqual(expected);

    [TestCase]
    [DataPoint(nameof(SingleDataPointProperty))]
    public void OnPublicSingleDataPointProperty(int value) => AssertThat(value).IsBetween(1, 3);

    [TestCase]
    [DataPoint(nameof(ArrayDataPointMethod))]
    public void OnPublicArrayDataPointMethod(int a, int b, int expected) => AssertThat(a + b).IsEqual(expected);

    [TestCase]
    [DataPoint(nameof(YieldedDataPointMethod))]
    public void DataPointYieldedMethod(int a, int b, int expected) => AssertThat(a + b).IsEqual(expected);

    [TestCase]
    [DataPoint(nameof(ExternalDataPoints.PublicArrayDataPointProperty), typeof(ExternalDataPoints))]
    public void OnExternalPublicArrayDataPointProperty(int a, int b, int expected) => AssertThat(a + b).IsEqual(expected);

    [TestCase]
    [DataPoint(nameof(ExternalDataPoints.PublicArrayDataPointMethod), typeof(ExternalDataPoints))]
    public void OnExternalPublicArrayDataPointMethod(int a, int b, int expected) => AssertThat(a + b).IsEqual(expected);

    [TestCase]
    [DataPoint(nameof(PublicTestDataFactory), 2)]
    public void ParameterizedPublicDataPoint(int value, int expected) => AssertThat(value).IsEqual(expected);

    #endregion

    #region public_async_data_points

    public static async IAsyncEnumerable<object?[]> AsyncArrayDataPoint()
    {
        for (var i = 0; i < 3; i++)
        {
            // Simulate async work for each item
            await Task.Delay(10);
            yield return [i, i + 1, i + i + 1];
        }
    }

    public static async IAsyncEnumerable<object?[]> AsyncArrayDataPointBlocked()
    {
        await Task.Delay(500);
        yield return [1, 2, 3];
    }

    public static async IAsyncEnumerable<int> AsyncSingleDataPoint()
    {
        yield return 1;
        await Task.Delay(10);
        yield return 2;
        await Task.Delay(10);
        yield return 3;
    }

    [TestCase]
    [DataPoint(nameof(AsyncArrayDataPoint))]
    public void OnAsyncArrayDataPoint(int a, int b, int expected) => AssertThat(a + b).IsEqual(expected);

    [TestCase]
    [DataPoint(nameof(AsyncSingleDataPoint))]
    public void OnAsyncSingleDataPoint(int a) => AssertThat(a).IsBetween(1, 3);

    [TestCase(Timeout = 100)]
    [DataPoint(nameof(AsyncArrayDataPointBlocked))]
    [ThrowsException(typeof(AsyncDataPointCanceledException), "The execution has timed out after 100ms.")]
    public void OnAsyncArrayDataPointFailByTimeout(int a, int b, int expected) => AssertThat(a + b).IsEqual(expected);

    #endregion
}

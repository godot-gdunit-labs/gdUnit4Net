// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text
namespace GdUnit4;

using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

using Asserts;

using Constraints;

using Core.Execution.Exceptions;
using Core.Execution.Monitoring;
using Core.Extensions;

using Extractors;

using Godot;
using Godot.Collections;

using ITuple = Asserts.ITuple;
using Tuple = Asserts.Tuple;

/// <summary>
///     A collection of assertions and helpers to verify values.
/// </summary>
public abstract partial class Assertions
{
    private static readonly HashSet<Variant.Type> CustomAssertForVariantType =
    [
        Variant.Type.Int,
        Variant.Type.Float,
        Variant.Type.Bool,
        Variant.Type.String,
        Variant.Type.StringName,
        Variant.Type.Vector2,
        Variant.Type.Vector2I,
        Variant.Type.Vector3,
        Variant.Type.Vector3I,
        Variant.Type.Vector4,
        Variant.Type.Vector4I
    ];

    /// <summary>
    ///     An Assertion to verify boolean values.
    /// </summary>
    /// <param name="current">The current boolean value to verify.</param>
    /// <returns>An instance of IBoolAssert for further assertions.</returns>
    public static IBoolAssert AssertBool(bool current) => new BoolAssert(current);

    /// <summary>
    ///     An Assertion to verify string values.
    /// </summary>
    /// <param name="current">The current string value to verify.</param>
    /// <returns>An instance of IStringAssert for further assertions.</returns>
    public static IStringAssert AssertString(string? current) => new StringAssert(current);

    /// <summary>
    ///     An Assertion to verify object values.
    /// </summary>
    /// <typeparam name="TValue">The type of the input.</typeparam>
    /// <param name="current">The current double value to verify.</param>
    /// <returns>An instance of IObjectAssert for further assertions.</returns>
    public static IObjectAssert<TValue> AssertObject<TValue>(TValue? current)
        => new ObjectAssert<TValue>(current);

    /// <summary>
    ///     An Assertion to verify object values.
    /// </summary>
    /// <param name="current">The current double value to verify.</param>
    /// <returns>An instance of IObjectAssert for further assertions.</returns>
    public static IObjectAssert<object?> AssertObject(object? current)
        => new ObjectAssert<object?>(current);

    /// <summary>
    ///     An Assertion to verify Godot signals.
    /// </summary>
    /// <param name="node">The object where is emitting the signal.</param>
    /// <returns>An instance of ISignalAssert for further assertions.</returns>
    public static ISignalAssert AssertSignal(GodotObject node)
        => new SignalAssert(node);

    /// <summary>
    ///     A dynamic assertion for <see cref="Variant" /> based on the input type.
    /// </summary>
    /// <param name="current">The input value to be asserted.</param>
    /// <returns>A dynamic assertion that provides assertion methods based on the input type.</returns>
    public static dynamic AssertThat(Variant current)
    {
        var unboxVariant = current.UnboxVariant();
        return CustomAssertForVariantType.Contains(current.VariantType)
            ? AssertThat(unboxVariant)
            : AssertObject(unboxVariant);
    }

    /// <summary>
    ///     An Assertion for <see cref="StringName" /> type.
    /// </summary>
    /// <param name="current">The input value to be asserted.</param>
    /// <returns>An instance of IStringAssert for further assertions.</returns>
    public static IStringAssert AssertThat(StringName current)
        => AssertString(current.UnboxVariant());

    /// <summary>
    ///     An Assertion to verify string values.
    /// </summary>
    /// <param name="current">The current string value to verify.</param>
    /// <returns>An instance of IStringAssert for further assertions.</returns>
    public static IStringAssert AssertThat(string? current)
        => new StringAssert(current);

    /// <summary>
    ///     An Assertion to verify boolean values.
    /// </summary>
    /// <param name="current">The current boolean value to verify.</param>
    /// <returns>An instance of IBoolAssert for further assertions.</returns>
    public static IBoolAssert AssertThat(bool current)
        => new BoolAssert(current);

    /// <summary>
    ///     A dynamic Assertion based on the input type.
    /// </summary>
    /// <typeparam name="TValue">The type of the input.</typeparam>
    /// <param name="current">The input value to be asserted.</param>
    /// <returns>A dynamic assertion that provides assertion methods based on the input type.</returns>
    public static dynamic AssertThat<TValue>(TValue? current)
        => current switch
        {
            bool v => AssertThat(v),
            string v => AssertThat(v),
            sbyte v => AssertThat(v),
            byte v => AssertThat(v),
            short v => AssertThat(v),
            ushort v => AssertThat(v),
            int v => AssertThat(v),
            uint v => AssertThat(v),
            long v => AssertThat(v),
            ulong v => AssertThat(v),
            float v => AssertThat(v),
            double v => AssertThat(v),
            decimal v => AssertThat(v),
            IDictionary v => AssertDictionary<TValue>(v),
            IEnumerable v => AssertThat<TValue>(v),
            _ => AssertObject<TValue>(current.UnboxVariant())
        };

    /// <summary>
    ///     An Assertion used by test generation to notify the test is not yet implemented.
    /// </summary>
    public static void AssertNotYetImplemented()
        => throw new TestFailedException("Test not yet implemented!");

    /// <summary>
    ///     Asserts that an exception of type <see cref="Exception" /> is thrown when executing the supplied action.
    /// </summary>
    /// <param name="action">An action that may throw an exception.</param>
    /// <returns>An instance of <see cref="IExceptionAssert" /> for further assertions on the thrown exception.</returns>
    public static IExceptionAssert AssertThrown(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return new ExceptionAssert<Exception>(action);
    }

    /// <summary>
    ///     An Assertion to verify for expecting exceptions when performing a task.
    /// </summary>
    /// <typeparam name="TAction">The type of the task result.</typeparam>
    /// <param name="task">A task that may throw an exception during execution.</param>
    /// <returns>
    ///     A task that resolves to an <see cref="IExceptionAssert" /> if an exception was thrown,
    ///     or null if the task completed successfully.
    /// </returns>
    /// <remarks>
    ///     This overload accepts a generic Task and captures any exception thrown during execution.
    /// </remarks>
    /// <example>
    ///     <code>
    ///     await AssertThrown(task.WithTimeout(500))
    ///        .ContinueWith(result => result.Result.HasMessage("timed out after 500ms."));
    ///     </code>
    /// </example>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "User test code boundary: the assertion captures any exception the task under test throws")]
    public static async Task<IExceptionAssert?> AssertThrown<TAction>(Task<TAction> task)
    {
        ArgumentNullException.ThrowIfNull(task);

        try
        {
            _ = await task.ConfigureAwait(true);
            return default;
        }
        catch (Exception e)
        {
            return new ExceptionAssert<Exception>(e);
        }
    }

    /// <summary>
    ///     An Assertion to verify for expecting exceptions when performing a task.
    /// </summary>
    /// <param name="task">A task that may throw an exception during execution.</param>
    /// <returns>
    ///     A task that resolves to an <see cref="IExceptionAssert" /> if an exception was thrown,
    ///     or null if the task completed successfully.
    /// </returns>
    /// <remarks>
    ///     This overload accepts a non-generic Task and captures any exception thrown during execution.
    /// </remarks>
    /// <example>
    ///     <code>
    ///     await AssertThrown(asyncOperation.WithTimeout(500))
    ///         .ContinueWith(result => result.Result.HasMessage("Operation timed out"));
    ///     </code>
    /// </example>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "User test code boundary: the assertion captures any exception the task under test throws")]
    public static async Task<IExceptionAssert?> AssertThrown(Task task)
    {
        ArgumentNullException.ThrowIfNull(task);
        try
        {
            await task.ConfigureAwait(true);
            return default;
        }
        catch (Exception e)
        {
            return new ExceptionAssert<Exception>(e);
        }
    }

    // ----------- Helpers -------------------------------------------------------------------------------------------------------

    /// <summary>
    ///     Adds the Node on the actual SceneTree to be processed during test execution.
    ///     The node is auto-freed and can be disabled by set autoFree = false.
    /// </summary>
    /// <typeparam name="TNode">The type of node to add.</typeparam>
    /// <param name="node">The node to add to the scene tree.</param>
    /// <param name="autoFree">Whether to automatically free the node after test execution.</param>
    /// <returns>The added node for method chaining.</returns>
    public static TNode AddNode<TNode>(TNode node, bool autoFree = true)
        where TNode : Node
    {
        if (autoFree)
            _ = AutoFree(node);
        var tree = Engine.GetMainLoop() as SceneTree;
        tree!.Root.AddChild(node);
        return node;
    }

    /// <summary>
    ///     A little helper to auto-freeing your created objects after test execution.
    /// </summary>
    /// <typeparam name="TValue">The type of Godot object to register.</typeparam>
    /// <param name="obj">The Godot object to register for automatic cleanup.</param>
    /// <returns>The registered object for method chaining.</returns>
    public static TValue AutoFree<TValue>(TValue obj)
        where TValue : GodotObject
        => MemoryPool.RegisterForAutoFree(obj);

    /// <summary>
    ///     Builds a tuple by given values.
    /// </summary>
    /// <param name="args">The values to include in the tuple.</param>
    /// <returns>A new tuple containing the specified values.</returns>
    public static ITuple Tuple(params object?[] args) => new Tuple(args);

    /// <summary>
    ///     Builds an extractor by given method name and optional arguments.
    /// </summary>
    /// <param name="methodName">The name of the method to extract.</param>
    /// <param name="args">Optional arguments to pass to the method.</param>
    /// <returns>A value extractor that can be used with assertion methods.</returns>
    public static IValueExtractor Extr(string methodName, params object[] args)
    {
        ArgumentNullException.ThrowIfNull(methodName);
        return new ValueExtractor(methodName, args);
    }

    /// <summary>
    ///     Waits for the specified signal to be emitted by a particular source node.
    ///     If the signal is not emitted within the given timeout, the operation fails.
    /// </summary>
    /// <example>
    ///     <code>
    ///      // Waits for the signal "mySignal" is emitted by myNode.
    ///      await runner.AwaitSignalOn(myNode, "mySignal");
    ///   </code>
    /// </example>
    /// <param name="source">The object from which the signal is emitted.</param>
    /// <param name="signal">The name of the signal to wait.</param>
    /// <param name="args">An optional set of signal arguments.</param>
    /// <returns>Task to wait.</returns>
    public static async Task<ISignalConstraint> AwaitSignalOn(GodotObject source, string signal, params Variant[] args) =>
        await ISceneRunner.AwaitSignalOn(source, signal, args).ConfigureAwait(true);

    /// <summary>
    ///     Provides the expected line number via compiler state.
    ///     Is primarily designed to use on internal test coverage to validate the reported error line is correct.
    /// </summary>
    /// <param name="lineNumber">The calling line number, automatically provided by the compiler.</param>
    /// <returns>The adjusted line number for error reporting.</returns>
    internal static int ExpectedLineNumber([CallerLineNumber] int lineNumber = 0) => lineNumber - 1;
}

// ###################### INumberAssert ############################################################################################

/// <summary>
///     A collection of assertions on Number types.
/// </summary>
public abstract partial class Assertions
{
    /// <summary>
    ///     An Assertion to verify sbyte values.
    /// </summary>
    /// <param name="current">The current sbyte value to verify.</param>
    /// <returns>An instance of INumberAssert for further assertions.</returns>
    public static INumberAssert<sbyte> AssertThat(sbyte current) => new NumberAssert<sbyte>(current);

    /// <summary>
    ///     An Assertion to verify byte values.
    /// </summary>
    /// <param name="current">The current byte value to verify.</param>
    /// <returns>An instance of INumberAssert for further assertions.</returns>
    public static INumberAssert<byte> AssertThat(byte current) => new NumberAssert<byte>(current);

    /// <summary>
    ///     An Assertion to verify short values.
    /// </summary>
    /// <param name="current">The current short value to verify.</param>
    /// <returns>An instance of INumberAssert for further assertions.</returns>
    public static INumberAssert<short> AssertThat(short current) => new NumberAssert<short>(current);

    /// <summary>
    ///     An Assertion to verify ushort values.
    /// </summary>
    /// <param name="current">The current ushort value to verify.</param>
    /// <returns>An instance of INumberAssert for further assertions.</returns>
    public static INumberAssert<ushort> AssertThat(ushort current) => new NumberAssert<ushort>(current);

    /// <summary>
    ///     An Assertion to verify int values.
    /// </summary>
    /// <param name="current">The current int value to verify.</param>
    /// <returns>An instance of INumberAssert for further assertions.</returns>
    public static INumberAssert<int> AssertThat(int current) => new NumberAssert<int>(current);

    /// <summary>
    ///     An Assertion to verify uint values.
    /// </summary>
    /// <param name="current">The current uint value to verify.</param>
    /// <returns>An instance of INumberAssert for further assertions.</returns>
    public static INumberAssert<uint> AssertThat(uint current) => new NumberAssert<uint>(current);

    /// <summary>
    ///     An Assertion to verify long values.
    /// </summary>
    /// <param name="current">The current long value to verify.</param>
    /// <returns>An instance of INumberAssert for further assertions.</returns>
    public static INumberAssert<long> AssertThat(long current) => new NumberAssert<long>(current);

    /// <summary>
    ///     An Assertion to verify ulong values.
    /// </summary>
    /// <param name="current">The current ulong value to verify.</param>
    /// <returns>An instance of INumberAssert for further assertions.</returns>
    public static INumberAssert<ulong> AssertThat(ulong current) => new NumberAssert<ulong>(current);

    /// <summary>
    ///     An Assertion to verify float values.
    /// </summary>
    /// <param name="current">The current float value to verify.</param>
    /// <returns>An instance of INumberAssert for further assertions.</returns>
    public static INumberAssert<float> AssertThat(float current) => new NumberAssert<float>(current);

    /// <summary>
    ///     An Assertion to verify double values.
    /// </summary>
    /// <param name="current">The current double value to verify.</param>
    /// <returns>An instance of INumberAssert for further assertions.</returns>
    public static INumberAssert<double> AssertThat(double current) => new NumberAssert<double>(current);

    /// <summary>
    ///     An Assertion to verify decimal values.
    /// </summary>
    /// <param name="current">The current decimal value to verify.</param>
    /// <returns>An instance of INumberAssert for further assertions.</returns>
    public static INumberAssert<decimal> AssertThat(decimal current) => new NumberAssert<decimal>(current);

    /// <summary>
    ///     An Assertion to verify integer values.
    /// </summary>
    /// <param name="current">The current integer value to verify.</param>
    /// <returns>An instance of INumberAssert for further assertions.</returns>
    public static INumberAssert<int> AssertInt(int current) => new NumberAssert<int>(current);

    /// <summary>
    ///     An Assertion to verify double values.
    /// </summary>
    /// <param name="current">The current double value to verify.</param>
    /// <returns>An instance of INumberAssert for further assertions.</returns>
    public static INumberAssert<double> AssertFloat(double current) => new NumberAssert<double>(current);
}

// ###################### IDictionaryAssert ############################################################################################

/// <summary>
///     A collection of assertions on Dictionary types.
/// </summary>
public abstract partial class Assertions
{
    /// <summary>
    ///     An Assertion to verify Dictionary values.
    /// </summary>
    /// <param name="current">The current dictionary value to verify.</param>
    /// <returns>An instance of IDictionaryAssert for further assertions.</returns>
    public static IDictionaryAssert<Variant, Variant> AssertThat(Dictionary? current)
        => new DictionaryAssert<Variant, Variant>(current);

    /// <summary>
    ///     An Assertion to verify IDictionary values.
    /// </summary>
    /// <param name="current">The current dictionary value to verify.</param>
    /// <returns>An instance of IDictionaryAssert for further assertions.</returns>
    public static IDictionaryAssert<object, object?> AssertThat(IDictionary? current)
        => DictionaryAssert<object, object?>.From(current);

    /// <summary>
    ///     A dynamic Assertion to verify generic IDictionary values.
    /// </summary>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="current">The current dictionary value to verify.</param>
    /// <returns>An instance of IDictionaryAssert for further assertions.</returns>
    public static dynamic AssertDictionary<TValue>(IDictionary? current)
    {
        var valueType = typeof(TValue);
        if (!valueType.IsGenericType)
            return DictionaryAssert<object, object?>.From(current);

        var assertType = typeof(DictionaryAssert<,>).MakeGenericType(valueType.GenericTypeArguments);
        var constructorArgType = typeof(IDictionary<,>).MakeGenericType(valueType.GenericTypeArguments);
        var constructor = (assertType.GetConstructor(
                               BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                               null,
                               [constructorArgType],
                               null)
                           ?? assertType.GetConstructor(
                               BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                               null,
                               [valueType],
                               null))
                          ?? throw new InvalidOperationException($"No suitable constructor found for {assertType.Name}");
        return constructor.Invoke([current]);
    }

    /// <summary>
    ///     An Assertion to verify Dictionary values.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="current">The current dictionary value to verify.</param>
    /// <returns>An instance of IDictionaryAssert for further assertions.</returns>
    public static IDictionaryAssert<TKey, TValue> AssertThat<TKey, TValue>(System.Collections.Generic.Dictionary<TKey, TValue>? current)
        where TKey : notnull
        where TValue : notnull
        => new DictionaryAssert<TKey, TValue>(current);

    /// <summary>
    ///     An Assertion to verify SortedDictionary values.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="current">The current dictionary value to verify.</param>
    /// <returns>An instance of IDictionaryAssert for further assertions.</returns>
    public static IDictionaryAssert<TKey, TValue> AssertThat<TKey, TValue>(SortedDictionary<TKey, TValue>? current)
        where TKey : notnull
        where TValue : notnull
        => new DictionaryAssert<TKey, TValue>(current);

    /// <summary>
    ///     An Assertion to verify ImmutableDictionary values.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="current">The current dictionary value to verify.</param>
    /// <returns>An instance of IDictionaryAssert for further assertions.</returns>
    public static IDictionaryAssert<TKey, TValue> AssertThat<TKey, TValue>(ImmutableDictionary<TKey, TValue>? current)
        where TKey : notnull
        where TValue : notnull
        => new DictionaryAssert<TKey, TValue>(current);

    /// <summary>
    ///     An Assertion to verify Godot Dictionary values.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="current">The current dictionary value to verify.</param>
    /// <returns>An instance of IDictionaryAssert for further assertions.</returns>
    public static IDictionaryAssert<TKey, TValue> AssertThat<[MustBeVariant] TKey, [MustBeVariant] TValue>(Dictionary<TKey, TValue>? current)
        where TKey : notnull
        where TValue : notnull
        => new DictionaryAssert<TKey, TValue>(current);
}

// ###################### IEnumerableAssert ############################################################################################

/// <summary>
///     A collection of assertions on Array/List types.
/// </summary>
public abstract partial class Assertions
{
    /// <summary>
    ///     The dynamic assertions for all enumerable types.
    /// </summary>
    /// <typeparam name="TValue">The type of elements in the array.</typeparam>
    /// <param name="current">The array values to verify.</param>
    /// <returns>An instance of IEnumerableAssert for further assertions.</returns>
    public static IEnumerableAssert<TValue?> AssertThat<TValue>(params TValue?[]? current)
        where TValue : class, IComparable
        => new EnumerableAssert<TValue?>(current);

    /// <summary>
    ///     The dynamic assertions for all IEnumerable types.
    /// </summary>
    /// <typeparam name="TValue">The type of elements in the array.</typeparam>
    /// <param name="current">The enumerable value to verify.</param>
    /// <returns>An instance of IEnumerableAssert for further assertions.</returns>
    public static dynamic AssertThat<TValue>(IEnumerable? current)
    {
        if (current == null)
            return new EnumerableAssert<object?>(null);

        // Get the actual element type from the enumerable
        var enumerableType = current.GetType();

        // Check if it implements IEnumerable<T>
        var genericEnumerable = enumerableType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        var elementType = genericEnumerable != null
            ? genericEnumerable.GetGenericArguments()[0]
            : typeof(object);

        var assertType = typeof(EnumerableAssert<>).MakeGenericType(elementType);
        var constructor = assertType.GetConstructor(
                              BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                              null,
                              [enumerableType],
                              null)
                          ?? assertType.GetConstructor(
                              BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                              null,
                              [elementType],
                              null)
                          ?? throw new InvalidOperationException($"No suitable constructor found for {assertType.Name}");
        return constructor.Invoke([current]);
    }

    /// <summary>
    ///     The dynamic assertions for all Godot Array types.
    /// </summary>
    /// <param name="current">The enumerable value to verify.</param>
    /// <returns>An instance of IEnumerableAssert for further assertions.</returns>
    public static IEnumerableAssert<Variant> AssertThat(Array? current)
        => new EnumerableAssert<Variant>(current);

    /// <summary>
    ///     The dynamic assertions for all Godot typed Array types.
    /// </summary>
    /// <typeparam name="TValue">The type of elements in the array.</typeparam>
    /// <param name="current">The enumerable value to verify.</param>
    /// <returns>An instance of IEnumerableAssert for further assertions.</returns>
    public static IEnumerableAssert<TValue?> AssertThat<[MustBeVariant] TValue>(Array<TValue>? current)
        => new EnumerableAssert<TValue?>(current);

    /// <summary>
    ///     An Assertion to verify array values.
    /// </summary>
    /// <typeparam name="TValue">The type of elements in the array.</typeparam>
    /// <param name="current">The current array value to verify.</param>
    /// <returns>An instance of IEnumerableAssert for further assertions.</returns>
    public static IEnumerableAssert<TValue?> AssertArray<TValue>(IEnumerable<TValue?>? current)
        => new EnumerableAssert<TValue?>(current);

    /// <summary>
    ///     An Assertion to verify array values.
    /// </summary>
    /// <param name="current">The current array value to verify.</param>
    /// <returns>An instance of IEnumerableAssert for further assertions.</returns>
    public static IEnumerableAssert<object?> AssertArray(IEnumerable<object?>? current)
        => new EnumerableAssert<object?>(current);
}

// ###################### IVectorAssert ############################################################################################

/// <summary>
///     A collection of assertions on Vector types.
/// </summary>
public abstract partial class Assertions
{
    /// <summary>
    ///     An Assertion to verify Godot.Vector2 values.
    /// </summary>
    /// <param name="current">The vector value to verify.</param>
    /// <returns>An instance of IVectorAssert for further assertions.</returns>
    public static IVectorAssert<Vector2> AssertThat(Vector2 current)
        => new VectorAssert<Vector2>(current);

    /// <summary>
    ///     An Assertion to verify Godot.Vector2I values.
    /// </summary>
    /// <param name="current">The vector value to verify.</param>
    /// <returns>An instance of IVectorAssert for further assertions.</returns>
    public static IVectorAssert<Vector2I> AssertThat(Vector2I current)
        => new VectorAssert<Vector2I>(current);

    /// <summary>
    ///     An Assertion to verify Godot.Vector3 values.
    /// </summary>
    /// <param name="current">The vector value to verify.</param>
    /// <returns>An instance of IVectorAssert for further assertions.</returns>
    public static IVectorAssert<Vector3> AssertThat(Vector3 current)
        => new VectorAssert<Vector3>(current);

    /// <summary>
    ///     An Assertion to verify Godot.Vector3I values.
    /// </summary>
    /// <param name="current">The vector value to verify.</param>
    /// <returns>An instance of IVectorAssert for further assertions.</returns>
    public static IVectorAssert<Vector3I> AssertThat(Vector3I current)
        => new VectorAssert<Vector3I>(current);

    /// <summary>
    ///     An Assertion to verify Godot.Vector4 values.
    /// </summary>
    /// <param name="current">The vector value to verify.</param>
    /// <returns>An instance of IVectorAssert for further assertions.</returns>
    public static IVectorAssert<Vector4> AssertThat(Vector4 current)
        => new VectorAssert<Vector4>(current);

    /// <summary>
    ///     An Assertion to verify Godot.Vector4I values.
    /// </summary>
    /// <param name="current">The vector value to verify.</param>
    /// <returns>An instance of IVectorAssert for further assertions.</returns>
    public static IVectorAssert<Vector4I> AssertThat(Vector4I current)
        => new VectorAssert<Vector4I>(current);

    /// <summary>
    ///     An Assertion to verify System.Numerics.Vector2 values.
    /// </summary>
    /// <param name="current">The vector value to verify.</param>
    /// <returns>An instance of IVectorAssert for further assertions.</returns>
    public static IVectorAssert<System.Numerics.Vector2> AssertThat(System.Numerics.Vector2 current)
        => new VectorAssert<System.Numerics.Vector2>(current);

    /// <summary>
    ///     An Assertion to verify System.Numerics.Vector3 values.
    /// </summary>
    /// <param name="current">The vector value to verify.</param>
    /// <returns>An instance of IVectorAssert for further assertions.</returns>
    public static IVectorAssert<System.Numerics.Vector3> AssertThat(System.Numerics.Vector3 current)
        => new VectorAssert<System.Numerics.Vector3>(current);

    /// <summary>
    ///     An Assertion to verify System.Numerics.Vector4 values.
    /// </summary>
    /// <param name="current">The vector value to verify.</param>
    /// <returns>An instance of IVectorAssert for further assertions.</returns>
    public static IVectorAssert<System.Numerics.Vector4> AssertThat(System.Numerics.Vector4 current)
        => new VectorAssert<System.Numerics.Vector4>(current);

    /// <summary>
    ///     An Assertion to verify vector values.
    /// </summary>
    /// <typeparam name="TValue">The type of Godot vector.</typeparam>
    /// <param name="vector">The vector value to verify.</param>
    /// <returns>An instance of IVectorAssert for further assertions.</returns>
    public static IVectorAssert<TValue> AssertVector<TValue>(TValue vector)
        where TValue : IEquatable<TValue>
        => new VectorAssert<TValue>(vector);

    /// <summary>
    ///     An Assertion to verify Godot.Vector2 values.
    /// </summary>
    /// <param name="current">The current vector2 value to verify.</param>
    /// <returns>An instance of IVectorAssert for further assertions.</returns>
    [Obsolete("AssertVec2 is deprecated, please use AssertVector instead.")]
    public static IVectorAssert<Vector2> AssertVec2(Vector2 current)
        => AssertVector(current);

    /// <summary>
    ///     An Assertion to verify Godot.Vector3 values.
    /// </summary>
    /// <param name="current">The current vector3 value to verify.</param>
    /// <returns>An instance of IVectorAssert for further assertions.</returns>
    [Obsolete("AssertVec3 is deprecated, please use AssertVector instead.")]
    public static IVectorAssert<Vector3> AssertVec3(Vector3 current)
        => AssertVector(current);
}

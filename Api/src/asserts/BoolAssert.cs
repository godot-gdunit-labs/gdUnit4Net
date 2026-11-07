// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Asserts;

using Constraints;

/// <inheritdoc cref="IBoolAssert" />
public sealed class BoolAssert : AssertBase<bool, IBoolConstraint>, IBoolAssert
{
    internal BoolAssert(bool current)
        : base(current)
    {
    }

    /// <inheritdoc />
    public IBoolConstraint IsFalse()
    {
        if (true.Equals(Current))
            ThrowTestFailureReport(AssertFailures.IsFalse(), Current, false);
        return this;
    }

    /// <inheritdoc />
    public IBoolConstraint IsTrue()
    {
        if (!true.Equals(Current))
            ThrowTestFailureReport(AssertFailures.IsTrue(), Current, true);
        return this;
    }
}

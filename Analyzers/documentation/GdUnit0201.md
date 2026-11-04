# GdUnit0201

## Multiple TestCase attributes not allowed with DataPoint

| Id         | Category        | Severity | Enabled |
|------------|-----------------|----------|---------|
| GdUnit0201 | Attribute Usage | Error    | True    |

## Problem Description

Methods decorated with DataPoint attribute can only have one TestCase attribute. Multiple TestCase attributes on a method that uses DataPoint will result in
undefined behavior.

### Error example

```csharp
    [TestCase]
    [TestCase] // ❌ GdUnit0201: Method 'TestDataPointProperty' cannot have multiple TestCase attributes when DataPoint attribute is present
    [DataPoint(nameof(ArrayDataPointProperty))]
    public void TestDataPointProperty(int a, int b, int expected) 
    {
        AssertThat(a + b).IsEqual(expected);
    }
```

### How to fix

Use only one TestCase attribute with DataPoint:

```csharp
    [TestCase] // ✅ Correct: Single TestCase with DataPoint
    [DataPoint(nameof(ArrayDataPointProperty))]
    public void TestDataPointProperty(int a, int b, int expected) 
    {
        AssertThat(a + b).IsEqual(expected);
    }
```

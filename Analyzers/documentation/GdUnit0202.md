# GdUnit0202

## Test case names must be unique

| Id         | Category        | Severity | Enabled |
|------------|-----------------|----------|---------|
| GdUnit0202 | Attribute Usage | Error    | True    |

# Problem Description

Multiple TestCase attributes on a test method cannot specify the same TestName. Duplicate test names will skip the tests and cause inconclusive results.

### Error example

```csharp
    [TestCase(TestName = "A")]
    [TestCase(TestName = "A")] // ❌ GdUnit0202: Test method 'Example' has multiple TestCase attributes with the same TestName
    public void Example()
    {
    }
```

### How to fix

Specify a unique TestName for each TestCase attribute:

```csharp
    [TestCase(TestName = "A")]
    [TestCase(TestName = "B")] // ✅ Correct: Unique TestName for each TestCase
    public void Example()
    {
    }
```

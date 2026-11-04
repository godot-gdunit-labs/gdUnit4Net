# GdUnit0501

## Godot Runtime Required for Test Method

| Id         | Category        | Severity | Enabled |
|------------|-----------------|----------|---------|
| GdUnit0501 | Attribute Usage | Error    | True    |

## Problem Description

Test methods that use Godot functionality (such as Node, Scene, or other Godot types)
must be annotated with `[RequireGodotRuntime]`. This ensures proper test execution in
the Godot engine environment.

Add `[RequireGodotRuntime]` to either test method or class level.

### Error example

```csharp
    [TestCase] // ❌ GdUnit0501: Test method 'TestWithGodot' uses Godot functionality but is not annotated with '[RequireGodotRuntime]'.
    public void TestWithGodot() 
    {
        var instance = new Node2D()
        AssertThat(instance).IsNotNull();
    }
```

### How to fix

Add `[RequireGodotRuntime]` to method or class level when testing Godot functionality:

```csharp
    [RequireGodotRuntime] // ✅ Correct: Method level annotation
    [TestCase]
    public void TestWithGodot() 
    {
        var instance = new Node2D();
        AssertThat(instance).IsNotNull();
    }

    // OR

    [RequireGodotRuntime] // ✅ Correct: Class level annotation
    public class MyTestClass
    {
        [TestCase]
        public void TestWithGodot() 
        {
            var instance = new Node2D();
            AssertThat(instance).IsNotNull();
        }
    }
```

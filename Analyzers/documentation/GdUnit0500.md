# GdUnit0500

## Godot Runtime Required for Test Class

| Id         | Category        | Severity | Enabled |
|------------|-----------------|----------|---------|
| GdUnit0500 | Attribute Usage | Error    | True    |

## Problem Description

Test classes with hooks (`[Before]`, `[After]`, `[BeforeTest]`, `[AfterTest]`) that use Godot functionality
must be annotated with `[RequireGodotRuntime]`. This ensures proper test execution in the
Godot engine environment.

Add `[RequireGodotRuntime]` to the test class level.

### Error example

```csharp
    [TestSuite]
    public class SceneRunnerTest  // ❌ GdUnit0500: Test class 'SceneRunnerTest' contains Godot dependencies in test hooks and requires [RequireGodotRuntime] attribute
    {
        [Before]
        public void Setup() => 
            Engine.PhysicsTicksPerSecond = 60;  // Using Godot Engine in test hook

        [TestCase]
        public void TestMethod() =>
            AssertThat(true).IsTrue();
    }
```

### How to fix

Add the `[RequireGodotRuntime]` attribute to the test class:

```csharp
    [RequireGodotRuntime]  // ✅ Correct: Uses [RequireGodotRuntime] for class with Godot dependencies in hooks
    [TestSuite]
    public class SceneRunnerTest
    {
        [Before]
        public void Setup() => 
            Engine.PhysicsTicksPerSecond = 60;

        [TestCase]
        public void TestMethod() =>
            AssertThat(true).IsTrue();
    }
```

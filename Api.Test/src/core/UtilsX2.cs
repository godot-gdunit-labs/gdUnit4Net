namespace GdUnit4.Tests.Core;

using Godot;

public static class UtilsX2
{
    public static string Foo() => string.Empty;

    public static string Bar() => ProjectSettings.GlobalizePath("res://src/core/resources/sources/TestPerson.cs");
}

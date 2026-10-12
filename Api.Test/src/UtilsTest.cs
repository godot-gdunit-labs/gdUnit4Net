namespace GdUnit4.Tests;

#if GDUNIT4NET_API_V5
using System.IO;
using Godot;
using static Assertions;
using static Utils;

[TestSuite]
public class UtilsTest
{
    [TestCase]
    public void CreateTempDirSuccess()
    {
        var tempDir = CreateTempDir("foo");
        AssertThat(tempDir).IsEqual(Path.Combine(GodotTempDir(), "foo"));
        AssertThat(Directory.Exists(tempDir)).IsTrue();

        tempDir = CreateTempDir("bar1\\test\\foo");
        AssertThat(tempDir).IsEqual(Path.Combine(GodotTempDir(), "bar1\\test\\foo"));
        AssertThat(Directory.Exists(tempDir)).IsTrue();

        tempDir = CreateTempDir("bar2/test/foo");
        AssertThat(tempDir).IsEqual(Path.Combine(GodotTempDir(), "bar2/test/foo"));
        AssertThat(Directory.Exists(tempDir)).IsTrue();
    }

    [TestCase]
    public void CreateTempDirAtTwice()
    {
        var tempDir = CreateTempDir("foo");

        // create again
        CreateTempDir("foo");

        AssertThat(tempDir).IsEqual(Path.Combine(GodotTempDir(), "foo"));
        AssertThat(Directory.Exists(tempDir)).IsTrue();
    }

    [TestCase]
    public void ClearTempDirSuccess()
    {
        var tempDir = CreateTempDir("foo");
        AssertThat(Directory.Exists(tempDir)).IsTrue();

        ClearTempDir();
        AssertThat(Directory.Exists(tempDir)).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void GodotErrorAsString()
    {
        AssertThat(ErrorAsString(Error.Bug)).IsEqual("Bug");
        AssertThat(ErrorAsString(47)).IsEqual("Bug");

        // with not existing error number
        AssertThat(ErrorAsString(100)).IsEqual("The error: 100 is not defined in Godot.");
    }
}
#endif

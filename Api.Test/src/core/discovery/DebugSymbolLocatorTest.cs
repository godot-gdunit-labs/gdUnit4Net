namespace GdUnit4.Tests.Core.Discovery;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using GdUnit4.Core.Discovery;

using static Assertions;
using static Utils;

[TestSuite]
public class DebugSymbolLocatorTest
{
    private const string SOURCE_FILE = "src/core/discovery/DebugSymbolLocatorTest.cs";

    [AfterTest]
    public void AfterEach()
        => ClearTempDir();

    // The expected source line numbers below refer to these methods, keep them in place.
    public static async IAsyncEnumerable<int> AsyncIterator()
    {
        await Task.Delay(10);
        yield return 1;
    }

    public static IEnumerable<int> Iterator()
    {
        yield return 1;
    }

    #region resolve the source location from debug symbols

    [TestCase]
    public void FindLocationOfAsyncIterator()
        => AssertThat(DebugSymbolLocator.FindStateMachineLocation(AsyncIterator().GetType()))
            .IsEqual(new DebugSymbolLocator.SourceLocation(DiscoverTestUtils.GetSourceFilePath(SOURCE_FILE), 24));

    [TestCase]
    public void FindLocationOfIterator()
        => AssertThat(DebugSymbolLocator.FindStateMachineLocation(Iterator().GetType()))
            .IsEqual(new DebugSymbolLocator.SourceLocation(DiscoverTestUtils.GetSourceFilePath(SOURCE_FILE), 30));

    [TestCase]
    public void FindLocationOfTypeWithoutStateMachine()
        => AssertThat(DebugSymbolLocator.FindStateMachineLocation(typeof(DebugSymbolLocatorTest)))
            .IsNull();

    #endregion

    #region fall back gracefully without readable debug symbols

    [TestCase]
    public void FindLocationWithCopiedDebugSymbols()
    {
        var stateMachineType = AsyncIterator().GetType();
        var assemblyPath = CopyAssembly(stateMachineType, "with-symbols");
        File.Copy(Path.ChangeExtension(stateMachineType.Assembly.Location, ".pdb"), Path.ChangeExtension(assemblyPath, ".pdb"));

        AssertThat(DebugSymbolLocator.FindStateMachineLocation(assemblyPath, stateMachineType.MetadataToken))
            .IsEqual(new DebugSymbolLocator.SourceLocation(DiscoverTestUtils.GetSourceFilePath(SOURCE_FILE), 24));
    }

    [TestCase]
    public void FindLocationOnRebuiltAssembly()
    {
        var stateMachineType = AsyncIterator().GetType();

        // a different module version id means the assembly file does not match the loaded assembly
        AssertThat(DebugSymbolLocator.FindStateMachineLocation(stateMachineType.Assembly.Location, stateMachineType.MetadataToken, Guid.NewGuid()))
            .IsNull();
        AssertThat(DebugSymbolLocator.FindStateMachineLocation(stateMachineType.Assembly.Location, stateMachineType.MetadataToken, stateMachineType.Module.ModuleVersionId))
            .IsEqual(new DebugSymbolLocator.SourceLocation(DiscoverTestUtils.GetSourceFilePath(SOURCE_FILE), 24));
    }

    [TestCase]
    public void FindLocationWithoutDebugSymbols()
    {
        var stateMachineType = AsyncIterator().GetType();
        var assemblyPath = CopyAssembly(stateMachineType, "without-symbols");

        AssertThat(DebugSymbolLocator.FindStateMachineLocation(assemblyPath, stateMachineType.MetadataToken))
            .IsNull();
    }

    [TestCase]
    public void FindLocationWithCorruptDebugSymbols()
    {
        var stateMachineType = AsyncIterator().GetType();
        var assemblyPath = CopyAssembly(stateMachineType, "corrupt-symbols");
        File.WriteAllText(Path.ChangeExtension(assemblyPath, ".pdb"), "not a valid symbol file");

        AssertThat(DebugSymbolLocator.FindStateMachineLocation(assemblyPath, stateMachineType.MetadataToken))
            .IsNull();
    }

    [TestCase]
    public void FindLocationOnMissingAssembly()
    {
        AssertThat(DebugSymbolLocator.FindStateMachineLocation(Path.Combine(GodotTempDir(), "not-exists.dll"), 0))
            .IsNull();
        AssertThat(DebugSymbolLocator.FindStateMachineLocation(string.Empty, 0))
            .IsNull();
    }

    [TestCase]
    public void FindLocationOnInvalidAssembly()
    {
        var assemblyPath = Path.Combine(CreateTempDir("invalid-assembly"), "Invalid.dll");
        File.WriteAllText(assemblyPath, "not a valid assembly");

        AssertThat(DebugSymbolLocator.FindStateMachineLocation(assemblyPath, AsyncIterator().GetType().MetadataToken))
            .IsNull();
    }

    #endregion

    private static string CopyAssembly(Type type, string tempDirName)
    {
        var assemblyPath = Path.Combine(CreateTempDir(tempDirName), Path.GetFileName(type.Assembly.Location));
        File.Copy(type.Assembly.Location, assemblyPath);
        return assemblyPath;
    }
}

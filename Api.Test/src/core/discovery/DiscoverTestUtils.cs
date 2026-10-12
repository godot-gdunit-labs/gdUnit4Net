namespace GdUnit4.Tests.Core.Discovery;

using System;
using System.IO;
using System.Linq;
using System.Reflection;

using Mono.Cecil;

internal static class DiscoverTestUtils
{
    internal static string GetSourceFilePath(string relativeSourcePath)
    {
        // Get the directory of the executing assembly
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var projectDir = Path.GetDirectoryName(assemblyLocation)!;

        // Navigate up to find the test file
        // Note: Adjust the path based on your project structure
        while (Directory.GetFiles(projectDir, "*.csproj").Length == 0 && Directory.GetParent(projectDir) != null)
            projectDir = Directory.GetParent(projectDir)!.FullName;

        // Find the test file in the project directory
        var sourceFile = Path.Combine(projectDir.Replace('\\', Path.DirectorySeparatorChar), relativeSourcePath.Replace('/', Path.DirectorySeparatorChar));
        return Path.GetFullPath(sourceFile);
    }

    internal static MethodDefinition FindMethodDefinition(AssemblyDefinition assemblyDefinition, Type clazzType, string methodName)
    {
        var methodInfo = clazzType.GetMethod(methodName)!;
        var typeDefinition = assemblyDefinition.MainModule.Types
            .FirstOrDefault(t => t.FullName == methodInfo.DeclaringType?.FullName)!;

        return typeDefinition.Methods
            .First(m => m.Name == methodInfo.Name);
    }
}

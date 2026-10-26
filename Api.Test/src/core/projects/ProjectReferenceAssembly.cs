namespace GdUnit4.Tests.Core.Projects;

using System;
using System.Reflection;

using GdUnit4.Core.Projects;

using Mono.Cecil;

/// <summary>
///     Writes an assembly carrying the metadata the build writes for each referenced Godot project.
/// </summary>
internal static class ProjectReferenceAssembly
{
    internal static void Write(string assemblyPath, params string[] projectPaths)
        => Write(assemblyPath, false, projectPaths);

    // Adds an attribute whose type lives in an assembly that does not exist, reading the attributes then fails.
    internal static void WriteWithUnresolvableAttribute(string assemblyPath, params string[] projectPaths)
        => Write(assemblyPath, true, projectPaths);

    private static void Write(string assemblyPath, bool unresolvableAttribute, string[] projectPaths)
    {
        var name = new AssemblyNameDefinition("ProjectReferenceAssembly", new Version(1, 0));
        using var assembly = AssemblyDefinition.CreateAssembly(name, name.Name, ModuleKind.Dll);
        var module = assembly.MainModule;
        if (unresolvableAttribute)
        {
            var missingAssembly = new AssemblyNameReference("MissingAssembly", new Version(1, 0));
            module.AssemblyReferences.Add(missingAssembly);
            var attributeType = new TypeReference("Missing", "MissingAttribute", module, missingAssembly);
            assembly.CustomAttributes.Add(new CustomAttribute(new MethodReference(".ctor", module.TypeSystem.Void, attributeType) { HasThis = true }));
        }

        var constructor = module.ImportReference(typeof(AssemblyMetadataAttribute).GetConstructor([typeof(string), typeof(string)]));

        foreach (var projectPath in projectPaths)
            assembly.CustomAttributes.Add(Metadata(GodotProjectReference.METADATA_KEY, projectPath));
        assembly.CustomAttributes.Add(Metadata("OtherKey", "../Other"));
        assembly.Write(assemblyPath);

        CustomAttribute Metadata(string key, string value)
        {
            var attribute = new CustomAttribute(constructor);
            attribute.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, key));
            attribute.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, value));
            return attribute;
        }
    }
}

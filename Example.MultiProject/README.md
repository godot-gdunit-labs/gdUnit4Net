# Multi-Project Example

The game and its tests live in two Godot projects. The test project references the game project, and gdUnit4
makes the game available at test runtime, without copying files or creating symlinks.

| Project | Content |
| --- | --- |
| `Game/` | The game: scenes, resources, scripts, input map, autoload, project settings, audio bus layout |
| `Game.Test/` | The tests, with their own scenes and resources that use game scenes |

## Setup

The test project references the game project:

```xml
<ItemGroup>
    <ProjectReference Include="../Game/Game.csproj" />
</ItemGroup>
```

Nothing else is needed. The `gdUnit4.api` package detects every referenced project that contains a
`project.godot`. At test runtime the test runner:

- imports the assets of the game, only when they changed since the last run
- mounts the game files, so game scenes and resources load by `res://` and `uid://` paths
- registers the game scripts, so game scenes get their C# classes
- applies the game input map, autoloads, project settings and audio bus layout

Game code can be debugged with breakpoints like test code.

To turn it off, set `<GdUnit4MountGodotProjectReferences>false</GdUnit4MountGodotProjectReferences>` in the test project.

## Rules

- Game and test files must not share a path: the game files are mounted into the `res://` of the test project, and a
  test file with the same path hides the game file. The test runner reports such files as a warning.
- Settings that Godot reads only at engine start, like the rendering driver or the physics engine, come from the test
  project.

## Run

Build the local gdUnit4 packages from the repository root, then set `GODOT_BIN` in
`Example.MultiProject/Game.Test/.runsettings` to the Godot Mono binary of your machine:

```bash
dotnet build Analyzers/GdUnit4Analyzers.csproj --configuration Release
dotnet build Api/GdUnit4Api.csproj --configuration Release
dotnet build TestAdapter/GdUnit4TestAdapter.csproj --configuration Release
cd Example.MultiProject/Game.Test
dotnet test
```

This example references the gdUnit4 sources of this repository. A project of your own references the
`gdUnit4.api` and `gdUnit4.test.adapter` packages instead.

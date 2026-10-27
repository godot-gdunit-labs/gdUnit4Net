# Test a Godot game from a separate project

Multi-project support is available in `gdUnit4.api` `5.1.0-rc7` and later. It lets a Godot C# test project reference a separate game project while keeping both
projects' files and settings under their own project roots.

## Project structure

Keep a `project.godot` and a Godot C# project in both the game and test directories. The test project owns its test suites, test scenes, and test resources,
while the game project owns the scenes, resources, scripts, and settings used by the game.

| Project | Contents |
| --- | --- |
| `Game/` | The game project, including `project.godot`, `Game.csproj`, resources, scenes, and scripts. |
| `Game.Test/` | The test project, including `project.godot`, `Game.Test.csproj`, test suites, and test-owned scenes and resources. |

The complete working sample is in [`Example.MultiProject/`](https://github.com/godot-gdunit-labs/gdUnit4Net/tree/master/Example.MultiProject).

## Configure the test project

Install and configure the API and VSTest adapter as described in the [API setup guide](../README.md#installation). Then add a project reference to the game project
in the test project's C# project file:

```xml
<ItemGroup>
  <ProjectReference Include="../Game/Game.csproj" />
</ItemGroup>
```

No file copying or symlinking is needed. The API package detects referenced projects that contain a `project.godot` file and prepares them before a test run.

## Runtime behavior

For tests that require Godot, GdUnit4Net imports the referenced project's assets when needed, mounts its files into the test project's `res://` namespace, and
registers its UIDs and C# scripts. It also applies the project's input map, autoloads, project settings, translations, and audio bus layout. Tests can then load
game scenes and resources by `res://` or `uid://` paths, and game code remains available to the debugger.

Inside a test suite marked `[RequireGodotRuntime]`, a test can load the game scene and simulate an input action:

```csharp
[TestCase]
public async Task GameSceneReactsOnInputAction()
{
    var runner = ISceneRunner.Load<Player>("res://Characters/Player.tscn", true);
    runner.SimulateActionPressed("jump");
    await runner.AwaitInputProcessed();
    AssertThat(runner.Scene().JumpCount).IsEqual(1);
}
```

This example is exercised by `GameSceneTest.GameSceneReactsOnInputAction` in the multi-project sample.

## Performance

Project preparation runs only for test suites marked `[RequireGodotRuntime]`; logic-only tests continue to run without starting Godot. For a Godot-backed run,
GdUnit4Net reads and hashes the referenced project's import inputs, packable files, and Godot executable to validate its caches. This content check reads the
files even when they have not changed, so its I/O cost grows with the size and number of referenced projects.

On the first run, or after relevant content changes, GdUnit4Net launches Godot in headless import mode and creates a resource pack. Unchanged imports and packs
are reused on later runs, but the pack is mounted each time Godot starts. Packs are stored under the test project's `.godot/gdunit4` directory without
compression, reducing pack-creation CPU cost at the expense of disk space and I/O.

## Rules and limitations

- The test project remains the runtime's root project. Settings marked by Godot as requiring restart keep the test project's startup values.
- A file in the test project hides a game file with the same `res://` path. GdUnit4Net reports these collisions as warnings.
- Godot loads GDExtensions at engine startup, so extensions from the game project are not loaded dynamically. Add required extensions to the test project.
- The test project must generate assembly attributes. `GenerateAssemblyInfo` is enabled by default; GdUnit4Net warns if it is disabled.
- To disable mounting referenced projects, set `<GdUnit4MountGodotProjectReferences>false</GdUnit4MountGodotProjectReferences>` in the test project.

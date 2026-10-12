namespace GdUnit4.Tests.Core;

using System.Threading.Tasks;

using Godot;

using static Assertions;

[RequireGodotRuntime]
[TestSuite]
public sealed class SceneRunnerLiveCycleTest
{
#nullable disable
    private ISceneRunner sceneRunner;
#nullable enable

    [Before]
    public void Setup() => AssertThat(sceneRunner).IsNull();

    [After]
    public void TearDown()
    {
        AssertThat(sceneRunner).IsNotNull();
        AssertThat(sceneRunner.Scene()).IsNull();
    }

    [BeforeTest]
    public void BeforeTest()
    {
        sceneRunner = ISceneRunner.Load("res://src/core/resources/scenes/TestSceneCSharp.tscn", true);
        AssertThat(sceneRunner).IsNotNull();
        AssertThat(sceneRunner.Scene()).IsNotNull();
    }

    [AfterTest]
    public void AfterTest()
    {
        AssertThat(sceneRunner).IsNotNull();
        AssertThat(sceneRunner.Scene()).IsNotNull();
    }

    [TestCase]
    public void LoadAdditionalScene()
    {
        using var runner = ISceneRunner.Load("uid://cn8ucy2rheu0f", true);
        AssertThat(runner.Scene())
            .IsInstanceOf<Node2D>()
            .IsNotNull();

        // verify scene is still valid
        AssertThat(sceneRunner.Scene()).IsNotNull();

        // verify it fails when try to load a scene using null argument
#pragma warning disable CS8625, CS8600 // Converting null literal or possible null value to non-nullable type.
        AssertThrown(() => ISceneRunner.Load((Node)null, true))
            .HasMessage("SceneRunner requires a valid scene instance, but received null");
#pragma warning restore CS8625, CS8600 // Converting null literal or possible null value to non-nullable type.
    }

    [TestCase]
    public async Task DisposeSceneRunner()
    {
        var runner = ISceneRunner.Load("res://src/core/resources/scenes/TestSceneCSharp.tscn", true);
        var tree = (SceneTree)Engine.GetMainLoop();

        var currentScene = runner.Scene();
        var nodePath = currentScene?.GetPath();

        // check scene is loaded and added to the root node
        AssertThat(GodotObject.IsInstanceValid(currentScene)).IsTrue();
        AssertThat(tree.Root.GetNodeOrNull(nodePath)).IsNotNull();

        await ISceneRunner.SyncProcessFrame;
        runner.Dispose();

        await ISceneRunner.SyncProcessFrame;

        // check scene is freed and removed from the root node
        AssertThat(GodotObject.IsInstanceValid(currentScene)).IsFalse();
        AssertThat(tree.Root.GetNodeOrNull(nodePath)).IsNull();
        AssertThat(runner.Scene()).IsNull();
    }
}

namespace GdUnit4.Tests.Resources;

using System;

using Godot;

public partial class TestSceneWithExceptionTest : Control
{
    private int frameCount;

    public void SomeMethodThatThrowsException()
    {
        Console.WriteLine("Throw a test exception");
        throw new InvalidOperationException("Test Exception");
    }

    public override void _Process(double delta)
    {
        frameCount++;

        // we throw an example exception
        if (frameCount == 10)
            throw new InvalidProgramException("Exception during scene processing");
        try
        {
            if (frameCount == 5)
                throw new InvalidProgramException("Exception during scene processing inside a catch block");
        }
        catch (InvalidProgramException)
        {
            // ignore
        }
    }
}

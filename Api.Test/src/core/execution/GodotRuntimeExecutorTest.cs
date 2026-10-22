namespace GdUnit4.Tests.Core.Execution;

using System;
using System.Buffers.Binary;
using System.IO.Pipes;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using GdUnit4.Api;
using GdUnit4.Core.Commands;
using GdUnit4.Core.Execution;
using GdUnit4.Core.Runners;

using static Assertions;

[TestSuite]
public class GodotRuntimeExecutorTest
{
    [TestCase(Timeout = 5000)]
    public async Task ExecuteCommandReturnsResponseOfServer()
    {
        await using var server = new PipeServer();
        await using var executor = await server.ConnectExecutor();

        var execution = executor.ExecuteCommand(new TerminateGodotInstanceCommand(), new NoInteractTestEventListener(), CancellationToken.None);
        await server.ReadMessage();
        await server.WriteMessage(PipeMessageSerializer.Serialize(new Response { StatusCode = HttpStatusCode.OK, Payload = "done" }));
        var response = await execution;

        AssertThat(response.StatusCode).IsEqual(HttpStatusCode.OK);
        AssertThat(response.Payload).IsEqual("done");
    }

    [TestCase(Timeout = 5000)]
    public async Task StartWithoutServerLogsPipeName()
    {
        using var capture = LogCapture.Watch<GodotRuntimeExecutor>();
        var pipeName = PipeServer.NewPipeName();
        await using var executor = new GodotRuntimeExecutor(pipeName, 100);

        var exception = await AssertThrown(executor.StartAsync());

        exception!.IsInstanceOf<TimeoutException>();
        AssertThat(capture.EntriesOf(LogLevel.Error))
            .Extract("Message")
            .ContainsExactly($"Starting GodotRuntimeExecutor failed, connecting to pipe '{pipeName}'.\n\t The operation has timed out.");
    }

    [TestCase(Timeout = 5000)]
    public async Task ExecuteCommandWithoutConnectionReturnsError()
    {
        await using var executor = new GodotRuntimeExecutor(PipeServer.NewPipeName(), 100);

        var response = await executor.ExecuteCommand(new TerminateGodotInstanceCommand(), new NoInteractTestEventListener(), CancellationToken.None);

        AssertThat(response.StatusCode).IsEqual(HttpStatusCode.InternalServerError);
        AssertString(ExceptionPayload.Deserialize(response.Payload)!.Message)
            .StartsWith("System.InvalidOperationException: Client is not connected");
    }

    [TestCase(Timeout = 5000)]
    public async Task ExecuteCommandReturnsErrorOnInvalidMessage()
    {
        await using var server = new PipeServer();
        await using var executor = await server.ConnectExecutor();

        var execution = executor.ExecuteCommand(new TerminateGodotInstanceCommand(), new NoInteractTestEventListener(), CancellationToken.None);
        await server.ReadMessage();
        await server.WriteMessage(/*lang=json,strict*/ """{"Unknown":1}""");
        var response = await execution;

        AssertThat(response.StatusCode).IsEqual(HttpStatusCode.InternalServerError);
        AssertString(ExceptionPayload.Deserialize(response.Payload)!.Message)
            .StartsWith("System.Text.Json.JsonException: Invalid pipe message, the message type or the data is missing.");
    }

    [TestCase(Timeout = 5000)]
    public async Task ExecuteCommandPropagatesExceptionOfEventListener()
    {
        await using var server = new PipeServer();
        await using var executor = await server.ConnectExecutor();

        var execution = executor.ExecuteCommand(new TerminateGodotInstanceCommand(), new FailingTestEventListener(), CancellationToken.None);
        await server.ReadMessage();
        await server.WriteMessage(PipeMessageSerializer.Serialize(TestEvent.BeforeTest(Guid.NewGuid(), "res://ExampleTest.cs", "ExampleTest", "TestFoo")));

        (await AssertThrown(execution))!
            .IsInstanceOf<ArgumentException>()
            .HasMessage("The event listener fails.");
    }

    private sealed class FailingTestEventListener : ITestEventListener
    {
        public bool IsFailed { get; set; }

        public int CompletedTests { get; set; }

        public void PublishEvent(ITestEvent testEvent) => throw new ArgumentException("The event listener fails.");
    }

    private sealed class PipeServer : IAsyncDisposable
    {
        private readonly string pipeName = NewPipeName();
        private readonly NamedPipeServerStream stream;

        public PipeServer()
            => stream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

        public static string NewPipeName() => $"gdunit4-executor-test-{Guid.NewGuid():N}";

        public async Task<GodotRuntimeExecutor> ConnectExecutor()
        {
            var executor = new GodotRuntimeExecutor(pipeName, 2000);
            await Task.WhenAll(stream.WaitForConnectionAsync(), executor.StartAsync());
            return executor;
        }

        public async Task ReadMessage()
        {
            var lengthBytes = new byte[4];
            await stream.ReadExactlyAsync(lengthBytes);
            await stream.ReadExactlyAsync(new byte[BinaryPrimitives.ReadInt32LittleEndian(lengthBytes)]);
        }

        public async Task WriteMessage(string json)
        {
            var messageBytes = Encoding.UTF8.GetBytes(json);
            var lengthBytes = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(lengthBytes, messageBytes.Length);
            await stream.WriteAsync(lengthBytes);
            await stream.WriteAsync(messageBytes);
            await stream.FlushAsync();
        }

        public async ValueTask DisposeAsync() => await stream.DisposeAsync();
    }
}

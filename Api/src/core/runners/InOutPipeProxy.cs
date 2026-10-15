// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Runners;

using System.Buffers.Binary;
using System.IO.Pipes;
using System.Net;
using System.Text;
using System.Text.Json;

using Api;

using Commands;

internal class InOutPipeProxy<TPipe> : IAsyncDisposable
    where TPipe : PipeStream
{
    protected InOutPipeProxy(TPipe pipe, ITestEngineLogger logger)
    {
        Logger = logger;
        Pipe = pipe;
    }

    protected ITestEngineLogger Logger { get; }

    protected bool IsConnected => Pipe.IsConnected;

    protected TPipe Proxy => Pipe;

    private TPipe Pipe { get; }

    public async ValueTask DisposeAsync()
    {
        if (IsConnected)
            Pipe.Close();
        await Pipe.DisposeAsync()
            .ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    protected async Task<object?> ReadInData(CancellationToken cancellationToken)
    {
        var responseLengthBytes = new byte[4];
        await ReadExactBytesAsync(responseLengthBytes, 0, 4, cancellationToken)
            .ConfigureAwait(false);
        if (cancellationToken.IsCancellationRequested)
        {
            return new Response
            {
                StatusCode = HttpStatusCode.Gone,
                Payload = "Connection interrupted by cancellation requested."
            };
        }

        var responseLength = BinaryPrimitives.ReadInt32LittleEndian(responseLengthBytes);
        var responseBytes = new byte[responseLength];
        await ReadExactBytesAsync(responseBytes, 0, responseLength, cancellationToken)
            .ConfigureAwait(false);
        if (cancellationToken.IsCancellationRequested)
        {
            return new Response
            {
                StatusCode = HttpStatusCode.Gone,
                Payload = "Connection interrupted by cancellation requested."
            };
        }

        var json = Encoding.UTF8.GetString(responseBytes);
        if (json.Length == 0)
            return null;

        return PipeMessageSerializer.Deserialize(json);
    }

    protected async Task WriteResponse(Response response) => await WriteAsync(response)
        .ConfigureAwait(false);

    protected async Task<TCommand> ReadCommand<TCommand>(CancellationToken cancellationToken)
        where TCommand : BaseCommand
    {
        var responseLengthBytes = new byte[4];
        await ReadExactBytesAsync(responseLengthBytes, 0, 4, cancellationToken)
            .ConfigureAwait(false);
        var responseLength = BinaryPrimitives.ReadInt32LittleEndian(responseLengthBytes);

        if (!IsConnected)
            throw new IOException("Client not connected");

        var responseBytes = new byte[responseLength];
        await ReadExactBytesAsync(responseBytes, 0, responseLength, cancellationToken)
            .ConfigureAwait(false);
        if (!IsConnected)
            throw new IOException("Client not connected");

        var json = Encoding.UTF8.GetString(responseBytes);
        return PipeMessageSerializer.Deserialize<TCommand>(json);
    }

    protected async Task WriteCommand<TCommand>(TCommand command)
        where TCommand : BaseCommand
        => await WriteAsync(command)
            .ConfigureAwait(false);

    protected async Task WriteAsync<TData>(TData data)
        where TData : notnull
    {
        var json = SerializeObject(data);
        var messageBytes = Encoding.UTF8.GetBytes(json);

        // Write message length (4 bytes) followed by the message
        var lengthBytes = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(lengthBytes, messageBytes.Length);

        await Pipe
            .WriteAsync(lengthBytes)
            .ConfigureAwait(false);
        await Pipe
            .WriteAsync(messageBytes)
            .ConfigureAwait(false);
        await Pipe
            .FlushAsync()
            .ConfigureAwait(false);
    }

    private static string SerializeObject<TObject>(TObject data)
        where TObject : notnull
    {
        try
        {
            return PipeMessageSerializer.Serialize(data);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            var response = new Response
            {
                StatusCode = HttpStatusCode.BadRequest,
                Payload = $"Invalid command format: {ex.Message}"
            };
            return PipeMessageSerializer.Serialize(response);
        }
    }

    private async Task ReadExactBytesAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var totalBytesRead = 0;
        while (IsConnected && totalBytesRead < count)
        {
            try
            {
                var bytesRead = await Pipe
                    .ReadAsync(buffer.AsMemory(offset + totalBytesRead, count - totalBytesRead), cancellationToken)
                    .ConfigureAwait(false);
                totalBytesRead += bytesRead;
            }
            catch (OperationCanceledException)
            {
                if (!cancellationToken.IsCancellationRequested)
                    throw;
                break;
            }
        }

        // Console.WriteLine($"{typeof(TPipe)} Read {count} bytes from {totalBytesRead} of {count}, {IsConnected}");
    }
}

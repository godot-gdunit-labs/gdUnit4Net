// Copyright (c) 2026 Mike Schulze
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
        if (!await ReadExactBytesAsync(responseLengthBytes, 0, 4, cancellationToken).ConfigureAwait(false))
            return ConnectionGone(cancellationToken);

        var responseLength = BinaryPrimitives.ReadInt32LittleEndian(responseLengthBytes);
        var responseBytes = new byte[responseLength];
        if (!await ReadExactBytesAsync(responseBytes, 0, responseLength, cancellationToken).ConfigureAwait(false))
            return ConnectionGone(cancellationToken);

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
        if (!await ReadExactBytesAsync(responseLengthBytes, 0, 4, cancellationToken).ConfigureAwait(false))
            throw ConnectionLost(cancellationToken);

        var responseLength = BinaryPrimitives.ReadInt32LittleEndian(responseLengthBytes);
        var responseBytes = new byte[responseLength];
        if (!await ReadExactBytesAsync(responseBytes, 0, responseLength, cancellationToken).ConfigureAwait(false))
            throw ConnectionLost(cancellationToken);

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

    private static Response ConnectionGone(CancellationToken cancellationToken) => new()
    {
        StatusCode = HttpStatusCode.Gone,
        Payload = cancellationToken.IsCancellationRequested
            ? "Connection interrupted by cancellation requested."
            : "Connection closed by the other side."
    };

    private static Exception ConnectionLost(CancellationToken cancellationToken) => cancellationToken.IsCancellationRequested
        ? new OperationCanceledException(cancellationToken)
        : new IOException("Client not connected");

    /// <summary>
    ///     Reads exactly <paramref name="count" /> bytes from the pipe.
    /// </summary>
    /// <returns>
    ///     True when every byte was read; false when the read was cancelled or the other side closed the pipe: a read of
    ///     0 byte, which a closed pipe returns forever on Unix, or the pipe disconnected, as on Windows.
    /// </returns>
    private async Task<bool> ReadExactBytesAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var totalBytesRead = 0;
        while (totalBytesRead < count)
        {
            if (!IsConnected)
                return false;
            try
            {
                var bytesRead = await Pipe
                    .ReadAsync(buffer.AsMemory(offset + totalBytesRead, count - totalBytesRead), cancellationToken)
                    .ConfigureAwait(false);
                if (bytesRead == 0)
                    return false;
                totalBytesRead += bytesRead;
            }
            catch (OperationCanceledException)
            {
                if (!cancellationToken.IsCancellationRequested)
                    throw;
                return false;
            }
        }

        return true;
    }
}

using System.Runtime.CompilerServices;

namespace KN.SafeCommunicationPlatform.Hubs;

public interface ISafeCommunicationClient
{
    Task ReceiveFile(IAsyncEnumerable<ReadOnlyMemory<byte>> byteStream);

    IAsyncEnumerable<ReadOnlyMemory<byte>> GetFileAsync(string file);
}

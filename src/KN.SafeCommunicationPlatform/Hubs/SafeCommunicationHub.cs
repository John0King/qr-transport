namespace KN.SafeCommunicationPlatform.Hubs;
public class SafeCommunicationHub:Hub<ISafeCommunicationClient>
{
    public override Task OnConnectedAsync()
    {
        return base.OnConnectedAsync();
    }

    public async Task ReceiveFile(IAsyncEnumerable<ReadOnlyMemory<byte>> fileStream)
    {
        await Task.Delay(10);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        return base.OnDisconnectedAsync(exception);
    }
}
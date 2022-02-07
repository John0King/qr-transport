namespace KN.SafeCommunicationPlatform.Hubs
{
    public class QrHub:Hub
    {
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();

        }

        public async Task<string> GetChannelCode()
        {
            return "123";
        }
    }
}

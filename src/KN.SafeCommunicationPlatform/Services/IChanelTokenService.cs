namespace KN.SafeCommunicationPlatform.Services
{
    public interface IChanelTokenService
    {
        ValueTask<string> GetTokenAsync(string token);

        ValueTask<bool> UseTokenAsync(string token);
    }
}

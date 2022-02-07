using KN.SafeCommunicationPlatform.Protocols.Qr;

namespace KN.SafeCommunicationPlatform.Protocols
{
    public interface IQrConnectionBuilder
    {
        IQrConnectionBuilder WithQrHandler(IQrHandler qrHandler);
        QrConnection Build();
    }
}
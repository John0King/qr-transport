using KN.SafeCommunicationPlatform.Protocols.Qr;

namespace KN.SafeCommunicationPlatform.Protocols
{
    public interface IQrConnectionBuilder
    {
        IQrConnectionBuilder WithQrReader(IQrReader qrReader);
        IQrConnectionBuilder WithQrWriter(IQrWriter qrWriter);
        QrConnection Build();
    }
}
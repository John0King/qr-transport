namespace KN.SafeCommunicationPlatform.Protocols.AppTransportLayer
{
    public enum FrameOp:byte
    {
        New = 0x10,
        Continue = 0x11,
        Final = 0xEF,
    }
}

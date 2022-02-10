namespace KN.SafeCommunicationPlatform.Protocols
{
    /// <summary>
    /// <see cref="QrConnection"/> 的接收结果
    /// </summary>
    public readonly struct QrReceiveResult
    {
        public bool EndOfMessage { get; }

        public int Count { get; }

        public MessageType MessageType { get; }

        public QrReceiveResult(int count, MessageType messageType, bool endOfMessage)
        {
            Count = count;
            EndOfMessage = endOfMessage;
            MessageType = messageType;
        }
    }

    public enum MessageType
    {
        Binary,
        Close,
    }
}

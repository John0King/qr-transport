namespace KN.SafeCommunicationPlatform.Protocols.QrSockets
{
    /// <summary>
    /// QrSocket 接收结果。
    /// </summary>
    public readonly struct ReceiveResult
    {
        public int Count { get; }

        public bool EndOfMessage { get; }

        public MessageType MessageType { get; }

        public ReceiveResult(int count, MessageType messageType, bool endOfMessage)
        {
            Count = count;
            MessageType = messageType;
            EndOfMessage = endOfMessage;
        }

        internal static ReceiveResult From(QrReceiveResult result)
        {
            return new ReceiveResult(result.Count, result.MessageType, result.EndOfMessage);
        }
    }

    [Obsolete("Use ReceiveResult instead.")]
    public readonly struct ReciveResult
    {
        public int Count { get; }
        public bool EndOfMessage { get; }
        public MessageType MessageType { get; }

        public ReciveResult(int count, MessageType messageType, bool endOfMessage)
        {
            Count = count;
            MessageType = messageType;
            EndOfMessage = endOfMessage;
        }

        public static implicit operator ReciveResult(ReceiveResult result)
        {
            return new ReciveResult(result.Count, result.MessageType, result.EndOfMessage);
        }
    }
}
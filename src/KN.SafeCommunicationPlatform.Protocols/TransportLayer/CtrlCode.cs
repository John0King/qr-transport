namespace KN.SafeCommunicationPlatform.Protocols.TransportLayer;

public enum CtrlCode : ushort
{
    /// <summary>
    /// 主控连接
    /// </summary>
    Connect= 0x00,

    /// <summary>
    /// 回应主控连接
    /// </summary>
    AckConnect=0x01,

    /// <summary>
    /// 主控准备要发送数据
    /// </summary>
    Send = 0x10,

    SendContinue = 0x11,

    SendFin = 0x12,

    /// <summary>
    /// 回应主控知道他要发送数据
    /// </summary>
    AckSend = 0x11,


    /// <summary>
    /// 主控要求拉取你发送的数据
    /// </summary>
    Pull = 0x20,


    
    Ping = 0xF0,
    Pong = 0xF1,

    Close = 0xFA,
}

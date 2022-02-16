using KN.SafeCommunicationPlatform.Protocols.Qr;
using KN.SafeCommunicationPlatform.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KN.SafeCommunicationPlatform.Wpf.Qr
{
    /// <summary>
    /// Usb扫码器读取器
    /// </summary>
    public class UsbQrGunReader : IQrReader
    {
        private readonly Vbarapi _vbarApi = new Vbarapi();
        private CancellationTokenSource? CancellationTokenSource;
        public void Dispose()
        {
            this.StopRead();
        }

        public event Action<byte[]>? Readed;

        public async IAsyncEnumerable<Memory<byte>> GetQrStream([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested || CancellationTokenSource?.IsCancellationRequested == true)
                {
                   break;
                }
                if (_vbarApi.GetResultStr(out var result, out var size))
                {
                    if (result.Length > 0)
                    {
                        Readed?.Invoke(result);
                        yield return new Memory<byte>(result);
                    }

                }
                await Task.Delay(2);
            }
            StopRead();

        }

        public void StartRead()
        {
            CancellationTokenSource = new CancellationTokenSource();
            _vbarApi.OpenDevice();
            _vbarApi.ControlScan(true);
        }

        public void StopRead()
        {
            CancellationTokenSource?.Cancel();
            _vbarApi.ControlScan(false);
            _vbarApi.CloseDevice();
        }
    }
}

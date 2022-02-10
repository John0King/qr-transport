using KN.SafeCommunicationPlatform.Protocols.Qr;
using KN.SafeCommunicationPlatform.Wpf.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KN.SafeCommunicationPlatform.Wpf.Qr
{
    public class UsbQrGunReader : IQrReader
    {
        private readonly Vbarapi _vbarApi = new Vbarapi();
        private CancellationTokenSource? CancellationTokenSource;
        public void Dispose()
        {
            this.StopRead();
        }

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
                    yield return new Memory<byte>(result);
                }
                await Task.Delay(10);
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

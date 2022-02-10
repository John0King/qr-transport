using KN.SafeCommunicationPlatform.Protocols.Qr;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXing.SkiaSharp;
namespace KN.SafeCommunicationPlatform.Wpf.Qr
{
    public class SkiaQrWriter : IQrWriter
    {
        private readonly Action<SKBitmap> _exportor;
        private readonly BarcodeWriter _qrWriter;
        private readonly Encoding _encoding;

        public SkiaQrWriter(Action<SKBitmap> exportor)
        {
            _exportor = exportor;
            _qrWriter = new BarcodeWriter()
            {
                Format = ZXing.BarcodeFormat.QR_CODE,
                Options = new ZXing.QrCode.QrCodeEncodingOptions { 
                    Width = 300, 
                    Height = 300, 
                    CharacterSet = "ISO-8859-15",
                    ErrorCorrection = ZXing.QrCode.Internal.ErrorCorrectionLevel.L,
                    Margin = 1
                },
            };
            _encoding = Encoding.GetEncoding("ISO-8859-15");
        }
        public void WriteQrData(ReadOnlySpan<byte> qrData)
        {
            var str = _encoding.GetString(qrData);
            var bitmap = _qrWriter.Write(str);
            _exportor(bitmap);
        }
    }
}

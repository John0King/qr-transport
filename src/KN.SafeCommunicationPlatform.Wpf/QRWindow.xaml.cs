using CommunityToolkit.Mvvm.ComponentModel;
using KN.SafeCommunicationPlatform.Protocols;
using KN.SafeCommunicationPlatform.Wpf.Qr;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KN.SafeCommunicationPlatform.Wpf
{
    /// <summary>
    /// QRWindow.xaml 的交互逻辑
    /// </summary>
    public partial class QRWindow : Window
    {
        private readonly SkiaQrWriter _qrWriter;
        private readonly UsbQrGunReader _qrGunReader = new UsbQrGunReader();
        private QrConnection? qrConnection;
        public QRWindow()
        {
            this.DataContext = this;
            InitializeComponent();

            _qrWriter = new SkiaQrWriter((map) => this.QrBitmap = map);
            IsCanConnect = true;
        }

        public static DependencyProperty QrBitmapProperty = DependencyProperty.Register(nameof(QrBitmap), typeof(SKBitmap), typeof(QRWindow));

        public bool IsCanConnect
        {
            get { return (bool)GetValue(IsCanConnectProperty); }
            set { SetValue(IsCanConnectProperty, value); SetValue(IsCanDisConnectProperty, !value); }
        }

        public bool IsCanDisConnect
        {
            get
            {
                return (bool)GetValue(IsCanDisConnectProperty);
            }
            set => SetValue(IsCanDisConnectProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsConnected.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsCanConnectProperty =
            DependencyProperty.Register(nameof(IsCanConnect), typeof(bool), typeof(QRWindow), new PropertyMetadata(true));

        public static readonly DependencyProperty IsCanDisConnectProperty =
           DependencyProperty.Register(nameof(IsCanDisConnect), typeof(bool), typeof(QRWindow), new PropertyMetadata(false));

        public SKBitmap QrBitmap 
        { 
            get
            {
                return (SKBitmap)GetValue(QrBitmapProperty);
            }
            set
            {
                SetValue(QrBitmapProperty, value);
                SkViewer.InvalidateVisual();
            }
        }

        private void SKElement_PaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
        {
            if (QrBitmap != null)
            {
                e.Surface.Canvas.DrawBitmap(QrBitmap, e.Info.Rect);
            }
            else
            {
                e.Surface.Canvas.DrawColor(SKColor.Parse("#cccccc"));
            }
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            qrConnection?.Dispose();
            qrConnection = QrConnection.CreateBuilder()
                .WithQrWriter(_qrWriter)
                .WithQrReader(_qrGunReader)
                .Build();
            await qrConnection.ConnectAsync();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var x = new byte[24];
            System.Security.Cryptography.RandomNumberGenerator.Fill(x.AsSpan());
            obxt.Text = Convert.ToBase64String(x);
            _qrWriter.WriteQrData(x);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var reader = new ZXing.SkiaSharp.BarcodeReader()
            {
                Options = new ZXing.Common.DecodingOptions
                {
                    CharacterSet = "ISO-8859-15"
                }
            };
            var result = reader.Decode(this.QrBitmap);
            this.txt.Text = result.Text;
            this.bxt.Text = Convert.ToBase64String(((List<byte[]>)result.ResultMetadata[ZXing.ResultMetadataType.BYTE_SEGMENTS])
                .SelectMany(x=>x).ToArray());
        }
    }
}

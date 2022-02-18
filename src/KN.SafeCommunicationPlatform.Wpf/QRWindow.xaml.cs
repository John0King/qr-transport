using CommunityToolkit.Mvvm.ComponentModel;
using KN.SafeCommunicationPlatform.EF;
using KN.SafeCommunicationPlatform.Protocols;
using KN.SafeCommunicationPlatform.Protocols.TransportLayer;
using KN.SafeCommunicationPlatform.Wpf.Qr;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using SkiaSharp;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipelines;
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
        private readonly UsbQrGunReader _qrGunReader;
        //private QrConnection? qrConnection;
        private EventBaseTransportHandler? connection;
        public QRWindow()
        {
            this.DataContext = this;
            InitializeComponent();

            _qrWriter = new SkiaQrWriter((map) => Dispatcher.Invoke(()=> this.QrBitmap = map));
            _qrGunReader = new UsbQrGunReader();
            _qrGunReader.Readed += OnQrRead;
            IsCanConnect = true;
            Loaded += Init;
        }

        private void Init(object? sender, EventArgs e)
        {
            try
            {
                //qrConnection?.Dispose();
                //qrConnection = QrConnection.CreateBuilder()
                //    .WithQrWriter(_qrWriter)
                //    .WithQrReader(_qrGunReader)
                //    .Build();
                //await qrConnection.ConnectAsync();
                using (var db = CreateDbContext())
                {
                    db.Database.EnsureCreated();
                }


                this.connection = new EventBaseTransportHandler(_qrGunReader, _qrWriter);
                connection.Received += OnReceiveMessage;
                connection.Errored += async (e) =>
                {
                    MessageBox.Show(e.ToString());
                    //log
                };
                connection.Closed += async () =>
                {
                    MessageBox.Show($"链接已关闭");
                };
                connection.Listen();

                //_ = StartReceiveQuene();
                _ = StartSendingQueue();
            }
            finally
            {
                //IsCanConnect = qrConnection?.State == QrConnectionState.Closed;
                //IsCanDisConnect = qrConnection?.State == QrConnectionState.Connected;
            }


        }

        static DbContextOptions<AppDbContext> dbOption = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite("Data Source=mydb.db")
                    .Options;

        private AppDbContext CreateDbContext()
        {
            
            var db = new AppDbContext(dbOption);
            return db;
        }
        private Task StartSendingQueue()
        {
            return Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    try
                    {
                        using (var db = CreateDbContext())
                        {


                            var result = db.SendingData.Where(x => x.Processed == false).OrderBy(x => x.AddTime)
                            .ToList();
                            foreach (var item in result)
                            {
                                if (item.IsProcessing(TimeSpan.FromMinutes(1)))
                                {
                                    continue;
                                }
                                item.ProcessingTime = DateTimeOffset.UtcNow;
                                await db.SaveChangesAsync();
                                try
                                {
                                    var str = System.Text.Json.JsonSerializer.Serialize(item);
                                    await connection!.SendAsync(new MemoryStream(Encoding.UTF8.GetBytes(str)));
                                    item.Processed = true;
                                    await db.SaveChangesAsync();
                                }
                                catch (Exception ex)
                                {
                                    item.ProcessingTime = null;
                                    item.Processed = false;
                                    await db.SaveChangesAsync();
                                }
                            }
                            await Task.Delay(1000);
                        }
                    }
                    catch(Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        //private Task StartReceiveQuene()
        //{
        //    return Task.Factory.StartNew(async () =>
        //    {
        //        if (qrConnection != null)
        //        {
        //            using var owner = MemoryPool<byte>.Shared.Rent(Packet.MaxPayload);

        //            while (true)
        //            {
        //                var result = await qrConnection.ReceiveAsync(owner.Memory);
        //                if (result.MessageType == MessageType.Binary)
        //                {
        //                    var pipe = new Pipe();
        //                    using (var stream = pipe.Writer.AsStream())
        //                    {
        //                        while (!result.EndOfMessage)
        //                        {

        //                            await stream.WriteAsync(owner.Memory[0..result.Count]);

        //                        }
        //                    }

        //                    using (var reader = new StreamReader(pipe.Reader.AsStream()))
        //                    {
        //                        var json = await reader.ReadToEndAsync();
        //                        var data = System.Text.Json.JsonSerializer.Deserialize<SendingData>(json)!;
        //                        var dbData = data.ToReceiveData();
        //                        db!.Add(dbData);
        //                        await db.SaveChangesAsync();
        //                    }

        //                }
        //                else if (result.MessageType == MessageType.Close)
        //                {
        //                    break;
        //                }
        //            }
        //        }
        //    },TaskCreationOptions.LongRunning);
        //}

        private async Task OnReceiveMessage(Stream stream)
        {
            using(var db = CreateDbContext())
            {
                using(var reader = new StreamReader(stream))
                {
                    var str = await reader.ReadToEndAsync();
                    var sendingData = System.Text.Json.JsonSerializer.Deserialize<SendingData>(str);
                    var receiveData = sendingData?.ToReceiveData();
                    if(receiveData != null)
                    {
                        db.Add(receiveData);
                        await db.SaveChangesAsync();
                    }
                }
            }
        }
        private void OnQrRead(byte[] buffer)
        {
            Dispatcher.Invoke(() =>
            {
                this.qr_r_txt.Text = Convert.ToBase64String(buffer);
            });

        }

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

        public static DependencyProperty QrBitmapProperty = DependencyProperty.Register(nameof(QrBitmap), typeof(SKBitmap), typeof(QRWindow));


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
                DecodeQr(value);
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
            //try
            //{
            //    if (qrConnection != null)
            //    {
            //        await qrConnection.ConnectAsync();
            //    }

            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message);
            //}
        }



        private void DecodeQr(SKBitmap bitmap)
        {
            var reader = new ZXing.SkiaSharp.BarcodeReader()
            {
                Options = new ZXing.Common.DecodingOptions
                {
                    CharacterSet = "ISO-8859-15"
                }
            };
            var result = reader.Decode(this.QrBitmap);
            if(result!= null)
            {
                this.qr_s_txt.Text = Convert.ToBase64String(((List<byte[]>)result.ResultMetadata[ZXing.ResultMetadataType.BYTE_SEGMENTS])
                .SelectMany(x => x).ToArray());
            }
            else
            {
                this.qr_r_txt.Text = "<null>";
            }
        }
    }
}

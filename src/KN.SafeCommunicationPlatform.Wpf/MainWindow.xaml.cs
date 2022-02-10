using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Window = System.Windows.Window;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
namespace KN.SafeCommunicationPlatform.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }


        OpenCvSharp.VideoCapture? VideoCapture;

        public async void Go(object sender, RoutedEventArgs args)
        {
            VideoCapture = VideoCapture.FromCamera(0, VideoCaptureAPIs.ANY);
            AppendLog($"开始捕捉");
            AppendLog($"FPS: {VideoCapture.Fps}");
            AppendLog($"CaptureType: {VideoCapture.CaptureType}");
            var x = VideoCapture.Open(0, VideoCaptureAPIs.ANY);
            AppendLog($"Open: {x}");
            //var mat = new Mat();
            while (!VideoCapture.IsDisposed)
            {
                AppendLog("read");
                using var mat = VideoCapture.RetrieveMat();
                
                if (!mat.Empty())
                { 
                    AppendLog("Video Empty");
                    await Task.Delay(100);
                    continue;
                }
                Dispatcher.Invoke(() =>
                {
                    this.Viewer.Source = mat.ToWriteableBitmap();
                });

                var reader = new ZXing.OpenCV.BarcodeReader();
                var result = reader.Decode(mat);

                this.result.Text = Convert.ToBase64String(result.RawBytes);
                AppendLog("=" + result.Text ?? this.result.Text);
                await Task.Delay(100);
            }
            //mat.Dispose();
        }

        private void Stop(object sender, RoutedEventArgs e)
        {
            VideoCapture?.Dispose();
            
        }

        private void AppendLog(string text)
        {
            log.Text = text.PadRight(80) + "---" + DateTime.Now + "\n" + log.Text;
            if(log.Text.Length > 9000)
            {
                log.Text = log.Text.Substring(0, 9000);
            }

        }
    }
}

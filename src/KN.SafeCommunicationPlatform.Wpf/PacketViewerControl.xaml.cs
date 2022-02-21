using KN.SafeCommunicationPlatform.Protocols.TransportLayer;
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

namespace KN.SafeCommunicationPlatform.Wpf
{
    /// <summary>
    /// PacketViewerControl.xaml 的交互逻辑
    /// </summary>
    public partial class PacketViewerControl : UserControl
    {
        public PacketViewerControl()
        {
            InitializeComponent();
        }



        public Packet Packet
        {
            get { return (Packet)GetValue(PacketProperty); }
            set { SetValue(PacketProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PacketProperty =
            DependencyProperty.Register(nameof(Packet), typeof(Packet), typeof(PacketViewerControl), new PropertyMetadata());


    }
}

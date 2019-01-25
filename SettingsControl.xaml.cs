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

namespace Viper.PluginCalcRotWheelSlip
{
    /// <summary>
    /// Logique d'interaction pour SettingsControlDemo.xaml
    /// </summary>

    public partial class SettingsControl : UserControl
    {
        public SettingsControl()
        {
            InitializeComponent();
            Speed.Value = AccSpeed.Value;
            //Speed.
        }

        private void Speed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            AccSpeed.Value = (int)Speed.Value;
        }

        private void Brake_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {

        }

        private void Throttle_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {

        }

        private void Vel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {

        }
    }

    public class AccSpeed
    {
        /*private static int Speed = 20;
        public static int Value
        {
            get { return Speed; }
            set { Speed = value; }
        }*/
        public static int Value { get; set; }
    }
}

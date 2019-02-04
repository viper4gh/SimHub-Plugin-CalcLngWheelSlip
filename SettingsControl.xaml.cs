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
using Newtonsoft.Json.Linq; // Needed for JObject 
using System.IO;    // Needed for read/write JSON settings file
using SimHub;   // Needed for Logging

namespace Viper.PluginCalcRotWheelSlip
{
    /// <summary>
    /// Logique d'interaction pour SettingsControlDemo.xaml
    /// </summary>

    public partial class SettingsControl : UserControl
    {
        private bool first_initialization;

        public SettingsControl()
        {
            InitializeComponent();
            first_initialization = true;
            Speed.Value = AccData.Speed;    //triggers Speed_ValueChanged, because of that "value_changed" must not set to true during the first intialization
            Brake.Value = AccData.Brake;
            Throttle.Value = AccData.Throttle;
            Vel.Value = AccData.Vel;
            first_initialization = false;
        }

        private bool value_changed = false;

        private void Speed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (Speed.Value < Speed.Minimum || Speed.Value == null) Speed.Value = Speed.Minimum;
            if (Speed.Value > Speed.Maximum) Speed.Value = Speed.Maximum;
            AccData.Speed = (int)Speed.Value;
            if (!first_initialization) value_changed = true;
        }

        private void Brake_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (Brake.Value < Brake.Minimum || Brake.Value == null) Brake.Value = Brake.Minimum;
            if (Brake.Value > Brake.Maximum) Brake.Value = Brake.Maximum;
            AccData.Brake = (int)Brake.Value;
            if (!first_initialization) value_changed = true;
        }

        private void Throttle_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (Throttle.Value < Throttle.Minimum || Throttle.Value == null) Throttle.Value = Throttle.Minimum;
            if (Throttle.Value > Throttle.Maximum) Throttle.Value = Throttle.Maximum;
            AccData.Throttle = (int)Throttle.Value; 
            if (!first_initialization) value_changed = true;
        }

        private void Vel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (Vel.Value < Vel.Minimum || Vel.Value == null) Vel.Value = Vel.Minimum;
            if (Vel.Value > Vel.Maximum) Vel.Value = Vel.Maximum;
            AccData.Vel = (double)Vel.Value;
            if (!first_initialization) value_changed = true;
        }

        private void SHSection_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //Trigger for saving JSON file. Event is fired if you enter or leave the Plugin Settings View or if you close SimHub

            //Saving on leaving Settings View only
            if (!SHSectionPluginOptions.IsVisible)
            {
                // generate JSON file only if something has changed
                if (value_changed)
                {
                    // generate JSON data
                    JObject JSONdata = new JObject(
                        new JProperty("Speed_min", Speed.Value),
                        new JProperty("Brake_max", Brake.Value),
                        new JProperty("Throttle_max", Throttle.Value),
                        new JProperty("VelX_max", Vel.Value)
                        );
                    //string settings_path = AccData.path;
                    try
                    {
                        // create/write settings file
                        File.WriteAllText(@AccData.path, JSONdata.ToString());
                        Logging.Current.Info("Plugin Viper.PluginCalcRotWheelSlip - Settings file saved: " + System.Environment.CurrentDirectory + "\\" + AccData.path);
                    }
                    catch
                    {
                        //A MessageBox creates graphical glitches after closing it. Search another way, maybe using the Standard Log in SimHub\Logs
                        //MessageBox.Show("Cannot create or write the following file: \n" + System.Environment.CurrentDirectory + "\\" + AccData.path, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Logging.Current.Error("Plugin Viper.PluginCalcRotWheelSlip - Cannot create or write settings file: " + System.Environment.CurrentDirectory + "\\" + AccData.path);


                    }
                    value_changed = false;
                }
                
                
            }
        }
    }

    //public class for exchanging the data with the main cs file (Init and DataUpdate function)
    public class AccData
    {
        public static int Speed { get; set; }
        public static int Brake { get; set; }
        public static int Throttle { get; set; }
        public static double Vel { get; set; }
        public static string path { get; set; }
    }

    /*public class AccSpeed - old way
    {*/
        /*private static int Speed = 20;
        public static int Value
        {
            get { return Speed; }
            set { Speed = value; }
        }*/
        /*public static int Value { get; set; }
    }*/
}

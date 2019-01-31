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
        private bool first_initialization;

        public SettingsControl()
        {
            InitializeComponent();
            first_initialization = true;
            Speed.Value = AccSpeed.Value;   //triggers Speed_ValueChanged, because of that the value_changed must set to true on this intialization
            Brake.Value = AccBrake.Value;
            Throttle.Value = AccThrottle.Value;
            Vel.Value = AccVel.Value;
            first_initialization = false;
            //Speed. 
            
        }

        private bool value_changed = false;

        private void Speed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (Speed.Value < Speed.Minimum || Speed.Value == null) Speed.Value = Speed.Minimum;
            if (Speed.Value > Speed.Maximum) Speed.Value = Speed.Maximum;
            AccSpeed.Value = (int)Speed.Value;
            if(!first_initialization) value_changed = true;
        }

        private void Brake_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (Brake.Value < Brake.Minimum || Brake.Value == null) Brake.Value = Brake.Minimum;
            if (Brake.Value > Brake.Maximum) Brake.Value = Brake.Maximum;
            AccBrake.Value = (int)Brake.Value;
            if (!first_initialization) value_changed = true;
        }

        private void Throttle_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (Throttle.Value < Throttle.Minimum || Throttle.Value == null) Throttle.Value = Throttle.Minimum;
            if (Throttle.Value > Throttle.Maximum) Throttle.Value = Throttle.Maximum;
            AccThrottle.Value = (int)Throttle.Value;
            if (!first_initialization) value_changed = true;
        }

        private void Vel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (Vel.Value < Vel.Minimum || Vel.Value == null) Vel.Value = Vel.Minimum;
            if (Vel.Value > Vel.Maximum) Vel.Value = Vel.Maximum;
            AccVel.Value = (double)Vel.Value;
            if (!first_initialization) value_changed = true;
        }

        private void SHSection_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //Trigger for saving JSON file. Event is fired if you enter or leave the Plugin Settings View or if you close SimHub

            //Saving on leaving Settings View only
            if (!SHSectionPluginOptions.IsVisible)
            {
                
                if (value_changed)
                {

                    //TODO: JSON saving
                    
                    value_changed = false;
                }
                
                
            }
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

    public class AccBrake
    {
        /*private static int Brake = 0;
        public static int Value
        {
            get { return Brake; }
            set { Speed = value; }
        }*/
        public static int Value { get; set; }
    }

    public class AccThrottle
    {
        /*private static int Throttle = 0;
        public static int Value
        {
            get { return Throttle; }
            set { Speed = value; }
        }*/
        public static int Value { get; set; }
    }

    public class AccVel
    {
        /*private static int Vel = 0.004;
        public static double Value
        {
            get { return Vel; }
            set { Speed = value; }
        }*/
        public static double Value { get; set; }
    }

}

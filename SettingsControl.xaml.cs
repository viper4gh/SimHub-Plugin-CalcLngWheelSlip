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
            TestField.Value = 5;
        }

        public double GetValue()
        {
            return (double)TestField.Value;
        }
    }
}

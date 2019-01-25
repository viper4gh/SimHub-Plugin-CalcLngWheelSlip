using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Windows.Forms;
using System.Windows.Controls;

namespace Viper.PluginCalcRotWheelSlip
{
    
    [PluginName("Calculate Rotational Tyre Slip 0.1")]
    [PluginDescrition("Calculates Tyre Slip by the relationship between Tyre RPS and Car Speed. Perfect for analyzing your Throttle and Brake input and TC/ABS settings\nFor Project CARS 2 and R3E only")]
    [PluginAuthor("Viper")]
    
    public class DataPlugin : IPlugin, IDataPlugin, IWPFSettings
    {
        //private int SpeedWarningLevel = 100;
        private object TyreDiameterFront;
        private bool TyreDiameterCalculated = false;
        private bool manualOverride = false;
        private bool reset = false;

        //input variables
        private string curGame;
        private object tmpObj;
        private double VelocityX = 0;
        private float Speedms = 0;
        private float[] TyreRPS = new float[] { 0f, 0f, 0f, 0f };

        //output variables
        private float[] TyreDiameter = new float[] { 0f, 0f, 0f, 0f };   // in meter - FL,FR,RL,RR
        private float[] RotTyreSlip = new float[] { 0f, 0f, 0f, 0f }; // Rotational Tyre Slip values FL,FR,RL,RR

        /// <summary>
        /// Instance of the current plugin manager
        /// </summary>
        public PluginManager PluginManager { get; set; }
            
        /// <summary>
        /// called one time per game data update
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <param name="data"></param>
        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            //pluginManager.SetPropertyValue("CurrentDateTime", this.GetType(), DateTime.Now);
            //pluginManager.TriggerInput()

            curGame = pluginManager.GetPropertyValue("DataCorePlugin.CurrentGame").ToString();

            if (data.GameRunning)
            {
                /*if (data.OldData != null && data.NewData != null)
                {
                    if (data.OldData.SpeedKmh < SpeedWarningLevel && data.OldData.SpeedKmh >= SpeedWarningLevel)
                    {
                        pluginManager.TriggerEvent("SpeedWarning", this.GetType());
                    }
                }*/
                if (data.OldData != null && data.NewData != null)   //TODO: check a record where the game was captured from startup on
                {
                    //////////////////////////////////////////// 
                    //map raw game variables for PCars2 and RRRE
                    if (curGame == "PCars2")
                    {
                        /*tmpObj = pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.mLocalVelocity01");
                        VelocityX = Convert.ToDouble((float)tmpObj);*/
                        VelocityX = Math.Abs((Convert.ToDouble((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.mLocalVelocity01"))));
                        Speedms = (float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.mSpeed");
                        tmpObj = pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.mTyreRPS01");
                        TyreRPS[0] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.mTyreRPS01"));
                        TyreRPS[1] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.mTyreRPS02"));
                        TyreRPS[2] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.mTyreRPS03"));
                        TyreRPS[3] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.mTyreRPS04"));
                        /*for (int i = 0; i < TyreRPS.Length; i++)
                        {
                        }*/
                    }
                    else if (curGame == "RRRE")
                    {
                        /*tmpObj = pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Player.LocalVelocity.X");
                        VelocityX = (double)tmpObj;*/
                        VelocityX = Math.Abs((double)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Player.LocalVelocity.X"));
                        Speedms = (float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.CarSpeed");
                        TyreRPS[0] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.TireRps.FrontLeft"));
                        TyreRPS[1] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.TireRps.FrontRight"));
                        TyreRPS[2] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.TireRps.RearLeft"));
                        TyreRPS[3] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.TireRps.RearRight"));
                    }

                    // END mapping
                    //////////////////////////////////////////////

                    // reset Tyre Diameter Calculation after car switch
                    if (data.OldData.CarModel != data.NewData.CarModel || reset == true)
                    {
                        TyreDiameterCalculated = false;
                        reset = false;
                        pluginManager.SetPropertyValue("CalcRotWheelSlip.TyreDiameterComputed", this.GetType(), false);
                        pluginManager.SetPropertyValue("CalcRotWheelSlip.Computed.TyreDiameter_FL", this.GetType(), "[NULL]");
                        pluginManager.SetPropertyValue("CalcRotWheelSlip.Computed.TyreDiameter_FR", this.GetType(), "[NULL]");
                        pluginManager.SetPropertyValue("CalcRotWheelSlip.Computed.TyreDiameter_RL", this.GetType(), "[NULL]");
                        pluginManager.SetPropertyValue("CalcRotWheelSlip.Computed.TyreDiameter_RR", this.GetType(), "[NULL]");
                        pluginManager.SetPropertyValue("CalcRotWheelSlip.Computed.RotTyreSlip_FL", this.GetType(), 0);
                        pluginManager.SetPropertyValue("CalcRotWheelSlip.Computed.RotTyreSlip_FR", this.GetType(), 0);
                        pluginManager.SetPropertyValue("CalcRotWheelSlip.Computed.RotTyreSlip_RL", this.GetType(), 0);
                        pluginManager.SetPropertyValue("CalcRotWheelSlip.Computed.RotTyreSlip_RR", this.GetType(), 0);
                    }

                    // calculate Tyre Diameter automatic (Speed > 20 km/h, Brake and Throttle = 0) or on manual Override  // TODO: pcars2 mLocalVelocity01/mSpeed , R3R LocalVelocity.X/CarSpeed  between -0.01 and 0.01
                    if ((data.NewData.SpeedKmh > 20 && data.NewData.Brake == 0 && data.NewData.Throttle == 0 && (VelocityX/Speedms) < 0.01 && TyreDiameterCalculated == false) || manualOverride == true)
                    {
                        //calculate Tyre diameters
                        // from javascript:
                        /*if($prop('DataCorePlugin.GameData.NewData.SpeedKmh') > 0 ){
	                        var TyreRPS = Math.abs($prop('DataCorePlugin.ExternalScript.TyreRPS_FL'));
	                        var Speed = $prop('DataCorePlugin.ExternalScript.Speedms');
	                        var diameter = 1;
	                        if(TyreRPS != 0) {
		                        //diameter = Speed / TyreRPS / Math.PI * (360 / 180 * Math.PI); // in brackets calculation radians to degree for full 360° circle
		                        diameter = Speed / TyreRPS * 2;
	                        }
	                        return diameter;
                        } else {return "NA"}
                        */
                        for (int i = 0; i < TyreRPS.Length; i++)
                        {
                            if(TyreRPS[i] != 0)
                            {
                                TyreDiameter[i] = Speedms / TyreRPS[i] * 2;
                            }
                        }
                        pluginManager.SetPropertyValue("CalcRotWheelSlip.Computed.TyreDiameter_FL", this.GetType(), TyreDiameter[0]);
                        pluginManager.SetPropertyValue("CalcRotWheelSlip.Computed.TyreDiameter_FR", this.GetType(), TyreDiameter[1]);
                        pluginManager.SetPropertyValue("CalcRotWheelSlip.Computed.TyreDiameter_RL", this.GetType(), TyreDiameter[2]);
                        pluginManager.SetPropertyValue("CalcRotWheelSlip.Computed.TyreDiameter_RR", this.GetType(), TyreDiameter[3]);

                        TyreDiameterCalculated = true;
                        manualOverride = false;
                        pluginManager.SetPropertyValue("CalcRotWheelSlip.TyreDiameterComputed", this.GetType(), true);
                    }

                    // calculate Tyre Lock / Spin
                    if (TyreDiameterCalculated == true)
                    {
                        //from javascript
                        /*if($prop('DataCorePlugin.GameData.NewData.SpeedKmh') > 0.1 ){
	                        var TyreRPS = Math.abs($prop('DataCorePlugin.ExternalScript.TyreRPS_FL'));
	                        var Speed = $prop('DataCorePlugin.ExternalScript.Speedms');
	                        var Slip = 0;
	                        Slip = (Speed - $prop('DataCorePlugin.ExternalScript.TyreDiameterFront') * TyreRPS / 2) / Speed ;
	                        if(Slip > 0 && $prop('DataCorePlugin.GameData.NewData.SpeedKmh') < 5){
		                        return 0;
	                        }else{
		                        return Slip;
	                        }
                        } else {return 0;}
                        */
                        if (Speedms > 0.01)
                        {
                            for (int i = 0; i < TyreDiameter.Length; i++)
                            {
                                RotTyreSlip[i] = (Speedms - TyreDiameter[i] * TyreRPS[i] / 2) / Speedms;
                                if(RotTyreSlip[i] > 0 && Speedms < 1.5)
                                {
                                    RotTyreSlip[i] = 0;
                                }
                            }
                            pluginManager.SetPropertyValue("CalcRotWheelSlip.Computed.RotTyreSlip_FL", this.GetType(), RotTyreSlip[0]);
                            pluginManager.SetPropertyValue("CalcRotWheelSlip.Computed.RotTyreSlip_FR", this.GetType(), RotTyreSlip[1]);
                            pluginManager.SetPropertyValue("CalcRotWheelSlip.Computed.RotTyreSlip_RL", this.GetType(), RotTyreSlip[2]);
                            pluginManager.SetPropertyValue("CalcRotWheelSlip.Computed.RotTyreSlip_RR", this.GetType(), RotTyreSlip[3]);
                        }
                    }
                }

            }
        }

        /*public float[] calcTyreDiameter()
        {
            float[] tmpTyreDiameter = new float[] { 0.65f, 0.65f, 0.7f, 0.7f };
            //return tmpTyreDiameter;
            return tmpTyreDiameter;
        }*/

                        /// <summary>
                        /// Called at plugin manager stop, close/displose anything needed here !
                        /// </summary>
                        /// <param name="pluginManager"></param>
                        public void End(PluginManager pluginManager)
        {
        }

        /// <summary>
        /// Return you winform settings control here, return null if no settings control
        /// 
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <returns></returns>
        public System.Windows.Forms.Control GetSettingsControl(PluginManager pluginManager)
        {
            return null;
        }

        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            return new SettingsControl();
        }

        /// <summary>
        /// Called after plugins startup
        /// </summary>
        /// <param name="pluginManager"></param>
        public void Init(PluginManager pluginManager)
        {
            //pluginManager.AddProperty("CurrentDateTime", this.GetType(), DateTime.Now);
            pluginManager.AddProperty("CalcRotWheelSlip.Computed.RotTyreSlip_FL", this.GetType(), 0);
            pluginManager.AddProperty("CalcRotWheelSlip.Computed.RotTyreSlip_FR", this.GetType(), 0);
            pluginManager.AddProperty("CalcRotWheelSlip.Computed.RotTyreSlip_RL", this.GetType(), 0);
            pluginManager.AddProperty("CalcRotWheelSlip.Computed.RotTyreSlip_RR", this.GetType(), 0);

            pluginManager.AddProperty("CalcRotWheelSlip.Computed.TyreDiameter_FL", this.GetType(), "[NULL]");
            pluginManager.AddProperty("CalcRotWheelSlip.Computed.TyreDiameter_FR", this.GetType(), "[NULL]");
            pluginManager.AddProperty("CalcRotWheelSlip.Computed.TyreDiameter_RL", this.GetType(), "[NULL]");
            pluginManager.AddProperty("CalcRotWheelSlip.Computed.TyreDiameter_RR", this.GetType(), "[NULL]");

            pluginManager.AddProperty("CalcRotWheelSlip.TyreDiameterComputed", this.GetType(), false);

            /*pluginManager.AddEvent("SpeedWarning", this.GetType());

            pluginManager.AddAction("IncrementSpeedWarning", this.GetType(), (a, b) =>
            {
                this.SpeedWarningLevel++;
            });

            pluginManager.AddAction("DecrementSpeedWarning", this.GetType(), (a, b) =>
            {
                this.SpeedWarningLevel--;
            });*/

            pluginManager.AddAction("CalcRotWheelSlip.CaptureTyreDiameter", this.GetType(), (a, b) =>
            {
                this.manualOverride = true;
            });

            pluginManager.AddAction("CalcRotWheelSlip.ResetTyreDiameter", this.GetType(), (a, b) =>
            {
                this.reset = true;
            });
        }
    }
}
 
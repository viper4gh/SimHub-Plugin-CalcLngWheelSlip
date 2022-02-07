using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Windows.Forms;
using System.Windows.Controls;
using Newtonsoft.Json.Linq; // Needed for JObject
using System.IO;    // Need for read/write JSON settings file
using SimHub;   // Needed for Logging

namespace Viper.PluginCalcLngWheelSlip
{
    [PluginName("Calculate Longitudinal Wheel Slip")]
    [PluginDescrition("Calculates Wheel Slip by the relationship between Tyre RPS and Car Speed. Perfect for analyzing your Throttle and Brake input and TC/ABS settings\nWorks for pCARS 1 and 2, R3E, AC, ACC, rF2 and F1 2018")]
    [PluginAuthor("Viper")]
    
    public class DataPlugin : IPlugin, IDataPlugin, IWPFSettings
    {
        private bool TyreDiameterCalculated = false;
        private bool manualOverride = false;
        private bool reset = false;

        //input variables
        private string curGame;
        private float VelocityX = 0;
        private float Speedms = 0;
        private float[] TyreRPS = new float[] { 0f, 0f, 0f, 0f };

        //output variables
        private float[] TyreDiameter = new float[] { 0f, 0f, 0f, 0f };   // in meter - FL,FR,RL,RR
        private float[] LngWheelSlip = new float[] { 0f, 0f, 0f, 0f }; // Longitudinal Wheel Slip values FL,FR,RL,RR

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
            //curGame = pluginManager.GetPropertyValue("DataCorePlugin.CurrentGame").ToString();
            curGame = data.GameName;

            if (data.GameRunning)
            {
                if (data.OldData != null && data.NewData != null && (curGame == "PCars2" || curGame == "PCars" || curGame == "Automobilista2" || curGame == "RRRE" || curGame == "RFactor2" || curGame == "RFactor2Spectator" || curGame == "AssettoCorsa" || curGame == "AssettoCorsaCompetizione" || curGame == "F12018"/* || curGame == "???"  -add other games here*/))   //TODO: check a record where the game was captured from startup on
                {
                    // Determine Speed in m/s - cast from object to double and then to float
                    Speedms = (float)((double)pluginManager.GetPropertyValue("DataCorePlugin.GameData.NewData.SpeedKmh") / 3.6);

                    //////////////////////////////////////////// 
                    //map raw game variables for PCars2 and RRRE
                    switch (curGame)
                    {
                        // TyreRPS array wheel order: FL, FR, RL, RR
                        case "PCars2":
                        case "PCars":
                        case "Automobilista2":
                            VelocityX = Math.Abs(((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.mLocalVelocity01")));
                            TyreRPS[0] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.mTyreRPS01"));
                            TyreRPS[1] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.mTyreRPS02"));
                            TyreRPS[2] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.mTyreRPS03"));
                            TyreRPS[3] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.mTyreRPS04"));
                            break;
                        case "RRRE":
                            VelocityX = Math.Abs((float)(double)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Player.LocalVelocity.X"));
                            TyreRPS[0] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.TireRps.FrontLeft"));
                            TyreRPS[1] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.TireRps.FrontRight"));
                            TyreRPS[2] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.TireRps.RearLeft"));
                            TyreRPS[3] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.TireRps.RearRight"));
                            break;
                        case "RFactor2":
                        case "RFactor2Spectator":
                            VelocityX = Math.Abs((float)(double)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.CurrentPlayer.mLocalVel.x"));
                            TyreRPS[0] = Math.Abs((float)(double)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.CurrentPlayerTelemetry.mWheels01.mRotation"));
                            TyreRPS[1] = Math.Abs((float)(double)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.CurrentPlayerTelemetry.mWheels02.mRotation"));
                            TyreRPS[2] = Math.Abs((float)(double)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.CurrentPlayerTelemetry.mWheels03.mRotation"));
                            TyreRPS[3] = Math.Abs((float)(double)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.CurrentPlayerTelemetry.mWheels04.mRotation"));
                            break;
                        case "AssettoCorsa":
                        case "AssettoCorsaCompetizione":
                            // local lateral Velocity is not available in AC/ACC. Used lateral G-force instead and divided it by 3 to bring it in the same range for the defined limit
                            VelocityX = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Physics.AccG01") / 3);
                            TyreRPS[0] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Physics.WheelAngularSpeed01"));
                            TyreRPS[1] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Physics.WheelAngularSpeed02"));
                            TyreRPS[2] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Physics.WheelAngularSpeed03"));
                            TyreRPS[3] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Physics.WheelAngularSpeed04"));
                            break;
                        case "F12018":
                            VelocityX = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.PlayerMotionData.m_localVelocityX"));
                            // F1 2018 provides tyre surface speed directly - array wheel order from API is RL, RR, FL, FR
                            TyreRPS[0] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.PlayerMotionData.m_wheelSpeed03"));
                            TyreRPS[1] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.PlayerMotionData.m_wheelSpeed04"));
                            TyreRPS[2] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.PlayerMotionData.m_wheelSpeed01"));
                            TyreRPS[3] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.PlayerMotionData.m_wheelSpeed02"));
                            break;
                        /*
                        case "???":
                            // add other game
                            break;
                        */
                    }
                    // END mapping
                    //////////////////////////////////////////////

                    // reset Tyre Diameter Calculation after car switch
                    if (data.OldData.CarModel != data.NewData.CarModel || reset == true)
                    {
                        TyreDiameterCalculated = false;
                        reset = false;
                        pluginManager.SetPropertyValue("CalcLngWheelSlip.TyreDiameterComputed", this.GetType(), false);
                        pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.TyreDiameter_FL", this.GetType(), "-");
                        pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.TyreDiameter_FR", this.GetType(), "-");
                        pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.TyreDiameter_RL", this.GetType(), "-");
                        pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.TyreDiameter_RR", this.GetType(), "-");
                        pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.LngWheelSlip_FL", this.GetType(), 0);
                        pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.LngWheelSlip_FR", this.GetType(), 0);
                        pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.LngWheelSlip_RL", this.GetType(), 0);
                        pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.LngWheelSlip_RR", this.GetType(), 0);
                    }

                    // F1 2018 needs no tyre diameter calculation, because the Tyre RPS values provide the tyre surface speed already. In this case it is also not possible to calculate the tyre diameter.
                    if (curGame != "F12018")
                    {
                        // calculate Tyre Diameter automatic (Speed > 20 km/h, Brake and Throttle = 0) or on manual Override 
                        // The if statement is for finding a moment when the wheel slip is nearly 0, because only then the car speed = tyre surface speed and the only then the tyre diameter calculation is correct
                        if ((data.NewData.SpeedKmh > AccData.Speed && data.NewData.Brake <= AccData.Brake && data.NewData.Throttle <= AccData.Throttle && (VelocityX / Speedms) < AccData.Vel && TyreDiameterCalculated == false) || manualOverride == true)
                        {
                            //calculate tyre diameters
                            for (int i = 0; i < TyreRPS.Length; i++)
                            {
                                if (TyreRPS[i] != 0)
                                {
                                    TyreDiameter[i] = Speedms / TyreRPS[i] * 2;
                                }
                            }
                            pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.TyreDiameter_FL", this.GetType(), TyreDiameter[0]);
                            pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.TyreDiameter_FR", this.GetType(), TyreDiameter[1]);
                            pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.TyreDiameter_RL", this.GetType(), TyreDiameter[2]);
                            pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.TyreDiameter_RR", this.GetType(), TyreDiameter[3]);

                            TyreDiameterCalculated = true;
                            manualOverride = false;
                            pluginManager.SetPropertyValue("CalcLngWheelSlip.TyreDiameterComputed", this.GetType(), true);
                        }
                    }

                    // calculate Wheel Lock / Spin
                    if (TyreDiameterCalculated == true || curGame == "F12018")
                    {
                        for (int i = 0; i < TyreDiameter.Length; i++)
                        {
                            //calculate over 0.01 m/s only, because the Slip results are extreme high below 0.01 (Division by Speed)
                            if (Speedms > 0.5)
                            {
                                /*Understanding calculation
                                 For the longitudinal wheel slip we need the ratio between the tyre surface speed and the car speed.
                                 Car speed is directly available, but the tyre surface speed must be calculated.
                                    tyre surface speed(m/s) = tyre diameter(m) * Pi * tyre revolutions per second
                                 The games provide TyreRPS, but it is in radiants per second, you have to calculate the revolutions per second first.
                                    wheel rotation degrees per second = TyreRPS * 180 / Pi
                                    wheel revolutions per second (one full revolution = 360°) = TyreRPS * 180 / Pi / 360
                                And now all together:
                                    tyre surface speed(m/s) = tyre diameter(m) * Pi * TyreRPS * 180 / Pi / 360
                                You can eliminate Pi and shorten 180/360
                                    tyre surface speed(m/s) = tyre diameter(m) * TyreRPS / 2
                                Or for tyre diameter calculation
                                    tyre diameter(m) = tyre surface speed(m/s) / TyreRPS * 2
                                If the wheel slip is 0 then tyre surface speed = car speed, which is used abobe for the tyre diameter calculation
                                    tyre diameter(m) = car speed(m/s) / TyreRPS * 2
                                The ratio is now
                                    (car speed - tyre surface speed) / car speed
                                Some Examples:
                                    no wheel slip: (50 m/s - 50 m/s) / 50 m/s = 0
                                    full wheel lock: (50 m/s - 0 m/s) / 50 m/s = 1
                                    wheel spins with double car speed: (50 m/s - 100 m/s) / 50 m/s = -1
                                */
                                if(curGame != "F12018")
                                {
                                    LngWheelSlip[i] = (Speedms - TyreDiameter[i] * TyreRPS[i] / 2) / Speedms;
                                }
                                else
                                {
                                    // F1 2018 TyreRPS = Wheel Speed directly
                                    LngWheelSlip[i] = (Speedms - TyreRPS[i]) / Speedms;
                                }
                                
                                //don't show wheel lock below 1 m/s
                                if (LngWheelSlip[i] > 0 && Speedms < 1)
                                {
                                    LngWheelSlip[i] = 0;
                                }
                                
                                if (LngWheelSlip[0] < -0.1) { }  // For Debugging only
                            }
                            else
                            {
                                // below 0.5 m/s show wheel slip directly, because division by speed generates to high values. Use divisor 10 to bring it better in the range between 0 and -1
                                // 
                                if (curGame != "F12018")
                                {
                                    LngWheelSlip[i] = (Speedms - TyreDiameter[i] * TyreRPS[i] / 2) / 10;
                                }
                                else
                                {
                                    // F1 2018 TyreRPS = Wheel Speed directly
                                    LngWheelSlip[i] = (Speedms - TyreRPS[i]) / 10;
                                }

                                if (Speedms > 0.4) { }  // For Debugging only
                            }
                            
                        }
                        pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.LngWheelSlip_FL", this.GetType(), LngWheelSlip[0]);
                        pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.LngWheelSlip_FR", this.GetType(), LngWheelSlip[1]);
                        pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.LngWheelSlip_RL", this.GetType(), LngWheelSlip[2]);
                        pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.LngWheelSlip_RR", this.GetType(), LngWheelSlip[3]);
                    }
                }

            }
        }

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
            // set path/filename for settings file
            AccData.path = PluginManager.GetCommonStoragePath("Viper.PluginCalcLngWheelSlip.json");
            
            // try to read settings file
            try
            {
                JObject JSONdata = JObject.Parse(File.ReadAllText(@AccData.path));
                AccData.Speed = (int)JSONdata["Speed_min"];
                AccData.Brake = (int)JSONdata["Brake_max"];
                AccData.Throttle = (int)JSONdata["Throttle_max"];
                AccData.Vel = (double)JSONdata["VelX_max"];
                Logging.Current.Info("Plugin Viper.PluginCalcLngWheelSlip - Settings file " + System.Environment.CurrentDirectory + "\\" + AccData.path + " loaded.");
            }
            // if there is no settings file, use the following defaults
            catch
            {
                AccData.Speed = 20;
                AccData.Brake = 0;
                AccData.Throttle = 5;
                AccData.Vel = 0.001;
                Logging.Current.Info("Plugin Viper.PluginCalcLngWheelSlip - Default settings loaded.");
            }
            
            
            pluginManager.AddProperty("CalcLngWheelSlip.Computed.LngWheelSlip_FL", this.GetType(), 0);
            pluginManager.AddProperty("CalcLngWheelSlip.Computed.LngWheelSlip_FR", this.GetType(), 0);
            pluginManager.AddProperty("CalcLngWheelSlip.Computed.LngWheelSlip_RL", this.GetType(), 0);
            pluginManager.AddProperty("CalcLngWheelSlip.Computed.LngWheelSlip_RR", this.GetType(), 0);

            pluginManager.AddProperty("CalcLngWheelSlip.Computed.TyreDiameter_FL", this.GetType(), "-");
            pluginManager.AddProperty("CalcLngWheelSlip.Computed.TyreDiameter_FR", this.GetType(), "-");
            pluginManager.AddProperty("CalcLngWheelSlip.Computed.TyreDiameter_RL", this.GetType(), "-");
            pluginManager.AddProperty("CalcLngWheelSlip.Computed.TyreDiameter_RR", this.GetType(), "-");

            pluginManager.AddProperty("CalcLngWheelSlip.TyreDiameterComputed", this.GetType(), false);

            pluginManager.AddAction("CalcLngWheelSlip.CalcTyreDiameter", this.GetType(), (a, b) =>
            {
                this.manualOverride = true;
            });
            
            pluginManager.AddAction("CalcLngWheelSlip.ResetTyreDiameter", this.GetType(), (a, b) =>
            {
                this.reset = true;
            });
        }
    }
}
 
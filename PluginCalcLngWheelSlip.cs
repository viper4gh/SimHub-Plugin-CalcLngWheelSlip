using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Text;  //For File Encoding
using System.Windows.Forms;
using System.Windows.Controls;
//using System.Linq; // Needed for Properties().OrderBy
using Newtonsoft.Json.Linq; // Needed for JObject
using System.IO;    // Need for read/write JSON settings file
using SimHub;   // Needed for Logging

namespace Viper.PluginCalcLngWheelSlip
{
    [PluginName("Calculate Longitudinal Wheel Slip")]
    [PluginDescription("Calculates Wheel Slip by the relationship between Tyre RPS and Car Speed.\nPerfect for analyzing your Throttle/Brake input and TC/ABS/Diff settings.\nWorks for pCARS 1 and 2, AMS2, PMR, R3E, AC, ACC, AC EVO, AC Rally, rF2, LMU, F1 2018-2025, GT7, WRC23")]
    [PluginAuthor("Viper")]
    
    //the class name is used as the property headline name in SimHub "Available Properties"
    public class ViperDataPlugin : IPlugin, IDataPlugin, IWPFSettings
    {
        private bool TyreDiameterCalculated = false;
        private bool TyreDiameterDetNeeded = true;
        private bool manualOverride = false;
        private bool reset = false;
        private bool F1_18_22 = false;   // for check if one of the F1 games 2018-2022 is used
        private bool F1_23_x = false;   // for check if one of the F1 games 2023 - x is used

        //input variables
        private string curGame;
        private string CarModel = "-";
        //private string CarModel = "";
        private float VelocityX = 0;
        private float Speedms = 0;
        private float[] TyreRPS = new float[] { 0f, 0f, 0f, 0f };

        private JObject JSONdata_diameters;

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
            if (data.GameRunning)
            {
                //curGame = pluginManager.GetPropertyValue("DataCorePlugin.CurrentGame").ToString();
                curGame = data.GameName;

                // check if F1 games 2018-2022 is in use, since 2023 Property Names have changed
                if (curGame == "F12018" ||
                    curGame == "F12019" ||
                    curGame == "F12020" ||
                    curGame == "F12021" ||
                    curGame == "F12022"
                )
                {
                    F1_18_22 = true;
                }
                else
                {
                    F1_18_22 = false;
                }

                // check if F1 games 2023-x is in use
                if (curGame == "F12023" ||
                    curGame == "F12024" ||
                    curGame == "F12025"
                )
                {
                    F1_23_x = true;
                }
                else
                {
                    F1_23_x = false;
                }

                if (data.OldData != null && data.NewData != null && (
                    curGame == "PCars2" || 
                    curGame == "PCars" || 
                    curGame == "Automobilista2" ||
                    curGame == "ProjectMotorRacing" ||
                    curGame == "RRRE" || 
                    curGame == "RFactor2" || 
                    curGame == "RFactor2Spectator" || 
                    curGame == "LMU" || 
                    curGame == "AssettoCorsa" || 
                    curGame == "AssettoCorsaCompetizione" || 
                    curGame == "AssettoCorsaEVO" ||
                    curGame == "AssettoCorsaRally" ||
                    F1_18_22 ||
                    F1_23_x ||
                    curGame == "F12023" ||
                    curGame == "GranTurismo7" || 
                    curGame == "EAWRC23"/* || curGame == "???"  -add other games here*/
                ))   //TODO: check a record where the game was captured from startup on
                {
                    // Determine Speed in m/s - cast from object to double and then to float
                    // For WRC23 SpeedKmh is calculated from vehicle_transmission_speed and shows not the real car speed, using vehicle_speed from raw data instead 
                    if (curGame == "EAWRC23")
                    {
                        Speedms = (float)(pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.SessionUpdate.vehicle_speed"));
                    }
                    else
                    {
                        Speedms = (float)((double)pluginManager.GetPropertyValue("DataCorePlugin.GameData.NewData.SpeedKmh") / 3.6);
                    }

                    // Use another value for car speed for all games, info from Wotever (SimHub dev)
                    //Speedms = (float)(data.NewData.FeedbackData.GroundSpeed.GetValueOrDefault() /3.6);

                    //////////////////////////////////////////// 
                    //map raw game variables for games (not F1)
                    switch (curGame)
                    {
                        // TyreRPS array wheel order: FL, FR, RL, RR
                        case "PCars2":
                        case "PCars":
                        case "Automobilista2":
                            // Shared Memory Mode
                            if(pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.mLocalVelocity01") != null)
                            {
                                VelocityX = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.mLocalVelocity01"));
                                TyreRPS[0] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.mTyreRPS01"));
                                TyreRPS[1] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.mTyreRPS02"));
                                TyreRPS[2] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.mTyreRPS03"));
                                TyreRPS[3] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.mTyreRPS04"));
                            }
                            // UDP Mode
                            else
                            {
                                VelocityX = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.sTelemetryData.sLocalVelocity01"));
                                TyreRPS[0] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.sTelemetryData.sTyreRPS01"));
                                TyreRPS[1] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.sTelemetryData.sTyreRPS02"));
                                TyreRPS[2] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.sTelemetryData.sTyreRPS03"));
                                TyreRPS[3] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.sTelemetryData.sTyreRPS04"));
                            }
                            TyreDiameterDetNeeded = true;
                            // alternative value for tyre RPS
                            /*TyreRPS[0] = Math.Abs((float)data.NewData.FeedbackData.WheelRPS[0]);
                            TyreRPS[1] = Math.Abs((float)data.NewData.FeedbackData.WheelRPS[1]);
                            TyreRPS[2] = Math.Abs((float)data.NewData.FeedbackData.WheelRPS[2]);
                            TyreRPS[3] = Math.Abs((float)data.NewData.FeedbackData.WheelRPS[3]);*/
                            break;
                        case "RRRE":
                            VelocityX = Math.Abs((float)(double)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Player.LocalVelocity.X"));
                            TyreRPS[0] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.TireRps.FrontLeft"));
                            TyreRPS[1] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.TireRps.FrontRight"));
                            TyreRPS[2] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.TireRps.RearLeft"));
                            TyreRPS[3] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.TireRps.RearRight"));
                            TyreDiameterDetNeeded = true;
                            break;
                        case "RFactor2":
                        case "RFactor2Spectator":
                        case "LMU": // Le Mans Ultimate
                            VelocityX = Math.Abs((float)(double)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.CurrentPlayer.mLocalVel.x"));
                            TyreRPS[0] = Math.Abs((float)(double)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.CurrentPlayerTelemetry.mWheels01.mRotation"));
                            TyreRPS[1] = Math.Abs((float)(double)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.CurrentPlayerTelemetry.mWheels02.mRotation"));
                            TyreRPS[2] = Math.Abs((float)(double)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.CurrentPlayerTelemetry.mWheels03.mRotation"));
                            TyreRPS[3] = Math.Abs((float)(double)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.CurrentPlayerTelemetry.mWheels04.mRotation"));
                            TyreDiameterDetNeeded = true;
                            break;
                        case "AssettoCorsa":
                        case "AssettoCorsaCompetizione":
                        case "AssettoCorsaEVO":
                        case "AssettoCorsaRally":
                            // local lateral Velocity is not available in AC/ACC. Used lateral G-force instead and divided it by 3 to bring it in the same range for the defined limit
                            VelocityX = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Physics.AccG01") / 3);
                            TyreRPS[0] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Physics.WheelAngularSpeed01"));
                            TyreRPS[1] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Physics.WheelAngularSpeed02"));
                            TyreRPS[2] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Physics.WheelAngularSpeed03"));
                            TyreRPS[3] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Physics.WheelAngularSpeed04"));
                            TyreDiameterDetNeeded = true;
                            break;
                        case "GranTurismo7":
                            VelocityX = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.LocalVelocity.X"));
                            TyreRPS[0] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Wheel_RevPerSecond01"));
                            TyreRPS[1] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Wheel_RevPerSecond02"));
                            TyreRPS[2] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Wheel_RevPerSecond03"));
                            TyreRPS[3] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Wheel_RevPerSecond04"));
                            TyreDiameterDetNeeded = true;
                            break;
                        //games where tyre diameter detection phase is not needed, in this case TyreRPS includes the tyre speeds instead and VelocityX is not needed
                        case "EAWRC23":
                            TyreRPS[0] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.SessionUpdate.vehicle_cp_forward_speed_fl"));
                            TyreRPS[1] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.SessionUpdate.vehicle_cp_forward_speed_fr"));
                            TyreRPS[2] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.SessionUpdate.vehicle_cp_forward_speed_bl"));
                            TyreRPS[3] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.SessionUpdate.vehicle_cp_forward_speed_br"));
                            TyreDiameterDetNeeded = false;
                            break;
                        case "ProjectMotorRacing":
                            TyreRPS[0] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.PlayerTelemetry.m_wheels01.m_linearSpeed"));
                            TyreRPS[1] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.PlayerTelemetry.m_wheels02.m_linearSpeed"));
                            TyreRPS[2] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.PlayerTelemetry.m_wheels03.m_linearSpeed"));
                            TyreRPS[3] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.PlayerTelemetry.m_wheels04.m_linearSpeed"));
                            TyreDiameterDetNeeded = false;
                            break;
                            /*
                            case "???":
                                // add other game
                                break;
                            */
                    }

                    //VelocityX is not needed for F1 games, because it is needed for the tyre diameter detection phase only, which is also not needed and not executed for F1 games
                    //F1 games provides tyre surface speed directly - array wheel order from API is RL, RR, FL, FR
                    //map raw game variables for F1 games 2018-2022
                    if (F1_18_22)
                    {
                        
                        TyreRPS[0] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.PlayerMotionData.m_wheelSpeed03"));
                        TyreRPS[1] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.PlayerMotionData.m_wheelSpeed04"));
                        TyreRPS[2] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.PlayerMotionData.m_wheelSpeed01"));
                        TyreRPS[3] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.PlayerMotionData.m_wheelSpeed02"));
                        TyreDiameterDetNeeded = false;
                    }
                    //map raw game variables for F1 games 2023-x
                    if (F1_23_x)
                    {
                        // since F1 2023 Property Names have changed in comparison to the versions before
                        TyreRPS[0] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.PacketMotionExData.m_wheelSpeed03"));
                        TyreRPS[1] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.PacketMotionExData.m_wheelSpeed04"));
                        TyreRPS[2] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.PacketMotionExData.m_wheelSpeed01"));
                        TyreRPS[3] = Math.Abs((float)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.PacketMotionExData.m_wheelSpeed02"));
                        TyreDiameterDetNeeded = false;
                    }
                    // END mapping
                    //////////////////////////////////////////////

                    //set TyreDiameterDetNeeded Property
                    pluginManager.SetPropertyValue("CalcLngWheelSlip.TyreDiameterDetNeeded", this.GetType(), TyreDiameterDetNeeded);
                    
                    // reset Tyre Diameter Calculation after car switch or pressing the reset key
                    //if (data.OldData.CarModel != data.NewData.CarModel || reset == true)  //not working correctly, on starting a session the data.OldData Object is null for a short time and the reset logic is not triggered
                    if ((/*CarModel != "-" && */CarModel != data.NewData.CarModel) || reset == true)
                    {
                        TyreDiameterCalculated = false;
                        
                        pluginManager.SetPropertyValue("CalcLngWheelSlip.TyreDiameterComputed", this.GetType(), false);
                        pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.TyreDiameter_FL", this.GetType(), "-");
                        pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.TyreDiameter_FR", this.GetType(), "-");
                        pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.TyreDiameter_RL", this.GetType(), "-");
                        pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.TyreDiameter_RR", this.GetType(), "-");
                        pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.LngWheelSlip_FL", this.GetType(), 0);
                        pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.LngWheelSlip_FR", this.GetType(), 0);
                        pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.LngWheelSlip_RL", this.GetType(), 0);
                        pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.LngWheelSlip_RR", this.GetType(), 0);
                        CarModel = "-";

                        JObject JSONcurGameData = (JObject)JSONdata_diameters[curGame];
                        //JObject JSONcurGameData = JSONdata_diameters[curGame] as JObject;
                        if (JSONcurGameData != null)
                        {  // data for current game available
                            JArray JSONdiameters = (JArray)JSONcurGameData[data.NewData.CarModel];
                            if (JSONdiameters != null) //car diameters found
                            {
                                //if the reset key was pressed, remove the car from JSONcurGameData before adding the new data
                                if (reset == true)
                                {   //remove car property from JSONcurGameData 
                                    JSONcurGameData.Property(data.NewData.CarModel).Remove();
                                }
                                //if CarModel switched
                                else
                                {
                                    //Validate tyre diameters from JSON file
                                    bool data_ok = true;
                                    //check array count
                                    if (JSONdiameters.Count == 4)
                                    {
                                        for (int i = 0; i < JSONdiameters.Count; i++)
                                        {
                                            // check data type
                                            if (JSONdiameters[i].Type == JTokenType.Float || JSONdiameters[i].Type == JTokenType.Integer)
                                            {
                                                // check value range
                                                if ((float)JSONdiameters[i] < 0.00999 || (float)JSONdiameters[i] > 10)  //0.01 is not working, used 0.00999, so that 0.01 is valid
                                                {
                                                    data_ok = false;
                                                    Logging.Current.Info("Plugin Viper.PluginCalcLngWheelSlip - Data file validation error. The value for a diameter is out of range (0.01-10): " + JSONdiameters[i]);
                                                }
                                            }
                                            else
                                            {
                                                data_ok = false;
                                                Logging.Current.Info("Plugin Viper.PluginCalcLngWheelSlip - Data file validation error. The value for the diameter is not from type Float or Integer: " + JSONdiameters[i].Type);

                                            }
                                        }
                                    }
                                    else
                                    {
                                        data_ok = false;
                                        Logging.Current.Info("Plugin Viper.PluginCalcLngWheelSlip - Data file validation error. The count of diameter values is wrong, expected 4, get " + JSONdiameters.Count + ".");
                                    }

                                    if (data_ok)
                                    {
                                        pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.TyreDiameter_FL", this.GetType(), JSONdiameters[0]);
                                        pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.TyreDiameter_FR", this.GetType(), JSONdiameters[1]);
                                        pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.TyreDiameter_RL", this.GetType(), JSONdiameters[2]);
                                        pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.TyreDiameter_RR", this.GetType(), JSONdiameters[3]);
                                        for (int i = 0; i < TyreDiameter.Length; i++)
                                        {
                                            TyreDiameter[i] = (float)JSONdiameters[i];
                                        }
                                        /*TyreDiameter[0] = (float)JSONdiameters[0];
                                        TyreDiameter[1] = (float)JSONdiameters[1];
                                        TyreDiameter[2] = (float)JSONdiameters[2];
                                        TyreDiameter[3] = (float)JSONdiameters[3];*/
                                        TyreDiameterCalculated = true;
                                        pluginManager.SetPropertyValue("CalcLngWheelSlip.TyreDiameterComputed", this.GetType(), true);
                                    }
                                    CarModel = data.NewData.CarModel;
                                }
                            }
                            else
                            {   //car diameters not found

                            }
                        }
                        reset = false;
                    }

                    // F1 games, WRC23, PMR need no tyre diameter calculation, because the Tyre RPS values provide the tyre surface speed already. In this case it is also not possible to calculate the tyre diameter.
                    //if (!F1x && curGame != "EAWRC23" && curGame != "ProjectMotorRacing")
                    if (TyreDiameterDetNeeded)
                        {
                        // calculate Tyre Diameter automatic or on manual Override
                        if (TyreDiameterCalculated == false || manualOverride == true)
                        {
                            // Check if Speed is in the limit
                            if(data.NewData.SpeedKmh > AccData.Speed)
                            {
                                pluginManager.SetPropertyValue("CalcLngWheelSlip.TyreDiameterDetLimitOK.Speed", this.GetType(), true);
                            }
                            else
                            {
                                pluginManager.SetPropertyValue("CalcLngWheelSlip.TyreDiameterDetLimitOK.Speed", this.GetType(), false);
                            }
                            // Check if Throttle is in the limit
                            if (data.NewData.Throttle <= AccData.Throttle)
                            {
                                pluginManager.SetPropertyValue("CalcLngWheelSlip.TyreDiameterDetLimitOK.Throttle", this.GetType(), true);
                            }
                            else
                            {
                                pluginManager.SetPropertyValue("CalcLngWheelSlip.TyreDiameterDetLimitOK.Throttle", this.GetType(), false);
                            }
                            // Check if Brake is in the limit
                            if (data.NewData.Brake <= AccData.Brake)
                            {
                                pluginManager.SetPropertyValue("CalcLngWheelSlip.TyreDiameterDetLimitOK.Brake", this.GetType(), true);
                            }
                            else
                            {
                                pluginManager.SetPropertyValue("CalcLngWheelSlip.TyreDiameterDetLimitOK.Brake", this.GetType(), false);
                            }
                            // Check if Lateral Velocity is in the limit
                            if ((VelocityX / Speedms) < AccData.Vel)
                            {
                                pluginManager.SetPropertyValue("CalcLngWheelSlip.TyreDiameterDetLimitOK.LateralVel", this.GetType(), true);
                            }
                            else
                            {
                                pluginManager.SetPropertyValue("CalcLngWheelSlip.TyreDiameterDetLimitOK.LateralVel", this.GetType(), false);
                            }

                            // The if statement is for finding a moment when the wheel slip is nearly 0, because only then the car speed = tyre surface speed and the only then the tyre diameter calculation is correct
                            if ((data.NewData.SpeedKmh > AccData.Speed && data.NewData.Brake <= AccData.Brake && data.NewData.Throttle <= AccData.Throttle && (VelocityX / Speedms) < AccData.Vel) || manualOverride == true)
                            {
                                //calculate tyre diameters
                                for (int i = 0; i < TyreRPS.Length; i++)
                                {
                                    if (TyreRPS[i] != 0)
                                    {
                                        TyreDiameter[i] = Speedms / TyreRPS[i] * 2;
                                        TyreDiameter[i] = (float)Math.Round(TyreDiameter[i], 3);
                                    }
                                }
                                pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.TyreDiameter_FL", this.GetType(), TyreDiameter[0]);
                                pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.TyreDiameter_FR", this.GetType(), TyreDiameter[1]);
                                pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.TyreDiameter_RL", this.GetType(), TyreDiameter[2]);
                                pluginManager.SetPropertyValue("CalcLngWheelSlip.Computed.TyreDiameter_RR", this.GetType(), TyreDiameter[3]);

                                // write diameters as new property in JSONdata_diameters object
                                JObject JSONcurGameData = (JObject)JSONdata_diameters[curGame];
                                // if data for game is not available, create a json game object first
                                if (JSONcurGameData == null)
                                {
                                    JObject emptyJObj = new JObject();
                                    JSONdata_diameters.Add(curGame,emptyJObj);
                                    JSONcurGameData = (JObject)JSONdata_diameters[curGame];
                                }
                                string diameters = "[" + TyreDiameter[0].ToString() + "," + TyreDiameter[1].ToString() + "," + TyreDiameter[2].ToString() + "," + TyreDiameter[3].ToString() + "]";
                                //check if the data for the car is already available and if yes, remove it first - can for example happen on manual override by pressing the key for CalcTyreDiameter 
                                if (JSONcurGameData[data.NewData.CarModel] != null)
                                {
                                    JSONcurGameData.Property(data.NewData.CarModel).Remove();
                                }
                                JSONcurGameData.Add(data.NewData.CarModel, JArray.Parse(diameters));

                                //Console.WriteLine(JSONdata_diameters.ToString());

                                //CarModel = data.NewData.CarModel;
                                TyreDiameterCalculated = true;
                                manualOverride = false;
                                pluginManager.SetPropertyValue("CalcLngWheelSlip.TyreDiameterComputed", this.GetType(), true);
                            }
                            CarModel = data.NewData.CarModel;
                        }
                    }

                    // calculate Wheel Lock / Spin
                    //if (TyreDiameterCalculated == true || F1x || curGame == "EAWRC23" || curGame == "ProjectMotorRacing")
                    if (TyreDiameterCalculated == true || !TyreDiameterDetNeeded)
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
                                //if(!F1x && curGame != "EAWRC23" && curGame != "ProjectMotorRacing")
                                if (TyreDiameterDetNeeded)
                                {
                                    LngWheelSlip[i] = (Speedms - TyreDiameter[i] * TyreRPS[i] / 2) / Speedms;
                                }
                                else
                                {
                                    // F1, WRC, PMR games TyreRPS = Wheel Speed directly
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
                                //if (!F1x && curGame != "EAWRC23" && curGame != "ProjectMotorRacing")
                                if (TyreDiameterDetNeeded)
                                {
                                    LngWheelSlip[i] = (Speedms - TyreDiameter[i] * TyreRPS[i] / 2) / 10;
                                }
                                else
                                {
                                    // F1, WRC, PMR games TyreRPS = Wheel Speed directly
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
            string path_data = PluginManager.GetCommonStoragePath("Viper.PluginCalcLngWheelSlip.data.json");
 
            // try to read complete data file from disk, compare file data with new data and write new file if there are diffs
            try
            {
                JObject JSONdata_diameters_file = JObject.Parse(File.ReadAllText(@path_data));
                JObject JSONcurGameData_file = (JObject)JSONdata_diameters_file[curGame];
                JObject JSONcurGameData = (JObject)JSONdata_diameters[curGame];
                if (!JToken.DeepEquals(JSONcurGameData_file, JSONcurGameData))
                {
                    //sort - disabled, not really needed
                    /*JSONdata_diameters.Property(curGame).Remove();
                    JObject sortedObj = new JObject(
                        JSONcurGameData.Properties().OrderBy(p => p.Name)
                    );
                    JSONdata_diameters.Add(curGame, sortedObj);*/

                    File.WriteAllText(@path_data, JSONdata_diameters.ToString(), Encoding.UTF8);
                    Logging.Current.Info("Plugin Viper.PluginCalcLngWheelSlip - data file " + System.Environment.CurrentDirectory + "\\" + path_data + " saved.");
                }
            }
            // if there is not already a settings file on disk, create new one and write data for current game
            catch (FileNotFoundException)
            {
                // try to write data file
                try
                {
                    //sort - disabled, not really needed, to many checks necessary, e.g. on Remove if JSONdata_diameters is empty
                    /*JSONdata_diameters.Property(curGame).Remove();
                    JObject sortedObj = new JObject(
                        JSONcurGameData.Properties().OrderBy(p => p.Name)
                    );
                    JSONdata_diameters.Add(curGame, sortedObj);*/

                    File.WriteAllText(@path_data, JSONdata_diameters.ToString(), Encoding.UTF8);
                    Logging.Current.Info("Plugin Viper.PluginCalcLngWheelSlip - data file " + System.Environment.CurrentDirectory + "\\" + path_data + " created and saved.");
                }
                // data file cannot be written
                catch (Exception ex)
                {
                    Logging.Current.Info("Plugin Viper.PluginCalcLngWheelSlip - cannot write data file. The following error occured: " + ex.Message);
                }
            }
            // other errors like Syntax error on JSON parsing, data file will not be saved
            catch (Exception ex)
            {
                Logging.Current.Info("Plugin Viper.PluginCalcLngWheelSlip - data file not saved. The following error occured: " + ex.Message);
            }
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
            string path_data = PluginManager.GetCommonStoragePath("Viper.PluginCalcLngWheelSlip.data.json");

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
                AccData.Speed = 50;
                AccData.Brake = 0;
                AccData.Throttle = 5;
                AccData.Vel = 0.001;
                Logging.Current.Info("Plugin Viper.PluginCalcLngWheelSlip - Default settings loaded.");
            }

            // try to read data file
            try
            {
                JSONdata_diameters = JObject.Parse(File.ReadAllText(@path_data));              
                Logging.Current.Info("Plugin Viper.PluginCalcLngWheelSlip - Data file " + System.Environment.CurrentDirectory + "\\" + path_data + " loaded.");
            }
            // if there is no data file, create new empty JObject
            catch (Exception ex)
            {
                JSONdata_diameters = new JObject();
                Logging.Current.Info("Plugin Viper.PluginCalcLngWheelSlip - no Data file loaded - the following error occured: " + ex.Message);
            }


            pluginManager.AddProperty("CalcLngWheelSlip.Computed.LngWheelSlip_FL", this.GetType(), 0);
            pluginManager.AddProperty("CalcLngWheelSlip.Computed.LngWheelSlip_FR", this.GetType(), 0);
            pluginManager.AddProperty("CalcLngWheelSlip.Computed.LngWheelSlip_RL", this.GetType(), 0);
            pluginManager.AddProperty("CalcLngWheelSlip.Computed.LngWheelSlip_RR", this.GetType(), 0);

            pluginManager.AddProperty("CalcLngWheelSlip.Computed.TyreDiameter_FL", this.GetType(), "-");
            pluginManager.AddProperty("CalcLngWheelSlip.Computed.TyreDiameter_FR", this.GetType(), "-");
            pluginManager.AddProperty("CalcLngWheelSlip.Computed.TyreDiameter_RL", this.GetType(), "-");
            pluginManager.AddProperty("CalcLngWheelSlip.Computed.TyreDiameter_RR", this.GetType(), "-");

            pluginManager.AddProperty("CalcLngWheelSlip.TyreDiameterDetNeeded", this.GetType(), true);
            pluginManager.AddProperty("CalcLngWheelSlip.TyreDiameterDetLimitOK.Speed", this.GetType(), false);
            pluginManager.AddProperty("CalcLngWheelSlip.TyreDiameterDetLimitOK.Throttle", this.GetType(), false);
            pluginManager.AddProperty("CalcLngWheelSlip.TyreDiameterDetLimitOK.Brake", this.GetType(), false);
            pluginManager.AddProperty("CalcLngWheelSlip.TyreDiameterDetLimitOK.LateralVel", this.GetType(), false);

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

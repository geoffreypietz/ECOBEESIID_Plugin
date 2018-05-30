using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using HomeSeerAPI;
using Scheduler;
using Scheduler.Classes;
using System.Reflection;
using System.Text;


using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using HSPI_Ecobee_Thermostat_Plugin.Models;
using static HomeSeerAPI.DeviceTypeInfo_m.DeviceTypeInfo;
using Newtonsoft.Json;
using System.Linq;

namespace HSPI_Ecobee_Thermostat_Plugin
{

    static class Util
    {

        // interface status
        // for InterfaceStatus function call
        public const int ERR_NONE = 0;
        public const int ERR_SEND = 1;

        public const int ERR_INIT = 2;
        public static HomeSeerAPI.IHSApplication hs;
        public static HomeSeerAPI.IAppCallbackAPI callback;
        public const string IFACE_NAME = "Ecobee Thermostat Plugin";
        //public const string IFACE_NAME = "Sample Plugin";
        // set when SupportMultipleInstances is TRUE
        public static string Instance = "";
        public static string gEXEPath = "";

        public static bool gGlobalTempScaleF = true;
        public static SortedList colTrigs_Sync;
        public static SortedList colTrigs;
        public static SortedList colActs_Sync;

        public static SortedList colActs;





        public static bool StringIsNullOrEmpty(ref string s)
        {
            if (string.IsNullOrEmpty(s))
                return true;
            return string.IsNullOrEmpty(s.Trim());
        }

        public enum LogType
        {
            LOG_TYPE_INFO = 0,
            LOG_TYPE_ERROR = 1,
            LOG_TYPE_WARNING = 2
        }

        public static void Log(string msg, LogType logType)
        {
            try
            {
                if (msg == null)
                    msg = "";
                if (!Enum.IsDefined(typeof(LogType), logType))
                {
                    logType = Util.LogType.LOG_TYPE_ERROR;
                }
                Console.WriteLine(msg);
                switch (logType)
                {
                    case LogType.LOG_TYPE_ERROR:
                        hs.WriteLog(Util.IFACE_NAME + " Error", msg);
                        break;
                    case LogType.LOG_TYPE_WARNING:
                        hs.WriteLog(Util.IFACE_NAME + " Warning", msg);
                        break;
                    case LogType.LOG_TYPE_INFO:
                        hs.WriteLog(Util.IFACE_NAME, msg);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in LOG of " + Util.IFACE_NAME + ": " + ex.Message);
            }

        }


        public static int MyDevice = -1;

        public static int MyTempDevice = -1;





        static internal List<DeviceDataPoint> Get_Device_List(List<DeviceDataPoint> deviceList)
        {
            // Gets relevant devices from HomeSeer
            DeviceClass dv = new DeviceClass();

            try
            {
                Scheduler.Classes.clsDeviceEnumeration EN = default(Scheduler.Classes.clsDeviceEnumeration);
                EN = (Scheduler.Classes.clsDeviceEnumeration)Util.hs.GetDeviceEnumerator();
                if (EN == null)
                    throw new Exception(IFACE_NAME + " failed to get a device enumerator from HomeSeer.");
                int dvRef;

                do
                {
                    dv = EN.GetNext();
                    if (dv == null)
                        continue;
                    if (dv.get_Interface(null) != IFACE_NAME)
                        continue;
                    dvRef = dv.get_Ref(null);
                    
                    var ddp = new DeviceDataPoint(dvRef, dv);
                    deviceList.Add(ddp);

                } while (!(EN.Finished));
            }
            catch (Exception ex)
            {
                Log("Exception in Get_Device_List: " + ex.Message, LogType.LOG_TYPE_ERROR);
            }

            return deviceList;
        }

        static internal void Update_ThermostatDevice(Thermostat thermostat, DeviceDataPoint ddPoint, EcobeeConnection ecobee)
        {
            string name;
            string id = GetDeviceKeys(ddPoint.device, out name);

            ThermostatEvent thermEvent = null;
            var eventExists = false;
            if (thermostat.events != null || thermostat.events.Count() != 0)
            {
                foreach (var tEvent in thermostat.events)
                {
                    if (tEvent.running)
                    {
                        thermEvent = tEvent;
                        eventExists = true;
                    }
                }
            }

            switch (name)
            {
                case "Ambient Temperature":
                    {
                        hs.SetDeviceValueByRef(ddPoint.dvRef, Math.Ceiling(Convert.ToDouble(thermostat.runtime.actualTemperature) / 10), true);
                        ddPoint.device.set_ScaleText(hs, thermostat.settings.useCelsius ? "C" : "F");

                        break;
                    }
                case "Humidity":
                    {
                        hs.SetDeviceValueByRef(ddPoint.dvRef, thermostat.runtime.actualHumidity, true);
                        break;
                    }
                case "Cool Range":
                    {
                        var range = thermostat.settings.coolRangeLow/10 + " - " + thermostat.settings.coolRangeHigh/10 + " " + getTemperatureUnits(thermostat.settings.useCelsius);
                        hs.SetDeviceString(ddPoint.dvRef, range, true);
                        break;
                    }
                case "Heat Range":
                    {
                        var range = thermostat.settings.heatRangeLow/10 + " - " + thermostat.settings.heatRangeHigh/10 + " " + getTemperatureUnits(thermostat.settings.useCelsius);
                        hs.SetDeviceString(ddPoint.dvRef, range, true);
                        break;
                    }
                case "HVAC Mode":
                    {
                        switch (thermostat.settings.hvac_mode)
                        {
                            case "off":
                                hs.SetDeviceValueByRef(ddPoint.dvRef, 0, true);
                                break;
                            case "auto":
                                hs.SetDeviceValueByRef(ddPoint.dvRef, 1, true);
                                break;
                            case "cool":
                                hs.SetDeviceValueByRef(ddPoint.dvRef, 2, true);
                                break;
                            case "heat":
                                hs.SetDeviceValueByRef(ddPoint.dvRef, 3, true);
                                break;
                        }
                        break;
                    }
                case "HVAC Status":
                    {
                        hs.SetDeviceString(ddPoint.dvRef, thermostat.settings.hvac_mode, true);
                        break;
                    }

                case "Fan Mode":
                    {
                        switch (thermostat.runtime.desiredFanMode)
                        {
                            case null:
                            case "off":
                                hs.SetDeviceValueByRef(ddPoint.dvRef, 0, true);
                                break;
                            case "on":
                                hs.SetDeviceValueByRef(ddPoint.dvRef, 1, true);
                                break;
                            case "auto":
                                hs.SetDeviceValueByRef(ddPoint.dvRef, 2, true);
                                break;
                        }

                        break;
                    }
                case "Fan Status":
                    {

                        hs.SetDeviceString(ddPoint.dvRef, thermostat.runtime.desiredFanMode, true);

                        break;
                    }
                case "Deadband":
                    {
                        hs.SetDeviceValueByRef(ddPoint.dvRef, Math.Ceiling(Convert.ToDouble((thermostat.runtime.desiredCool - thermostat.runtime.desiredHeat)) / 10), true);
                        ddPoint.device.set_ScaleText(hs, thermostat.settings.useCelsius ? "C" : "F");

                        break;
                    }
                case "Target Temperature High":
                    {

                        if (eventExists)
                        {
                            hs.SetDeviceValueByRef(ddPoint.dvRef, Math.Ceiling(Convert.ToDouble(thermEvent.coolHoldTemp) / 10), true);
                        }
                        else
                        {
                            hs.SetDeviceValueByRef(ddPoint.dvRef, Math.Ceiling(Convert.ToDouble(thermostat.runtime.desiredCool) / 10), true);
                        }
                        ddPoint.device.set_ScaleText(hs, thermostat.settings.useCelsius ? "C" : "F");

                        break;
                    }
                case "Target Temperature Low":
                    {

                        if (eventExists)
                        {
                            hs.SetDeviceValueByRef(ddPoint.dvRef, Math.Ceiling(Convert.ToDouble(thermEvent.heatHoldTemp) / 10), true);
                        }
                        else
                        {
                            hs.SetDeviceValueByRef(ddPoint.dvRef, Math.Ceiling(Convert.ToDouble(thermostat.runtime.desiredHeat) / 10), true);
                        }
                        ddPoint.device.set_ScaleText(hs, thermostat.settings.useCelsius ? "C" : "F");

                        break;
                    }

                case "Current Program":
                    {
                        if (eventExists)
                        {
                            switch (thermEvent.type)
                            {
                                case "hold":
                                    hs.SetDeviceValueByRef(ddPoint.dvRef, 0, true);
                                    break;
                                case "demandResponse":
                                    hs.SetDeviceValueByRef(ddPoint.dvRef, 1, true);
                                    break;
                                case "sensor":
                                    hs.SetDeviceValueByRef(ddPoint.dvRef, 2, true);
                                    break;
                                case "switchOccupancy":
                                    hs.SetDeviceValueByRef(ddPoint.dvRef, 3, true);
                                    break;
                                case "vacation":
                                    hs.SetDeviceValueByRef(ddPoint.dvRef, 4, true);
                                    break;
                                case "quickSave":
                                    hs.SetDeviceValueByRef(ddPoint.dvRef, 5, true);
                                    break;
                                case "today":
                                    hs.SetDeviceValueByRef(ddPoint.dvRef, 6, true);
                                    break;
                                case "template":
                                    hs.SetDeviceValueByRef(ddPoint.dvRef, 7, true);
                                    break;
                            }
                        }
                        else
                        {
                            hs.SetDeviceValueByRef(ddPoint.dvRef, 0, true);
                        }
                        break;
                    }
                case "Occupancy":
                    {
                        if (eventExists)
                        {
                            if (thermostat.events[0].isOccupied)
                            {
                                hs.SetDeviceValueByRef(ddPoint.dvRef, 1, true);
                            }
                            else
                            {
                                hs.SetDeviceValueByRef(ddPoint.dvRef, 0, true);
                            }

                        }
                        else
                        {
                            hs.SetDeviceValueByRef(ddPoint.dvRef, 2, true);
                        }

                        break;

                    }
            }
        }

        static internal void Update_RemoteDevice(Thermostat thermostat, SensorCapabilities capability, DeviceDataPoint ddPoint, EcobeeConnection ecobee)
        {
            string name;
            string id = GetDeviceKeys(ddPoint.device, out name);
            

            if(name.Equals("Temperature Sensor"))
            {
                if (capability.value != "unknown")
                {
                    var temp = Int16.Parse(capability.value) / 10;
                    hs.SetDeviceValueByRef(ddPoint.dvRef, temp, true); 
                }
                else
                {
                    hs.SetDeviceString(ddPoint.dvRef, "unknown", true);
                }
                ddPoint.device.set_ScaleText(hs, thermostat.settings.useCelsius ? "C" : "F");

            }
            if (name.Equals("Occupancy Sensor"))
            {
                if (capability.value.Equals("true"))
                {
                    hs.SetDeviceValueByRef(ddPoint.dvRef, 1, true);
                }
                else
                {
                    hs.SetDeviceValueByRef(ddPoint.dvRef, 0, true);
                }
            }
        }

        static internal void Find_Create_Devices(EcobeeConnection ecobee)
        {
            List<DeviceDataPoint> deviceList = new List<DeviceDataPoint>();

            using (var ecobeeData = ecobee.getEcobeeData())
            {
                deviceList = Get_Device_List(deviceList);

                Find_Create_Thermostats(ecobeeData, deviceList, ecobee); 
            }
        }

        static internal void Find_Create_Thermostats(EcobeeData ecobeeData, List<DeviceDataPoint> deviceList, EcobeeConnection ecobee)
        {
            bool create = false;
            bool associates = false;
            List<string> tStrings = getThermostatStrings();

            try
            {
                foreach (var thermostat in ecobeeData.thermostatList)
                {
                    foreach (var tString in tStrings)
                    {
                        create = Thermostat_Devices(tString, thermostat, deviceList, ecobee);
                        if (create) // True if a device was created
                            associates = true;
                    }
                    foreach (var remote in thermostat.remoteSensors)
                    {
                        if (remote.inUse && remote.name != thermostat.name)
                        {
                            foreach (var capability in remote.capabilityList)
                            {
                                if (capability.type.Equals("temperature") || capability.type.Equals("occupancy"))
                                {
                                    create = Remote_Devices(" Sensor", thermostat, remote, capability, deviceList, ecobee);
                                    if (create) // True if a device was created
                                        associates = true;
                                }
                            } 
                        }
                    }
                }
                if (associates)
                {
                    SetAssociatedDevices("Thermostats");
                }
            }
            catch (Exception ex)
            {
                Log("Exception in Find_Create_Thermostats: " + ex.Message, LogType.LOG_TYPE_ERROR);
                System.IO.File.WriteAllText(@"Data/HSPI_Ecobee_Thermostat_Plugin/debug.txt", ex.ToString());
            }
        }

        static internal bool Thermostat_Devices(string tString, Thermostat thermostat, List<DeviceDataPoint> deviceList, EcobeeConnection ecobee)
        {
            string name;
            string id;

            foreach (var ddPoint in deviceList)
            {
                id = GetDeviceKeys(ddPoint.device, out name);
                if (id == thermostat.identifier && name == tString)
                {
                    Update_ThermostatDevice(thermostat, ddPoint, ecobee);
                    return false;
                }
            }

            DeviceClass dv = new DeviceClass();
            dv = GenericHomeSeerDevice(dv, tString, thermostat.name, thermostat.identifier);
            var dvRef = dv.get_Ref(hs);
            id = GetDeviceKeys(dv, out name);
            switch (name)
            {
                case "Root":
                    {
                        DeviceTypeInfo_m.DeviceTypeInfo dt = new DeviceTypeInfo_m.DeviceTypeInfo();
                        dt.Device_API = DeviceTypeInfo_m.DeviceTypeInfo.eDeviceAPI.Thermostat;
                        dt.Device_Type = (int)DeviceTypeInfo_m.DeviceTypeInfo.eDeviceType_Thermostat.Root;
                        dv.set_DeviceType_Set(hs, dt); dv.set_Relationship(hs, Enums.eRelationship.Parent_Root);
                        dv.set_Device_Type_String(hs, "Ecobee Root Thermostat");
                        dv.MISC_Set(hs, Enums.dvMISC.STATUS_ONLY);

                        VSVGPairs.VSPair SPair = default(VSVGPairs.VSPair);
                        SPair = new VSVGPairs.VSPair(ePairStatusControl.Status);
                        SPair.PairType = VSVGPairs.VSVGPairType.SingleValue;
                        SPair.Value = 0;
                        SPair.Status = "Thermostat Root";
                        hs.DeviceVSP_AddPair(dvRef, SPair);

                        VSVGPairs.VGPair GPair = new VSVGPairs.VGPair();
                        GPair.PairType = VSVGPairs.VSVGPairType.SingleValue;
                        GPair.Set_Value = 0;
                        GPair.Graphic = "/images/HomeSeer/contemporary/ok.png";
                        hs.DeviceVGP_AddPair(dvRef, GPair);

                        break;
                    }
                case "HVAC Mode":
                    {
                        dv.MISC_Set(hs, Enums.dvMISC.SHOW_VALUES);
                        DeviceTypeInfo_m.DeviceTypeInfo dt = new DeviceTypeInfo_m.DeviceTypeInfo();
                        dt.Device_API = DeviceTypeInfo_m.DeviceTypeInfo.eDeviceAPI.Thermostat;
                        dt.Device_Type = (int)DeviceTypeInfo_m.DeviceTypeInfo.eDeviceType_Thermostat.Mode_Set;
                        dt.Device_SubType = 0;
                        dv.set_DeviceType_Set(hs, dt);

                        VSVGPairs.VSPair SPair = default(VSVGPairs.VSPair);
                        SPair = new VSVGPairs.VSPair(ePairStatusControl.Both);
                        SPair.PairType = VSVGPairs.VSVGPairType.SingleValue;
                        SPair.Render = Enums.CAPIControlType.Button;
                        SPair.Value = 0;
                        SPair.Status = "Off";
                        SPair.Render_Location.Row = 1;
                        SPair.Render_Location.Column = 1;
                        SPair.ControlUse = ePairControlUse._ThermModeOff;
                        hs.DeviceVSP_AddPair(dvRef, SPair);

                        VSVGPairs.VGPair GPair = new VSVGPairs.VGPair();
                        GPair.PairType = VSVGPairs.VSVGPairType.SingleValue;
                        GPair.Set_Value = 0;
                        GPair.Graphic = "/images/HomeSeer/contemporary/modeoff.png";
                        hs.DeviceVGP_AddPair(dvRef, GPair);

                        SPair.Value = 1;
                        SPair.Status = "Auto";
                        SPair.Render_Location.Row = 1;
                        SPair.Render_Location.Column = 2;
                        SPair.ControlUse = ePairControlUse._ThermModeAuto;
                        hs.DeviceVSP_AddPair(dvRef, SPair);

                        GPair.PairType = VSVGPairs.VSVGPairType.SingleValue;
                        GPair.Set_Value = 1;
                        GPair.Graphic = "/images/HomeSeer/contemporary/auto-mode.png";
                        hs.DeviceVGP_AddPair(dvRef, GPair);

                        SPair.Value = 2;
                        SPair.Status = "Cool";
                        SPair.Render_Location.Row = 1;
                        SPair.Render_Location.Column = 2;
                        SPair.ControlUse = ePairControlUse._ThermModeCool;
                        hs.DeviceVSP_AddPair(dvRef, SPair);

                        GPair.PairType = VSVGPairs.VSVGPairType.SingleValue;
                        GPair.Set_Value = 2;
                        GPair.Graphic = "/images/HomeSeer/contemporary/cooling.png";
                        hs.DeviceVGP_AddPair(dvRef, GPair);

                        SPair.Value = 3;
                        SPair.Status = "Heat";
                        SPair.Render_Location.Row = 1;
                        SPair.Render_Location.Column = 2;
                        SPair.ControlUse = ePairControlUse._ThermModeHeat;
                        hs.DeviceVSP_AddPair(dvRef, SPair);

                        GPair.PairType = VSVGPairs.VSVGPairType.SingleValue;
                        GPair.Set_Value = 3;
                        GPair.Graphic = "/images/HomeSeer/contemporary/heating.png";
                        hs.DeviceVGP_AddPair(dvRef, GPair);

                        break;
                    }
                case "Fan Mode":
                    {
                        dv.MISC_Set(hs, Enums.dvMISC.SHOW_VALUES);
                        DeviceTypeInfo_m.DeviceTypeInfo dt = new DeviceTypeInfo_m.DeviceTypeInfo();
                        dt.Device_API = DeviceTypeInfo_m.DeviceTypeInfo.eDeviceAPI.Thermostat;
                        dt.Device_Type = (int)DeviceTypeInfo_m.DeviceTypeInfo.eDeviceType_Thermostat.Fan_Mode_Set;
                        dt.Device_SubType = 0;
                        dv.set_DeviceType_Set(hs, dt);

                        VSVGPairs.VSPair SPair = default(VSVGPairs.VSPair);
                        SPair = new VSVGPairs.VSPair(ePairStatusControl.Both);
                        SPair.PairType = VSVGPairs.VSVGPairType.SingleValue;
                        SPair.Render = Enums.CAPIControlType.Button;
                        SPair.Value = 0;
                        SPair.Status = "Off";
                        SPair.Render_Location.Row = 1;
                        SPair.Render_Location.Column = 1;
                        hs.DeviceVSP_AddPair(dvRef, SPair);

                        VSVGPairs.VGPair GPair = new VSVGPairs.VGPair();
                        GPair.PairType = VSVGPairs.VSVGPairType.SingleValue;
                        GPair.Set_Value = 0;
                        GPair.Graphic = "/images/HomeSeer/contemporary/fan-state-off.png";
                        hs.DeviceVGP_AddPair(dvRef, GPair);

                        SPair.Value = 1;
                        SPair.Status = "On";
                        SPair.Render_Location.Row = 1;
                        SPair.Render_Location.Column = 2;
                        SPair.ControlUse = ePairControlUse._ThermFanOn;
                        hs.DeviceVSP_AddPair(dvRef, SPair);

                        GPair.PairType = VSVGPairs.VSVGPairType.SingleValue;
                        GPair.Set_Value = 1;
                        GPair.Graphic = "/images/HomeSeer/contemporary/fan-on.png";
                        hs.DeviceVGP_AddPair(dvRef, GPair);

                        SPair.Value = 2;
                        SPair.Status = "Auto";
                        SPair.Render_Location.Row = 1;
                        SPair.Render_Location.Column = 2;
                        SPair.ControlUse = ePairControlUse._ThermFanAuto;
                        hs.DeviceVSP_AddPair(dvRef, SPair);

                        GPair.PairType = VSVGPairs.VSVGPairType.SingleValue;
                        GPair.Set_Value = 2;
                        GPair.Graphic = "/images/HomeSeer/contemporary/fan-auto.png";
                        hs.DeviceVGP_AddPair(dvRef, GPair);
                        break;

                    }
                case "Deadband":
                case "Target Temperature High":
                case "Target Temperature Low":
                case "Ambient Temperature":
                    {
                        VSVGPairs.VSPair SPair = default(VSVGPairs.VSPair);
                        SPair = new VSVGPairs.VSPair(ePairStatusControl.Status);
                        SPair.PairType = VSVGPairs.VSVGPairType.Range;
                        SPair.RangeStart = -150;
                        SPair.RangeEnd = 100;
                        SPair.RangeStatusSuffix = " °" + VSVGPairs.VSPair.ScaleReplace;
                        SPair.HasScale = true;
                        hs.DeviceVSP_AddPair(dvRef, SPair);

                        if (name.Equals("Target Temperature High") || name.Equals("Target Temperature Low"))
                        {
                            dv.MISC_Set(hs, Enums.dvMISC.SHOW_VALUES);

                            SPair = new VSVGPairs.VSPair(HomeSeerAPI.ePairStatusControl.Control);
                            SPair.PairType = VSVGPairs.VSVGPairType.Range;
                            SPair.Render = Enums.CAPIControlType.TextBox_Number;
                            SPair.Render_Location.Row = 1;
                            SPair.Render_Location.Column = 1;
                            SPair.Status = "Enter target:";
                            SPair.RangeStart = 0;
                            SPair.RangeEnd = 100;
                            if (name.Equals("Target Temperature Low"))
                            {
                                SPair.ControlUse = ePairControlUse._HeatSetPoint;
                                DeviceTypeInfo_m.DeviceTypeInfo dt = new DeviceTypeInfo_m.DeviceTypeInfo();
                                dt.Device_API = DeviceTypeInfo_m.DeviceTypeInfo.eDeviceAPI.Thermostat;
                                dt.Device_Type = (int)DeviceTypeInfo_m.DeviceTypeInfo.eDeviceType_Thermostat.Setpoint;
                                dt.Device_SubType = (int)DeviceTypeInfo_m.DeviceTypeInfo.eDeviceSubType_Setpoint.Heating_1;
                                dv.set_DeviceType_Set(hs, dt);
                            }
                            else if (name.Equals("Target Temperature High"))
                            {
                                SPair.ControlUse = ePairControlUse._CoolSetPoint;

                                DeviceTypeInfo_m.DeviceTypeInfo dt = new DeviceTypeInfo_m.DeviceTypeInfo();
                                dt.Device_API = DeviceTypeInfo_m.DeviceTypeInfo.eDeviceAPI.Thermostat;
                                dt.Device_Type = (int)DeviceTypeInfo_m.DeviceTypeInfo.eDeviceType_Thermostat.Setpoint;
                                dt.Device_SubType = (int)DeviceTypeInfo_m.DeviceTypeInfo.eDeviceSubType_Setpoint.Cooling_1;
                                dv.set_DeviceType_Set(hs, dt);
                            }
                            hs.DeviceVSP_AddPair(dvRef, SPair);
                        }
                        else if (name.Equals("Ambient Temperature"))
                        {
                            DeviceTypeInfo_m.DeviceTypeInfo dt = new DeviceTypeInfo_m.DeviceTypeInfo();
                            dt.Device_API = DeviceTypeInfo_m.DeviceTypeInfo.eDeviceAPI.Thermostat;
                            dt.Device_Type = (int)DeviceTypeInfo_m.DeviceTypeInfo.eDeviceType_Thermostat.Temperature;
                            dv.set_DeviceType_Set(hs, dt);
                        }

                        VSVGPairs.VGPair GPair = new VSVGPairs.VGPair();
                        GPair.PairType = VSVGPairs.VSVGPairType.Range;
                        GPair.RangeStart = -100;
                        GPair.RangeEnd = 150;
                        GPair.Graphic = "/images/HomeSeer/contemporary/Thermometer-70.png";
                        hs.DeviceVGP_AddPair(dvRef, GPair);

                        break;
                    }
                case "Current Program":
                    {
                        dv.MISC_Set(hs, Enums.dvMISC.SHOW_VALUES);

                        VSVGPairs.VSPair SPair = default(VSVGPairs.VSPair);
                        SPair = new VSVGPairs.VSPair(ePairStatusControl.Status);
                        SPair.PairType = VSVGPairs.VSVGPairType.SingleValue;
                        SPair.Value = 0;
                        SPair.Status = "Hold";
                        hs.DeviceVSP_AddPair(dvRef, SPair);

                        SPair.Value = 1;
                        SPair.Status = "Demand Response";
                        hs.DeviceVSP_AddPair(dvRef, SPair);

                        SPair.Value = 2;
                        SPair.Status = "Sensor";
                        hs.DeviceVSP_AddPair(dvRef, SPair);

                        SPair.Value = 3;
                        SPair.Status = "Switch Occupancy";
                        hs.DeviceVSP_AddPair(dvRef, SPair);
                        SPair.Value = 4;
                        SPair.Status = "Vacation";
                        hs.DeviceVSP_AddPair(dvRef, SPair);
                        SPair.Value = 5;
                        SPair.Status = "Quick Save";
                        hs.DeviceVSP_AddPair(dvRef, SPair);
                        SPair.Value = 6;
                        SPair.Status = "Today";
                        hs.DeviceVSP_AddPair(dvRef, SPair);
                        SPair.Value = 7;
                        SPair.Status = "Template";
                        hs.DeviceVSP_AddPair(dvRef, SPair);

                        VSVGPairs.VGPair GPair = new VSVGPairs.VGPair();
                        GPair.PairType = VSVGPairs.VSVGPairType.Range;
                        GPair.RangeStart = 0;
                        GPair.RangeEnd = 4;
                        GPair.Graphic = "/images/HomeSeer/contemporary/home.png";
                        hs.DeviceVGP_AddPair(dvRef, GPair);

                        break;
                    }
                case "HVAC Status":
                case "Fan Status":
                case "Cool Range":
                case "Heat Range":
                    {
                        VSVGPairs.VGPair GPair = new VSVGPairs.VGPair();
                        GPair.PairType = VSVGPairs.VSVGPairType.SingleValue;
                        GPair.Graphic = "/images/HomeSeer/contemporary/alarmheartbeat.png";
                        hs.DeviceVGP_AddPair(dvRef, GPair);

                        break;
                    }

                case "Humidity":
                    {
                        DeviceTypeInfo_m.DeviceTypeInfo dt = new DeviceTypeInfo_m.DeviceTypeInfo();
                        dt.Device_API = DeviceTypeInfo_m.DeviceTypeInfo.eDeviceAPI.Thermostat;
                        dt.Device_Type = (int)DeviceTypeInfo_m.DeviceTypeInfo.eDeviceType_Thermostat.Temperature;
                        dt.Device_SubType = (int)DeviceTypeInfo_m.DeviceTypeInfo.eDeviceSubType_Temperature.Humidity;
                        dv.set_DeviceType_Set(hs, dt);

                        VSVGPairs.VSPair SPair = default(VSVGPairs.VSPair);
                        SPair = new VSVGPairs.VSPair(ePairStatusControl.Status);
                        SPair.PairType = VSVGPairs.VSVGPairType.Range;
                        SPair.RangeStart = 0;
                        SPair.RangeEnd = 100;
                        SPair.RangeStatusSuffix = " %";
                        hs.DeviceVSP_AddPair(dvRef, SPair);

                        VSVGPairs.VGPair GPair = new VSVGPairs.VGPair();
                        GPair.PairType = VSVGPairs.VSVGPairType.Range;
                        GPair.RangeStart = 0;
                        GPair.RangeEnd = 100;
                        GPair.Graphic = "/images/HomeSeer/contemporary/water.gif";
                        hs.DeviceVGP_AddPair(dvRef, GPair);

                        break;
                    }
                case "Occupancy":
                    {
                        VSVGPairs.VSPair SPair = default(VSVGPairs.VSPair);
                        SPair = new VSVGPairs.VSPair(ePairStatusControl.Status);
                        SPair.PairType = VSVGPairs.VSVGPairType.SingleValue;
                        SPair.Value = 0;
                        SPair.Status = "Unoccupied";
                        hs.DeviceVSP_AddPair(dvRef, SPair);

                        SPair.Value = 1;
                        SPair.Status = "Occupied";
                        hs.DeviceVSP_AddPair(dvRef, SPair);

                        SPair.Value = 2;
                        SPair.Status = "No Event";
                        hs.DeviceVSP_AddPair(dvRef, SPair);

                        VSVGPairs.VGPair GPair = new VSVGPairs.VGPair();
                        GPair.PairType = VSVGPairs.VSVGPairType.SingleValue;
                        GPair.Set_Value = 0;
                        GPair.Graphic = "/images/HomeSeer/contemporary/away.png";
                        hs.DeviceVGP_AddPair(dvRef, GPair);

                        GPair.Set_Value = 1;
                        GPair.Graphic = "/images/HomeSeer/contemporary/home.png";
                        hs.DeviceVGP_AddPair(dvRef, GPair);

                        GPair.Set_Value = 2;
                        GPair.Graphic = "/images/HomeSeer/contemporary/userclosing.png";
                        hs.DeviceVGP_AddPair(dvRef, GPair);

                        break;
                    }
            }

            return true;
        }

        static internal bool Remote_Devices(string tString, Thermostat thermostat, RemoteSensors remote, SensorCapabilities capability, List<DeviceDataPoint> deviceList, EcobeeConnection ecobee)
        {
            var sensorType = "";
            if (capability.type.Equals("temperature"))
            {
                sensorType = "Temperature";
            }
            if (capability.type.Equals("occupancy"))
            {
                sensorType = "Occupancy";
            }

            string name;
            string id;

            foreach (var ddPoint in deviceList)
            {
                id = GetDeviceKeys(ddPoint.device, out name);

                if(id == (thermostat.identifier + remote.code) && name == (sensorType + tString))
                {
                    Update_RemoteDevice(thermostat, capability, ddPoint, ecobee);
                    return false;
                }
            }

            

            DeviceClass dv = new DeviceClass();
            dv = GenericHomeSeerDevice(dv, sensorType + tString, remote.name, thermostat.identifier + remote.code);
            var dvRef = dv.get_Ref(hs);
            id = GetDeviceKeys(dv, out name);

            switch (name)
            {
                case "Temperature Sensor":
                    {
                        VSVGPairs.VSPair SPair = default(VSVGPairs.VSPair);
                        SPair = new VSVGPairs.VSPair(ePairStatusControl.Status);
                        SPair.PairType = VSVGPairs.VSVGPairType.Range;
                        SPair.RangeStart = -500;
                        SPair.RangeEnd = 500;
                        SPair.RangeStatusSuffix = " " + getTemperatureUnits(thermostat.settings.useCelsius);
                        hs.DeviceVSP_AddPair(dvRef, SPair);

                        VSVGPairs.VGPair GPair = new VSVGPairs.VGPair();
                        GPair.PairType = VSVGPairs.VSVGPairType.Range;
                        GPair.RangeStart = -500;
                        GPair.RangeEnd = 500;
                        GPair.Graphic = "/images/HomeSeer/contemporary/Thermometer-70.png";
                        hs.DeviceVGP_AddPair(dvRef, GPair);
                        break;
                    }
                case "Occupancy Sensor":
                    {
                        VSVGPairs.VSPair SPair = default(VSVGPairs.VSPair);
                        SPair = new VSVGPairs.VSPair(ePairStatusControl.Status);
                        SPair.PairType = VSVGPairs.VSVGPairType.SingleValue;
                        SPair.Value = 0;
                        SPair.Status = "Unoccupied";
                        hs.DeviceVSP_AddPair(dvRef, SPair);

                        SPair.Value = 1;
                        SPair.Status = "Occupied";
                        hs.DeviceVSP_AddPair(dvRef, SPair);

                        VSVGPairs.VGPair GPair = new VSVGPairs.VGPair();
                        GPair.PairType = VSVGPairs.VSVGPairType.SingleValue;
                        GPair.Set_Value = 0;
                        GPair.Graphic = "/images/HomeSeer/contemporary/away.png";
                        hs.DeviceVGP_AddPair(dvRef, GPair);

                        GPair.Set_Value = 1;
                        GPair.Graphic = "/images/HomeSeer/contemporary/home.png";
                        hs.DeviceVGP_AddPair(dvRef, GPair);
                        break;
                    }
            }

            return true;
        }

        static internal string GetDeviceKeys(DeviceClass dev, out string name)
        {
            string id = "";
            name = "";
            PlugExtraData.clsPlugExtraData pData = dev.get_PlugExtraData_Get(hs);
            if (pData != null)
            {
                id = (string)pData.GetNamed("id");
                name = (string)pData.GetNamed("name");
            }
            return id;
        }

        static internal void SetDeviceKeys(DeviceClass dev, string id, string name)
        {
            PlugExtraData.clsPlugExtraData pData = dev.get_PlugExtraData_Get(hs);
            if (pData == null)
                pData = new PlugExtraData.clsPlugExtraData();
            pData.AddNamed("id", id);
            pData.AddNamed("name", name);
            dev.set_PlugExtraData_Set(hs, pData);
        }

        static internal DeviceClass GenericHomeSeerDevice(DeviceClass dv, string dvName, string dvName_long, string device_id)
        {
            int dvRef;
            Log("Creating Device: " + dvName_long + " " + dvName, LogType.LOG_TYPE_INFO);
            var DT = new DeviceTypeInfo_m.DeviceTypeInfo();
            DT.Device_API = DeviceTypeInfo_m.DeviceTypeInfo.eDeviceAPI.Plug_In;
            if (dvName.Contains("Root"))
            {
                DT.Device_Type = 99;
            }
          
            dvRef = hs.NewDeviceRef(dvName_long + " " + dvName);
            dv = (DeviceClass)hs.GetDeviceByRef(dvRef);
            dv.set_Address(hs, "");
            SetDeviceKeys(dv, device_id, dvName);
            //dv.set_Code(hs, device_id + "-" + dvName_long + "-" + dvName);
            dv.set_Location(hs, "Thermostat");
            dv.set_Location2(hs, "Ecobee");
            dv.set_Interface(hs, IFACE_NAME);
            dv.set_Status_Support(hs, true);
            dv.set_Can_Dim(hs, false);
            dv.MISC_Set(hs, Enums.dvMISC.NO_LOG);
            dv.set_DeviceType_Set(hs, DT);
            dv.set_Relationship(hs, Enums.eRelationship.Child);
            dv.set_Device_Type_String(hs, "Ecobee Child Device");
            return dv;
        }

        private static void Default_VS_Pairs_AddUpdateUtil(int dvRef, VSVGPairs.VSPair Pair)
        {
            if (Pair == null)
                return;
            if (dvRef < 1)
                return;
            if (!hs.DeviceExistsRef(dvRef))
                return;

            VSVGPairs.VSPair Existing = null;

            // The purpose of this procedure is to add the protected, default VS/VG pairs WITHOUT overwriting any user added
            //   pairs unless absolutely necessary (because they conflict).

            try
            {
                Existing = hs.DeviceVSP_Get(dvRef, Pair.Value, Pair.ControlStatus);
                //VSPairs.GetPairByValue(Pair.Value, Pair.ControlStatus)


                if (Existing != null)
                {
                    // This is unprotected, so it is a user's value/status pair.
                    if (Existing.ControlStatus == HomeSeerAPI.ePairStatusControl.Both & Pair.ControlStatus != HomeSeerAPI.ePairStatusControl.Both)
                    {
                        // The existing one is for BOTH, so try changing it to the opposite of what we are adding and then add it.
                        if (Pair.ControlStatus == HomeSeerAPI.ePairStatusControl.Status)
                        {
                            if (!hs.DeviceVSP_ChangePair(dvRef, Existing, HomeSeerAPI.ePairStatusControl.Control))
                            {
                                hs.DeviceVSP_ClearBoth(dvRef, Pair.Value);
                                hs.DeviceVSP_AddPair(dvRef, Pair);
                            }
                            else
                            {
                                hs.DeviceVSP_AddPair(dvRef, Pair);
                            }
                        }
                        else
                        {
                            if (!hs.DeviceVSP_ChangePair(dvRef, Existing, HomeSeerAPI.ePairStatusControl.Status))
                            {
                                hs.DeviceVSP_ClearBoth(dvRef, Pair.Value);
                                hs.DeviceVSP_AddPair(dvRef, Pair);
                            }
                            else
                            {
                                hs.DeviceVSP_AddPair(dvRef, Pair);
                            }
                        }
                    }
                    else if (Existing.ControlStatus == HomeSeerAPI.ePairStatusControl.Control)
                    {
                        // There is an existing one that is STATUS or CONTROL - remove it if ours is protected.
                        hs.DeviceVSP_ClearControl(dvRef, Pair.Value);
                        hs.DeviceVSP_AddPair(dvRef, Pair);

                    }
                    else if (Existing.ControlStatus == HomeSeerAPI.ePairStatusControl.Status)
                    {
                        // There is an existing one that is STATUS or CONTROL - remove it if ours is protected.
                        hs.DeviceVSP_ClearStatus(dvRef, Pair.Value);
                        hs.DeviceVSP_AddPair(dvRef, Pair);

                    }

                }
                else
                {
                    // There is not a pair existing, so just add it.
                    hs.DeviceVSP_AddPair(dvRef, Pair);

                }


            }
            catch (Exception)
            {
            }


        }

        // This is called at the end of device creation
        // It works by first finding the Root device of the designated family (ie Device, Current Weather, Todays Forecast, Tomorrows Forecast)
        // Then, it finds the expected associates and adds them to the Root (eg CurrentWeatherRootDevice.AssociateDevice_Add(hs, WindSpeedRef#))
        static internal void SetAssociatedDevices(string family)
        {
            List<DeviceDataPoint> deviceList = new List<DeviceDataPoint>();
            string name;
            string id;

            deviceList = Get_Device_List(deviceList);

            foreach (var ddPoint in deviceList)
            {
                id = GetDeviceKeys(ddPoint.device, out name);

                if (name == "Root")   // True if the Device Root has been found
                {
                    ddPoint.device.AssociatedDevice_ClearAll(hs);

                    foreach (var aDDPoint in deviceList)
                    {
                        string aName;
                        string aId = GetDeviceKeys(aDDPoint.device, out aName);
                        if (aId.StartsWith(id) && aName != "Root")
                        {
                            ddPoint.device.AssociatedDevice_Add(hs, aDDPoint.dvRef);
                        }
                    }
                }

            }
        }

        static internal string getTemperatureUnits(bool units)
        {
            if (units)
            {
                return "°C";
            }
            else
            {
                return "°F";
            }
        }
        static internal string getDepthUnits(bool units)
        {
            if (units)
            {
                return "cm";
            }
            else
            {
                return "in";
            }
        }
        static internal string getSpeedUnits(bool units)
        {
            if (units)
            {
                return "km/h";
            }
            else
            {
                return "mph";
            }
        }
        // Gets the difference between event time(since) and now in minutes
        static internal double getTimeSince(double since)
        {
            since = getNowSinceEpoch() - since;
            since = Math.Round(since / 60);  // to minutes
            return since;
        }
        static internal double getNowSinceEpoch()
        {
            TimeSpan unixTime = DateTime.UtcNow - new DateTime(1970, 1, 1);
            double secondsSinceEpoch = (double)unixTime.TotalSeconds;
            return Math.Round(secondsSinceEpoch);
        }
        static internal List<string> getThermostatStrings()
        {
            var tStrings = new List<string>
            {
                "Root",
                "Deadband",
                "Target Temperature High",
                "Target Temperature Low",
                "Cool Range",
                "Heat Range",
                "HVAC Mode",
                "HVAC Status",
                "Fan Mode",
                "Fan Status",
                "Ambient Temperature",
                "Current Program",
                "Humidity",
                "Occupancy"
            };

            return tStrings;
        }
    }

}

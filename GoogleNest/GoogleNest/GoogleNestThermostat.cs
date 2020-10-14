using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GoogleNest
{
    public class GoogleNestThermostat : GoogleNestDevice
    {
        public delegate void Online(ushort state);
        public delegate void FanState(ushort state);
        public delegate void EcoModeState(ushort state);
        public delegate void TemperatureMode(SimplSharpString mode);
        public delegate void Humidity(ushort value);
        public delegate void CurrentMode(SimplSharpString mode);
        public delegate void EcoHeatSetPoint(ushort value);
        public delegate void EcoCoolSetPoint(ushort value);
        public delegate void CurrentSetPoint(ushort value);
        public delegate void CurrentTemperature(ushort value);
        public Online onOnline { get; set; }
        public FanState onFanState { get; set; }
        public EcoModeState onEcoModeState { get; set; }
        public TemperatureMode onTemperatureMode { get; set; }
        public Humidity onHumidity { get; set; }
        public CurrentMode onCurrentMode { get; set; }
        public EcoHeatSetPoint onEcoHeatSetPoint { get; set; }
        public EcoCoolSetPoint onEcoCoolSetPoint { get; set; }
        public CurrentSetPoint onCurrentSetPoint { get; set; }
        public CurrentTemperature onCurrentTemperature { get; set; }

        internal override void ParseData(JToken deviceData)
        {
            base.ParseData(deviceData);

            if (deviceData["traits"]["sdm.devices.traits.Connectivity"] != null)
            {
                if (deviceData["traits"]["sdm.devices.traits.Connectivity"].ToString().Contains("ONLINE"))
                {
                    if (onOnline != null)
                    {
                        onOnline(1);
                    }
                }
                else
                {
                    if (onOnline != null)
                    {
                        onOnline(0);
                    }
                }

                if (deviceData["traits"]["sdm.devices.traits.Temperature"] != null)
                {
                    if (onCurrentTemperature != null)
                    {
                        double temp = Math.Round(Convert.ToDouble(deviceData["traits"]["sdm.devices.traits.Temperature"]["ambientTemperatureCelsius"].ToString().Replace("\"", string.Empty)), 1);

                        onCurrentTemperature((ushort)(temp * 10));
                    }
                }
            }
        }
    }
}
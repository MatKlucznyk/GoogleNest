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
        public delegate void EcoModeState(SimplSharpString mode);
        public delegate void TemperatureMode(SimplSharpString mode);
        public delegate void Humidity(ushort value);
        public delegate void CurrentMode(SimplSharpString mode);
        public delegate void EcoHeatSetPoint(ushort value);
        public delegate void EcoCoolSetPoint(ushort value);
        public delegate void CurrentSetPoint(ushort value);
        public delegate void CurrentTemperature(ushort value, SimplSharpString sValue);
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

                        onCurrentTemperature((ushort)(temp * 10), deviceData["traits"]["sdm.devices.traits.Temperature"]["ambientTemperatureCelsius"].ToString().Replace("\"", string.Empty));
                    }
                }
                if (deviceData["traits"]["sdm.devices.traits.Humidity"] != null)
                {
                    if (onHumidity != null)
                    {
                        onHumidity(Convert.ToUInt16(deviceData["traits"]["sdm.devices.traits.Humidity"]["ambientHumidityPercent"].ToString().Replace("\"", string.Empty)));
                    }
                }
                if (deviceData["traits"]["sdm.devices.traits.Fan"] != null)
                {
                    if (onFanState != null)
                    {
                        if (deviceData["traits"]["sdm.devices.traits.Fan"]["timerMode"].ToString().Replace("\"", string.Empty) == "ON")
                        {
                            onFanState(1);
                        }
                        else
                        {
                            onFanState(0);
                        }
                    }
                }
                if (deviceData["traits"]["sdm.devices.traits.ThermostatMode"] != null)
                {
                    if (onCurrentMode != null)
                    {
                        onCurrentMode(deviceData["traits"]["sdm.devices.traits.ThermostatMode"]["mode"].ToString().Replace("\"", string.Empty));
                    }
                }
                if (deviceData["traits"]["sdm.devices.traits.ThermostatEco"] != null)
                {
                    if (onEcoModeState != null)
                    {
                        onEcoModeState(deviceData["traits"]["sdm.devices.traits.ThermostatEco"]["mode"].ToString().Replace("\"", string.Empty));
                    }
                    if (onEcoHeatSetPoint != null)
                    {
                        double temp = Math.Round(Convert.ToDouble(deviceData["traits"]["sdm.devices.traits.ThermostatEco"]["heatCelsius"].ToString().Replace("\"", string.Empty)), 1);

                        onEcoHeatSetPoint((ushort)(temp * 10));
                    }
                    if (onEcoCoolSetPoint != null)
                    {
                        double temp = Math.Round(Convert.ToDouble(deviceData["traits"]["sdm.devices.traits.ThermostatEco"]["coolCelsius"].ToString().Replace("\"", string.Empty)), 1);

                        onEcoCoolSetPoint((ushort)(temp * 10));
                    }
                }
                if (deviceData["traits"]["sdm.devices.traits.Settings"] != null)
                {
                    if (onTemperatureMode != null)
                    {
                        onTemperatureMode(deviceData["traits"]["sdm.devices.traits.Settings"]["temperatureScale"].ToString().Replace("\"", string.Empty));
                    }
                }
                if (deviceData["traits"]["sdm.devices.traits.ThermostatTemperatureSetpoint"] != null)
                {
                    if (onCurrentSetPoint != null)
                    {
                        //add farheniheit
                        if (deviceData["traits"]["sdm.devices.traits.ThermostatTemperatureSetpoint"]["coolCelsius"] != null)
                        {
                            double temp = Math.Round(Convert.ToDouble(deviceData["traits"]["sdm.devices.traits.ThermostatTemperatureSetpoint"]["coolCelsius"].ToString().Replace("\"", string.Empty)), 1);

                            onCurrentSetPoint((ushort)(temp * 10));
                        }
                        else if (deviceData["traits"]["sdm.devices.traits.ThermostatTemperatureSetpoint"]["heatCelsius"] != null)
                        {
                            double temp = Math.Round(Convert.ToDouble(deviceData["traits"]["sdm.devices.traits.ThermostatTemperatureSetpoint"]["heatCelsius"].ToString().Replace("\"", string.Empty)), 1);

                            onCurrentSetPoint((ushort)(temp * 10));
                        }
                    }
                }
            }
        }
    }
}
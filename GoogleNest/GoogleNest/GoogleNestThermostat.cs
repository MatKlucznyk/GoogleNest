using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Https;
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
        public delegate void CurrentHeatSetPoint(ushort value);
        public delegate void CurrentCoolSetPoint(ushort value);
        public delegate void CurrentTemperature(ushort value, SimplSharpString sValue);
        public delegate void CurrentHvac(SimplSharpString hvac);
        public Online onOnline { get; set; }
        public FanState onFanState { get; set; }
        public EcoModeState onEcoModeState { get; set; }
        public TemperatureMode onTemperatureMode { get; set; }
        public Humidity onHumidity { get; set; }
        public CurrentMode onCurrentMode { get; set; }
        public EcoHeatSetPoint onEcoHeatSetPoint { get; set; }
        public EcoCoolSetPoint onEcoCoolSetPoint { get; set; }
        public CurrentHeatSetPoint onCurrentHeatSetPoint { get; set; }
        public CurrentCoolSetPoint onCurrentCoolSetPoint { get; set; }
        public CurrentTemperature onCurrentTemperature { get; set; }
        public CurrentHvac onCurrentHvac { get; set; }

        private CTimer fanTimer;
        private bool isFahrenheit;

        public GoogleNestThermostat()
        {
            fanTimer = new CTimer(FanTimerCompleted, 1000);
            fanTimer.Stop();
        }

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

                        if (isFahrenheit)
                        {
                            temp = CelsiusToFahrenHeit(temp);
                        }

                        onCurrentTemperature((ushort)(temp * 10), temp.ToString());
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
                            fanTimer.Stop();
                            onFanState(0);
                        }
                        if (deviceData["traits"]["sdm.devices.traits.Fan"]["timerTimeout"] != null)
                        {
                            var time = deviceData["traits"]["sdm.devices.traits.Fan"]["timerTimeout"].ToString().Replace("\"", string.Empty);

                            var timeout = DateTime.Parse(time);
                            var timerSetting = timeout - DateTime.Now;

                            fanTimer.Reset(timerSetting.Milliseconds);
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

                        if (isFahrenheit)
                        {
                            temp = CelsiusToFahrenHeit(temp);
                        }

                        onEcoHeatSetPoint((ushort)(temp * 10));
                    }
                    if (onEcoCoolSetPoint != null)
                    {
                        double temp = Math.Round(Convert.ToDouble(deviceData["traits"]["sdm.devices.traits.ThermostatEco"]["coolCelsius"].ToString().Replace("\"", string.Empty)), 1);

                        if (isFahrenheit)
                        {
                            temp = CelsiusToFahrenHeit(temp);
                        }

                        onEcoCoolSetPoint((ushort)(temp * 10));
                    }
                }
                if (deviceData["traits"]["sdm.devices.traits.Settings"] != null)
                {
                    if (onTemperatureMode != null)
                    {
                        if (deviceData["traits"]["sdm.devices.traits.Settings"]["temperatureScale"].ToString().Replace("\"", string.Empty) == "FAHRENHEIT")
                        {
                            isFahrenheit = true;
                        }
                        else
                        {
                            isFahrenheit = false;
                        }

                        onTemperatureMode(deviceData["traits"]["sdm.devices.traits.Settings"]["temperatureScale"].ToString().Replace("\"", string.Empty));
                    }
                }
                if (deviceData["traits"]["sdm.devices.traits.ThermostatTemperatureSetpoint"] != null)
                {
                    //add farheniheit
                    if (deviceData["traits"]["sdm.devices.traits.ThermostatTemperatureSetpoint"]["coolCelsius"] != null)
                    {
                        double temp = Math.Round(Convert.ToDouble(deviceData["traits"]["sdm.devices.traits.ThermostatTemperatureSetpoint"]["coolCelsius"].ToString().Replace("\"", string.Empty)), 1);

                        if (isFahrenheit)
                        {
                            temp = CelsiusToFahrenHeit(temp);
                        }

                        if (onCurrentCoolSetPoint != null)
                            onCurrentCoolSetPoint((ushort)(temp * 10));
                    }
                    if (deviceData["traits"]["sdm.devices.traits.ThermostatTemperatureSetpoint"]["heatCelsius"] != null)
                    {
                        double temp = Math.Round(Convert.ToDouble(deviceData["traits"]["sdm.devices.traits.ThermostatTemperatureSetpoint"]["heatCelsius"].ToString().Replace("\"", string.Empty)), 1);

                        if (isFahrenheit)
                        {
                            temp = CelsiusToFahrenHeit(temp);
                        }

                        if (onCurrentHeatSetPoint != null)
                            onCurrentHeatSetPoint((ushort)(temp * 10));
                    }
                }
                if (deviceData["traits"]["sdm.devices.traits.ThermostatHvac"] != null)
                {
                    if (onCurrentHvac != null)
                    {
                        onCurrentHvac(deviceData["traits"]["sdm.devices.traits.ThermostatHvac"]["status"].ToString().Replace("\"", string.Empty));
                    }
                }
            }
        }

        public void SetCool(ushort setPoint)
        {
            try
            {
                double sPoint = sPoint = Math.Round((setPoint / 10.0), 1);

                if (isFahrenheit)
                {
                    sPoint = FahrenheitToCelsius(sPoint);
                }

                if (sPoint >= 90 && sPoint <= 320)
                {
                    var response = PostCommand("{\"command\":\"sdm.devices.commands.ThermostatTemperatureSetpoint.SetCool\",\"params\":{\"coolCelsius\":" + sPoint + "}}");

                    if (response != null)
                    {
                        if (response.Length > 0)
                        {
                            if (response == "{}\n")
                            {
                                if (onCurrentCoolSetPoint != null)
                                    onCurrentCoolSetPoint(setPoint);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
            }
        }

        public void SetHeat(ushort setPoint)
        {
            try
            {
                double sPoint = Math.Round((Convert.ToDouble(setPoint) / 10.0), 1);

                if (isFahrenheit)
                {
                    sPoint = FahrenheitToCelsius(sPoint);
                }

                if (sPoint >= 90 && sPoint <= 320)
                {
                    var response = PostCommand("{\"command\":\"sdm.devices.commands.ThermostatTemperatureSetpoint.SetHeat\",\"params\":{\"heatCelsius\":" + sPoint + "}}");

                    if (response != null)
                    {
                        if (response.Length > 0)
                        {
                            if (response == "{}\n")
                            {
                                if (onCurrentHeatSetPoint != null)
                                    onCurrentHeatSetPoint(setPoint);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
            }
        }

        public void SetRange(ushort heat, ushort cool)
        {
            try
            {
                var HsPoint = Math.Round((Convert.ToDouble(heat) / 10.0), 1);
                var CsPoint = Math.Round((Convert.ToDouble(cool) / 10.0), 1);

                if (isFahrenheit)
                {
                    HsPoint = FahrenheitToCelsius(HsPoint);
                    CsPoint = FahrenheitToCelsius(CsPoint);
                }

                if (HsPoint >= 90 && HsPoint <= 320 && CsPoint >= 90 && CsPoint <= 320)
                {
                    var response = PostCommand("{\"command\":\"sdm.devices.commands.ThermostatTemperatureSetpoint.SetRange\",\"params\":{\"heatCelsius\":" + HsPoint + ",\"coolCelsius\":" + CsPoint + "}}");

                    if (response != null)
                    {
                        if (response.Length > 0)
                        {
                            if (response == "{}\n")
                            {
                                if (onCurrentHeatSetPoint != null)
                                    onCurrentHeatSetPoint(heat);

                                if (onCurrentCoolSetPoint != null)
                                    onCurrentCoolSetPoint(cool);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
            }
        }

        public void RunFan(ushort timeInSeconds)
        {
            try
            {
                if (timeInSeconds > 0)
                {
                    var response = PostCommand("{\"command\":\"sdm.devices.commands.Fan.SetTimer\",\"params\":{\"timerMode\":\"ON\",\"duration\":\"" + timeInSeconds + "s\"}}");

                    if (response != null)
                    {
                        if (response.Length > 0)
                        {
                            if (response == "{}\n")
                            {
                                fanTimer.Reset(timeInSeconds * 1000);

                                if (onFanState != null)
                                    onFanState(1);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
            }
        }

        public void SetHvacMode(string mode)
        {
            try
            {
                var response = PostCommand("{\"command\":\"sdm.devices.commands.ThermostatMode.SetMode\",\"params\":{\"mode\":\"" + mode + "\"}}");

                    if (response != null)
                    {
                        if (response.Length > 0)
                        {
                            if (response == "{}\n")
                            {
                                GetDevice();
                            }
                        }
                    }
                }
            catch (Exception e)
            {
            }
        }

        public void SetEcoMode(string mode)
        {
            try
            {
                var response = PostCommand("{\"command\":\"sdm.devices.commands.ThermostatEco.SetMode\",\"params\":{\"mode\":\"" + mode + "\"}}");

                if (response != null)
                {
                    if (response.Length > 0)
                    {
                        if (response == "{}\n")
                        {
                            GetDevice();
                        }
                    }
                }
            }
            catch (Exception e)
            {
            }
        }

        private void FanTimerCompleted(object o)
        {
            if (onFanState != null)
                onFanState(0);
        }

        private double CelsiusToFahrenHeit(double temp)
        {
            return (temp * (9 / 5)) + 32;
        }

        private double FahrenheitToCelsius(double temp)
        {
            return (temp - 32) * (5 / 9);
        }
    }
}
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
        public delegate void EcoModeState(SimplSharpString mode);
        
        public delegate void CurrentMode(SimplSharpString mode);
        public delegate void EcoHeatSetPoint(ushort value);
        public delegate void EcoCoolSetPoint(ushort value);
        public delegate void CurrentHeatSetPoint(ushort value);
        public delegate void CurrentCoolSetPoint(ushort value);
        public delegate void CurrentHvac(SimplSharpString hvac);
        public EcoModeState onEcoModeState { get; set; }
        public CurrentMode onCurrentMode { get; set; }
        public EcoHeatSetPoint onEcoHeatSetPoint { get; set; }
        public EcoCoolSetPoint onEcoCoolSetPoint { get; set; }
        public CurrentHeatSetPoint onCurrentHeatSetPoint { get; set; }
        public CurrentCoolSetPoint onCurrentCoolSetPoint { get; set; }
        public CurrentHvac onCurrentHvac { get; set; }

        //Parse data related to thermostats
        internal override void ParseData(JToken deviceData)
        {
            //calls base class global device trait parsing
            base.ParseData(deviceData);

            //check if connectivty trait exists to determine if we need to really parse data or not
            if (isOnline)
            {
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
                        decimal temp = Math.Round(Convert.ToDecimal(deviceData["traits"]["sdm.devices.traits.ThermostatEco"]["heatCelsius"].ToString().Replace("\"", string.Empty)), 1);

                        if (isFahrenheit)
                        {
                            temp = CelsiusToFahrenHeit(temp);
                        }

                        onEcoHeatSetPoint((ushort)(temp * 10));
                    }
                    if (onEcoCoolSetPoint != null)
                    {
                        decimal temp = Math.Round(Convert.ToDecimal(deviceData["traits"]["sdm.devices.traits.ThermostatEco"]["coolCelsius"].ToString().Replace("\"", string.Empty)), 1);

                        if (isFahrenheit)
                        {
                            temp = CelsiusToFahrenHeit(temp);
                        }

                        onEcoCoolSetPoint((ushort)(temp * 10));
                    }
                }
                if (deviceData["traits"]["sdm.devices.traits.ThermostatTemperatureSetpoint"] != null)
                {
                    //add farheniheit
                    if (deviceData["traits"]["sdm.devices.traits.ThermostatTemperatureSetpoint"]["coolCelsius"] != null)
                    {
                        decimal temp = Math.Round(Convert.ToDecimal(deviceData["traits"]["sdm.devices.traits.ThermostatTemperatureSetpoint"]["coolCelsius"].ToString().Replace("\"", string.Empty)), 1);

                        if (isFahrenheit)
                        {
                            temp = CelsiusToFahrenHeit(temp);
                        }

                        if (onCurrentCoolSetPoint != null)
                            onCurrentCoolSetPoint((ushort)(temp * 10));
                    }
                    if (deviceData["traits"]["sdm.devices.traits.ThermostatTemperatureSetpoint"]["heatCelsius"] != null)
                    {
                        decimal temp = Math.Round(Convert.ToDecimal(deviceData["traits"]["sdm.devices.traits.ThermostatTemperatureSetpoint"]["heatCelsius"].ToString().Replace("\"", string.Empty)), 1);

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

        //Set cool setopoint
        public void SetCool(ushort setPoint)
        {
            try
            {
                //convert s+ ushort to a double moving the percision byb 10
                decimal sPoint = sPoint = Math.Round(((decimal)setPoint / (decimal)10.0), 1);

                if (isFahrenheit)
                {
                    sPoint = FahrenheitToCelsius(sPoint);
                }

                //checks if set point is an allowable range
                if (sPoint >= 9 && sPoint <= 32)
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

        //Set heat setpoint
        public void SetHeat(ushort setPoint)
        {
            try
            {
                //convert s+ ushort to a double moving the percision by 10
                decimal sPoint = Math.Round((decimal)setPoint / (decimal)10.0, 1);

                if (isFahrenheit)
                {
                    sPoint = FahrenheitToCelsius(sPoint);
                }

                //checks if set point is an allowable range
                if (sPoint >= 9 && sPoint <= 32)
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

        //Set heat/cool range setpoints
        public void SetRange(ushort heat, ushort cool)
        {
            try
            {
                //convert s+ ushort to a double moving the percision byb 10
                var HsPoint = Math.Round((decimal)(heat / (decimal)10.0), 1);
                var CsPoint = Math.Round(((decimal)cool / (decimal)10.0), 1);

                if (isFahrenheit)
                {
                    HsPoint = FahrenheitToCelsius(HsPoint);
                    CsPoint = FahrenheitToCelsius(CsPoint);
                }

                //checks if set point is an allowable range
                if (HsPoint >= 9 && HsPoint <= 32 && CsPoint >= 9 && CsPoint <= 32)
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

        //Set HVAC mode to heat, cool or heat/cool
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

        //Turn eco mode on or off
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
    }
}
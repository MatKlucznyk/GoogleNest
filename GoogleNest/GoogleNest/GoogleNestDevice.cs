﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Https;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GoogleNest
{
    public class GoogleNestDevice : IDisposable
    {
        public delegate void Online(ushort state);
        public delegate void RoomName(SimplSharpString name);
        public delegate void ErrorMsg(SimplSharpString msg);
        public delegate void FanState(ushort state);
        public delegate void TemperatureMode(SimplSharpString mode);
        public delegate void Humidity(ushort value);
        public delegate void CurrentTemperature(ushort value, SimplSharpString sValue);
        public Online onOnline { get; set; }
        public RoomName onRoomName { get; set; }
        public ErrorMsg onErrorMsg { get; set; }
        public FanState onFanState { get; set; }
        public TemperatureMode onTemperatureMode { get; set; }
        public Humidity onHumidity { get; set; }
        public CurrentTemperature onCurrentTemperature { get; set; }

        public string DeviceName { get { return deviceName; } }
        public ushort Debug 
        { 
            get { return Convert.ToUInt16(_debug); }
            set { _debug = Convert.ToBoolean(value); }
        }

        private string deviceName;
        private CTimer fanTimer;
        //private HttpsClient client = new HttpsClient() { TimeoutEnabled = true, Timeout = 5, HostVerification = false, PeerVerification = false, AllowAutoRedirect = false, IncludeHeaders = false };

        internal string DeviceID;
        internal bool isOnline;
        internal bool isFahrenheit;
        internal bool _debug;

        public GoogleNestDevice()
        {
            //Setup fan timer
            fanTimer = new CTimer(FanTimerCompleted, Timeout.Infinite);
        }

        //Add this device to list in GoogleNestCloud
        public void Initialize(string deviceName)
        {
            this.deviceName = deviceName;
            
            lock (GoogleNestCloud.devices)
            {
                GoogleNestCloud.devices.Add(deviceName, this);
            }

            if (GoogleNestCloud.Initialized)
            {
                GetDevice();
            }
        }
        
    
        //Parse device data
        internal virtual void ParseData(JToken deviceData)
        {
            if (deviceData["traits"]["sdm.devices.traits.Connectivity"] != null)
            {
                if (deviceData["traits"]["sdm.devices.traits.Connectivity"].ToString().Contains("ONLINE"))
                {
                    if (onOnline != null)
                    {
                        isOnline = true;
                        onOnline(1);
                    }
                }
                else
                {
                    if (onOnline != null)
                    {
                        isOnline = false;
                        onOnline(0);
                    }
                }
            }
            if (deviceData["name"] != null)
            {
                DeviceID = deviceData["name"].ToString().Replace("\"", string.Empty);
            }
            if (deviceData["parentRelations"] != null)
            {
                if (onRoomName != null)
                {
                    onRoomName(deviceData["parentRelations"][0]["displayName"].ToString().Replace("\"", string.Empty));
                }
            }
            if (deviceData["error"] != null)
            {
                onErrorMsg(deviceData["error"]["message"].ToString().Replace("\"", string.Empty));
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
            if (deviceData["traits"]["sdm.devices.traits.Temperature"] != null)
            {
                if (onCurrentTemperature != null)
                {
                    decimal temp = Math.Round(Convert.ToDecimal(deviceData["traits"]["sdm.devices.traits.Temperature"]["ambientTemperatureCelsius"].ToString().Replace("\"", string.Empty)), 1);

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
        }

        internal void PrintDebug(string msg)
        {
            if (_debug)
            {
                CrestronConsole.PrintLine("GoogleNestDevice --DEBUG: {0}", msg);
            }
        }

        //Post HTTPS command
        internal string PostCommand(string body)
        {
            try
            {
                if (DeviceID.Length > 0)
                {
                    HttpsHeaders headers = new HttpsHeaders();
                    headers.ContentType = "application/json";
                    headers.AddHeader(new HttpsHeader("Authorization", string.Format("{0} {1}", GoogleNestCloud.TokenType, GoogleNestCloud.Token)));

                    var response = HttpsConnection.ClientPool.SendRequest("https://smartdevicemanagement.googleapis.com/v1/" + DeviceID + ":executeCommand", RequestType.Post, headers, body);

                    if(response != null)
                    {
                        PrintDebug(string.Format("Response Found -> {0}", response.Content));
                        return response.Content;
                    }
                }

                return string.Empty;
            }
            catch (Exception e)
            {
                ErrorLog.Exception("Exception ocurred in PostCommand", e);
                return string.Empty;
            }
        }

        //Get device info
        public void GetDevice()
        {
            try
            {
                if (DeviceID.Length > 0)
                {
                    HttpsHeaders headers = new HttpsHeaders();
                    headers.ContentType = "application/json";
                    headers.AddHeader(new HttpsHeader("Authorization", string.Format("{0} {1}", GoogleNestCloud.TokenType, GoogleNestCloud.Token)));

                    var response = HttpsConnection.ClientPool.SendRequest("https://smartdevicemanagement.googleapis.com/v1/" + DeviceID, RequestType.Get, headers, string.Empty);

                    if (response.Content != null)
                    {
                        if (response.Content.Length > 0)
                        {
                            PrintDebug(string.Format("Response Found -> {0}", response.Content));
                            JToken body = JToken.Parse(response.Content);
                            ParseData(body);
                        }
                    }
                }
                else
                {
                    if (onErrorMsg != null)
                    {
                        onErrorMsg("Device not found, ensure the Label field is set in the app");
                    }
                }
            }
            catch (Exception e)
            {
                ErrorLog.Exception("Exception ocurred in GetDevice", e);
            }
        }

        //Run fan for specified time
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

        //turn the fan off
        public void FanOff()
        {
            try
            {
                var response = PostCommand("{\"command\":\"sdm.devices.commands.Fan.SetTimer\",\"params\":{\"timerMode\":\"OFF\"}}");

                if (response != null)
                {
                    if (response.Length > 0)
                    {
                        if (response == "{}\n")
                        {
                            if (onFanState != null)
                                onFanState(0);
                        }
                    }
                }
            }
            catch (Exception e)
            {
            }
        }

        //Fan timer has completed fan is now off
        private void FanTimerCompleted(object o)
        {
            if (onFanState != null)
                onFanState(0);
        }

        //Convert celsius to fahrenheit
        internal decimal CelsiusToFahrenHeit(decimal temp)
        {
            return (temp * ((decimal)9.0 / (decimal)5.0)) + (decimal)32.0;
        }

        //Convert fahrenheit to celcius
        internal decimal FahrenheitToCelsius(decimal temp)
        {
            return (temp - (decimal)32.0) * ((decimal)5.0 / (decimal)9.0);
        }

        public void Dispose()
        {
            if (fanTimer != null)
            {
                fanTimer.Stop();
                fanTimer.Dispose();
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GoogleNest
{
    public class GoogleNestDevice
    {
        public string DeviceName { get { return DeviceName; } }

        private string deviceName;

        internal string DeviceID;

        public void Initialize(string deviceName)
        {
            this.deviceName = deviceName;
            
            lock (GoogleNestCloud.devices)
            {
                GoogleNestCloud.devices.Add(deviceName, this);
            }
        }
            
        internal virtual void ParseData(JToken deviceData)
        {
            DeviceID = deviceData["name"].ToString().Replace("\"", string.Empty);
        }
    }
}
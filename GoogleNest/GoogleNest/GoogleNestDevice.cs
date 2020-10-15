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
    public class GoogleNestDevice
    {
        public delegate void RoomName(SimplSharpString name);
        public RoomName onRoomName { get; set; }

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

            if (GoogleNestCloud.Initialized)
            {
                GetDevice();
            }
        }
            
        internal virtual void ParseData(JToken deviceData)
        {
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
        }

        public void GetDevice()
        {
            try
            {
                using (HttpsClient client = new HttpsClient())
                {
                    client.TimeoutEnabled = true;
                    client.Timeout = 10;
                    client.HostVerification = false;
                    client.PeerVerification = false;
                    client.AllowAutoRedirect = false;

                    HttpsClientRequest request = new HttpsClientRequest();

                    request.Url.Parse("https://smartdevicemanagement.googleapis.com/v1/" + DeviceID);
                    request.RequestType = RequestType.Get;
                    request.Header.ContentType = "application/json";
                    request.Header.AddHeader(new HttpsHeader("Authorization", string.Format("{0} {1}", GoogleNestCloud.TokenType, GoogleNestCloud.Token)));

                    HttpsClientResponse response = client.Dispatch(request);

                    if (response.ContentString != null)
                    {
                        if (response.ContentString.Length > 0)
                        {
                            JToken body = JToken.Parse(response.ContentString);
                            ParseData(body);
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
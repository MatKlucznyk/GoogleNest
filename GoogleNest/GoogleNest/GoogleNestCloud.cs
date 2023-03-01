using System;
using System.Text;
using System.Collections.Generic;
using Crestron.SimplSharp;                          				// For Basic SIMPL# Classes
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.Net.Https;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GoogleNest
{
    public class GoogleNestCloud : IDisposable
    {
        public static string AuthCode { get; set; }
        public static string ClientID { get; set; }
        public static string ClientSecret { get; set; }
        public static string ProjectID { get; set; }
        public ushort Debug
        {
            get { return Convert.ToUInt16(_debug); }
            set { _debug = Convert.ToBoolean(value); }
        }

        public delegate void ErrorMsg(SimplSharpString errorMessage);
        public delegate void IsInitialized(ushort state);
        public ErrorMsg onErrorMessage { get; set; }
        public IsInitialized onIsInitialized { get; set; }

        private string refreshTokenFilePath;
        private string refreshTokenFileName = "google_nest_config";
        private string refreshToken;

        private readonly CCriticalSection _disposeLock = new CCriticalSection();
        private CTimer refreshTimer;
        //private readonly HttpsClient client = new HttpsClient() { TimeoutEnabled = true, Timeout = 5, HostVerification = false, PeerVerification = false, AllowAutoRedirect = false, IncludeHeaders = false };
        private bool _disposed;
        private int _refreshTries;
        private bool _debug;

        internal static bool Initialized;
        internal static string Token;
        internal static string TokenType;
        private readonly static Dictionary<string, GoogleNestDevice> devices = new Dictionary<string, GoogleNestDevice>();
        private static readonly object _devicesLock = new object();

        public GoogleNestCloud()
        {
            refreshTimer = new CTimer(UseRefreshToken, Timeout.Infinite);
        }

        internal void PrintDebug(string msg)
        {
            if (_debug)
            {
                CrestronConsole.PrintLine("GoogleNestCloud --DEBUG: {0}", msg);
            }
        }

        internal static bool AddDevice(string name, GoogleNestDevice device)
        {
            lock (_devicesLock)
            {
                if (devices.ContainsKey(name))
                    return false;

                devices.Add(name, device);

                return true;
            }
        }

        //Check if refresh token file exists and consume if it does
        public void Initialize()
        {
            try
            {
                refreshTokenFilePath = string.Format(@"\user\{0}\", Directory.GetApplicationDirectory().Replace("/", "\\").Split('\\')[2]);

                if (File.Exists(refreshTokenFilePath + refreshTokenFileName))
                {
                    using (StreamReader reader = new StreamReader(File.OpenRead(refreshTokenFilePath + refreshTokenFileName)))
                    {
                        refreshToken = reader.ReadToEnd().Replace("\r\n", string.Empty);
                    }
                    if (refreshToken.Length > 0)
                    {
                        UseRefreshToken(null);
                    }
                    else
                    {
                        if (onErrorMessage != null)
                        {
                            
                            onErrorMessage("Stored refresh token file is empty, please delete file and use authorization code");
                        }
                    }

                }
                else
                {
                    if (AuthCode.Length > 0)
                    {
                        GetTokenAndRefreshToken();
                    }
                    else
                    {
                        if (onErrorMessage != null)
                        {
                            onErrorMessage("Authorization code cannot be empty on the first query to the Google API service, please set authorization code");
                        }
                    }
                }

                if (Token.Length > 0)
                {
                    Initialized = true;

                    if (onIsInitialized != null)
                    {
                        onIsInitialized(1);
                    }

                    GetDevices();
                }
                else
                {
                    Initialized = false;

                    if (onIsInitialized != null)
                    {
                        onIsInitialized(0);
                    }
                }
            }
            catch (Exception e)
            {
                Initialized = false;

                if (onIsInitialized != null)
                {
                    onIsInitialized(0);
                }

                ErrorLog.Exception("Exception ocurred in Initialize", e);
            }
        }

        //Use refresh token to request a session token
        private void UseRefreshToken(object o)
        {
            try
            {
                PrintDebug("Using refresh token...");
                var response = HttpsConnection.ClientPool.SendRequest("https://www.googleapis.com/oauth2/v4/token?client_id=" + ClientID + "&refresh_token=" + refreshToken + "&grant_type=refresh_token&redirect_uri=https://www.google.com&client_secret=" + ClientSecret, RequestType.Post, null, string.Empty);
                var found = false;

                if (response.Content != null)
                {
                    if (response.Content.Length > 0)
                    {
                        PrintDebug(string.Format("Response Found -> {0}", response.Content));
                        var body = JObject.Parse(response.Content);

                        if (body["expires_in"] != null)
                        {
                            var seconds = 1000;
                            var milliseconds = seconds * 1000;

                            found = true;
                            _refreshTries = 0;
                            refreshTimer.Reset(milliseconds);

                        }
                        if (body["access_token"] != null)
                        {
                            Token = body["access_token"].ToString().Replace("\"", string.Empty);
                        }
                        if (body["token_type"] != null)
                        {
                            TokenType = body["token_type"].ToString().Replace("\"", string.Empty);
                        }
                    }
                }
                if(found == false && _refreshTries < 120 && refreshToken.Length > 0)
                {
                    _refreshTries++;
                    refreshTimer.Reset(1800000);
                }
                else if(found == false && _refreshTries >= 120)
                {
                    if (onErrorMessage != null)
                    {
                        onErrorMessage("Refresh token failed to refresh 120 times (every 30 minutes for the past 2.5 days). Please gerenate a new authorization code.");
                    }
                }
            }
            catch (Exception e)
            {
                ErrorLog.Exception("Exception ocurred in UseRefreshToken", e);
            }
        }

        //Use authorization code to retrieve a refresh token and a session token
        private void GetTokenAndRefreshToken()
        {
            try
            {
                PrintDebug("Getting token and refresh token...");
                var response = HttpsConnection.ClientPool.SendRequest("https://www.googleapis.com/oauth2/v4/token?client_id=" + ClientID + "&code=" + AuthCode + "&grant_type=authorization_code&redirect_uri=https://www.google.com&client_secret=" + ClientSecret, RequestType.Post, null, string.Empty);

                if (response.Content != null)
                {
                    if (response.Content.Length > 0)
                    {
                        PrintDebug(string.Format("Response Found -> {0}", response.Content));
                        JObject body = JObject.Parse(response.Content);

                        if (body["expires_in"] != null)
                        {
                            var seconds = 1000;
                            var milliseconds = seconds * 1000;

                            refreshTimer.Reset(milliseconds);
                        }
                        if (body["access_token"] != null)
                        {
                            Token = body["access_token"].ToString().Replace("\"", string.Empty);
                        }
                        if (body["token_type"] != null)
                        {
                            TokenType = body["token_type"].ToString().Replace("\"", string.Empty);
                        }
                        if (body["refresh_token"] != null)
                        {
                            refreshToken = body["refresh_token"].ToString().Replace("\"", string.Empty);

                            using (StreamWriter writer = new StreamWriter(File.Create(refreshTokenFilePath + refreshTokenFileName)))
                            {
                                writer.WriteLine(refreshToken);
                            }
                        }
                    }
                }

            }
            catch (Exception e)
            {
                ErrorLog.Exception("Exception ocurred in GetTokenAndRefreshToken", e);
            }
        }

        //Get all devices
        public void GetDevices()
        {
            try
            {
                var headers = new HttpsHeaders();
                headers.ContentType = "application/json";
                headers.AddHeader(new HttpsHeader("Authorization", string.Format("{0} {1}", TokenType, Token)));


                var response = HttpsConnection.ClientPool.SendRequest("https://smartdevicemanagement.googleapis.com/v1/enterprises/" + ProjectID + "/devices", RequestType.Get, headers, string.Empty);

                if (response.Content != null)
                {
                    if (response.Content.Length > 0)
                    {
                        PrintDebug(string.Format("Response Found -> {0}", response.Content));
                        var body = JObject.Parse(response.Content);

                        if (body["error"] != null && onErrorMessage != null)
                        {
                            onErrorMessage(body["error"]["message"].ToString().Replace("\"", string.Empty));
                        }

                        foreach (var dev in body["devices"])
                        {
                            PrintDebug(string.Format("Device found {0}:\n{1}", dev["traits"]["sdm.devices.traits.Info"]["customName"].ToObject<string>(), dev));
                            if (devices.ContainsKey(dev["traits"]["sdm.devices.traits.Info"]["customName"].ToString().Replace("\"", string.Empty)))
                            {
                                devices[dev["traits"]["sdm.devices.traits.Info"]["customName"].ToString().Replace("\"", string.Empty)].ParseData(dev);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ErrorLog.Exception("Exception ocurred in GetDevices", e);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            _disposed = true;
            if (disposing)
            {
                if (refreshToken != null)
                {
                    refreshTimer.Stop();
                    refreshTimer.Dispose();
                }

                foreach (var device in devices)
                {
                    device.Value.Dispose();
                }
            }
        }
    }
}

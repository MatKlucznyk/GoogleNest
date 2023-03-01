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
    public class GoogleNestDoorbell : GoogleNestDevice
    {
        public delegate void LiveStreamMaxVideoResolution(ushort width, ushort height);
        public delegate void LiveStreamVideoCodecs(SimplSharpString listOfCodecs);
        public delegate void LiveStreamAudioCodecs(SimplSharpString listOfAudioCodecs);
        public delegate void LiveStreamSupportedProtocols(SimplSharpString listOfProtocols);
        public delegate void MaxImageResolution(ushort width, ushort height);
        public LiveStreamMaxVideoResolution onLiveStreamMaxVideoResolution { get; set; }
        public LiveStreamVideoCodecs onLiveStreamVideoCodecs { get; set; }
        public LiveStreamAudioCodecs onLiveStreamAudioCodecs { get; set; }
        public LiveStreamSupportedProtocols onLiveStreamSupportedProtocols { get; set; }
        public MaxImageResolution onMaxImageResolution { get; set; }

        internal override void ParseData(JToken deviceData)
        {
            base.ParseData(deviceData);

            if (deviceData["traits"]["sdm.devices.traits.CameraLiveStream"] != null)
            {
                if (onLiveStreamMaxVideoResolution != null)
                {
                    onLiveStreamMaxVideoResolution(
                        deviceData["traits"]["sdm.devices.traits.CameraLiveStream"]["maxVideoResolution"]["width"].ToObject<ushort>(),
                        deviceData["traits"]["sdm.devices.traits.CameraLiveStream"]["maxVideoResolution"]["height"].ToObject<ushort>()
                        );
                }
            }
        }
    }
}
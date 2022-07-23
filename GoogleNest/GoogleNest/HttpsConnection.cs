using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using HttpsUtility.Https;

namespace GoogleNest
{
    internal class HttpsConnection
    {
        internal static HttpsClientPool ClientPool = new HttpsClientPool();

    }
}
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace PaheScrapper.Helpers
{
    public static class NetworkHelper
    {
        public static bool IsNetworkStable()
        {
            string[] targets = {"google.com", "yahoo.com", "facebook.com", "linkedin.com", "twitter.com"};
            List<PingReply> pingReplies = new List<PingReply>();
            Ping ping = new Ping();

            for (int i = 0; i < 5; i++)
            {
                try
                {
                    var response = ping.Send(targets[i]);

                    if (response == null)
                        continue;

                    pingReplies.Add(response);
                }
                catch (Exception e)
                {
                    return false;
                }
            }

            var result = pingReplies
                .Select(l => l.Status == IPStatus.Success &&
                                    l.RoundtripTime < 700)
                .Aggregate((a, b) => a & b);

            return result;
        }

        public static void WaitStableNetwork()
        {
            bool isStable = false;

            do
            {
                isStable = IsNetworkStable();

                if (!isStable)
                    Thread.Sleep(1000);

            } while (!isStable);
        }
    }
}
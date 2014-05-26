using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using HtmlAgilityPack;
using Log = ScheduledTaskAgent1.Logger;


namespace ScheduledTaskAgent1
{
    public class BusTicker
    {
        const string route_url = @"http://pda.5284.com.tw/MQS/businfo2.jsp?routename=";

        public static Task<string> GetBusDueTime(BusStatDir bsd)
        {
            return GetBusDueTime(bsd.bus, bsd.station, bsd.dir);
        }

        public static async Task<string> GetBusDueTime(string busName, string stationName, BusDir busDir = BusDir.go)
        {
            string url = String.Format(@"http://pda.5284.com.tw/MQS/businfo3.jsp?Mode=1&Dir={1}&Route={0}&Stop={2}", Uri.EscapeUriString(busName), 
                busDir==BusDir.go?1:0, Uri.EscapeUriString(stationName));

            var client = new HttpClient();
            Log.Debug(String.Format("client.GetStringAsync({0}, {1}) begin", busName, stationName));
            string strResult = await client.GetStringAsync(new Uri(url));
            Log.Debug(String.Format("client.GetStringAsync({0}, {1}) end", busName, stationName));
            var doc = new HtmlDocument();
            doc.LoadHtml(strResult);

            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes(
                "/html/body/center/table/tr[6]/td");
            if (nodes.Count == 0)
                return "";
            return nodes[0].InnerText;
        }
    }
    public enum BusDir
    {
        go, back,
    };
    public class BusStatDir
    {
        public string bus;
        public string station;
        public BusDir dir;
    }



}

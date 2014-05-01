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


namespace ScheduledTaskAgent1
{
    public class BusTicker
    {
        const string route_url = @"http://pda.5284.com.tw/MQS/businfo2.jsp?routename=";

        public static Task<string> GetBusDueTime(BusStatDir bsd)
        {
            return GetBusDueTime(bsd.bus, bsd.station, bsd.dir, 0);
        }

        public static async Task<string> GetBusDueTime(string busName, string stationName, BusDir busDir = BusDir.go, int timeOut = 0)
        {
            string url = route_url + Uri.EscapeUriString(busName);
            var client = new HttpClient();
            string strResult = await client.GetStringAsync(url, timeOut);
            var doc = new HtmlDocument();
            doc.LoadHtml(strResult);

            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes(
                "/html/body/center/table/tr[5]/td/table/tr[2]/td["+  
                (busDir==BusDir.go?"1":"2") + "]/table/tr");

            HtmlNode node = nodes.FirstOrDefault(n => n.ChildNodes[0].InnerText.Equals(stationName));
            return node.ChildNodes[1].InnerText;
        }
    }
    public enum BusDir
    {
        go, back,
    };
    public struct BusStatDir
    {
        public string bus;
        public string station;
        public BusDir dir;
    }



}

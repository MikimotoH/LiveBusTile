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
        //const string route_url = @"http://pda.5284.com.tw/MQS/businfo2.jsp?routename=";
        public static Task<string> GetBusDueTime(BusInfo b)
        {
            return GetBusDueTime(b.m_Name, b.m_Station, b.m_Dir);
        }

        public static async Task<string> GetBusDueTime(string busName, string stationName, BusDir busDir)
        {
            string url = @"http://pda.5284.com.tw/MQS/businfo3.jsp?Mode=1&Dir={1}&Route={0}&Stop={2}".Fmt(
                Uri.EscapeUriString(busName), busDir==BusDir.go?1:0, Uri.EscapeUriString(stationName));

            var client = new HttpClient();
            string strResult = await client.GetStringAsync(new Uri(url));
            var doc = new HtmlDocument();
            doc.LoadHtml(strResult);

            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes(
                "/html/body/center/table/tr[6]/td");
            if (nodes.Count == 0)
            {
                ScheduledAgent.m_Logger.Debug("nodes.Count == 0");
                return "解析錯誤";
            }
            return nodes[0].InnerText;
        }
    }
}

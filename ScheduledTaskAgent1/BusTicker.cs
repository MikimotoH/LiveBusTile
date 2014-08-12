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
        public static string Pda5284Url(BusInfo b)
        {
            return @"http://pda.5284.com.tw/MQS/businfo3.jsp?Mode=1&Dir={1}&Route={0}&Stop={2}".Fmt(
                Uri.EscapeUriString(b.m_Name), b.m_Dir == BusDir.go ? 1 : 0, Uri.EscapeUriString(b.m_Station));
        }

        public static async Task<string> GetBusDueTime(BusInfo b)
        {
            var client = new HttpClient();
            return ParseHtmlBusTime(await client.GetStringAsync(new Uri(Pda5284Url(b))));
        }


        public static string ParseHtmlBusTime(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            try
            {
                HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes(
                    "/html/body/center/table/tr[6]/td");
                if (nodes.Count == 0)
                    return "節點找不到";
                return nodes[0].InnerText;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.DumpStr());
                Logger.Error("HTML=\n" + html);
                return "解析異常";
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using ScheduledTaskAgent1;
using System.Diagnostics;
using Microsoft.Phone.Controls;

namespace LiveBusTile.ViewModels
{
    class RunTimeBusCatVM
    {
        BusCat[] m_BusCats;
        const string url_businfo = @"http://pda.5284.com.tw/MQS/businfo1.jsp";

        public async void UpdateFromWebAsync(PhoneApplicationPage mainPage)
        {
            var client = new HttpClient();
            //Debug.WriteLine("{0} client.GetStringAsync start", DateTime.Now.ToString("HH:mm:ss.fff"));
            Stopwatch sw = new Stopwatch();
            sw.Start();
            string businfohtml = await client.GetStringAsync(new Uri(@"http://pda.5284.com.tw/MQS/businfo1.jsp"));
            sw.Stop();
            Logger.Debug(String.Format("Url {0} takes {1} ms", url_businfo, sw.Elapsed.TotalMilliseconds));
            //Logger.Debug(String.Format("{0} client.GetStringAsync end", DateTime.Now.ToString("HH:mm:ss.fff")));

            var doc = new HtmlDocument();
            HtmlNode.ElementsFlags.Remove("option");
            doc.LoadHtml(businfohtml);
            var list = new List<BusCat>();
            for (int i = 5; i <= 14; ++i)
            {
                HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("/html/body/center/table[1]/tr[" + i.ToString() + "]/td/select/option");
                string regionName = nodes[0].InnerText.Replace("->", "").Replace("選擇", "");
                nodes.RemoveAt(0);
                var texts = from node in nodes select new BusCat { busName = node.InnerText, region = regionName };
                list.AddRange(texts);
            }
            m_BusCats = list.ToArray();
            mainPage.DataContext = this;
        }


        public List<KeyedList<string, BusCat>> GroupedBuses
        {
            get{
                var buses = m_BusCats;
                var grouped = 
                    from bus in buses
                    orderby bus.region
                    group bus by bus.region into busByRegion
                    select new KeyedList<string,BusCat>(busByRegion);

                return new List<KeyedList<string,BusCat>>(grouped);
            }
        }
    }

}

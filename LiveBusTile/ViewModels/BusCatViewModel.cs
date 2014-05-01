using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using ScheduledTaskAgent1;
using System.Diagnostics;

namespace LiveBusTile.ViewModels
{
    class BusCatViewModel
    {
        static BusCat[] _busCats = new BusCat[]
        {
            new BusCat{busName="橘2", region = "中和線"}, 
            new BusCat{busName="橘3", region = "中和線"}, 
            new BusCat{busName="藍1", region="板南線"},
            new BusCat{busName="藍2", region="板南線"},
            new BusCat{busName="藍3", region="板南線"},
            new BusCat{busName="藍4", region="板南線"},
            new BusCat{busName="藍5", region="板南線"},
        };

        public List<KeyedList<string, BusCat>> GroupedBuses
        {
            get{
                var grouped =
                    from bus in BusCatViewModel._busCats
                    orderby bus.region
                    group bus by bus.region into busByRegion
                    select new KeyedList<string,BusCat>(busByRegion);

                return new List<KeyedList<string,BusCat>>(grouped);
            }
        }
    }

}

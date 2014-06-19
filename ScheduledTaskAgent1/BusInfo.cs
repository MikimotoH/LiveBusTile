using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduledTaskAgent1
{
    public class BusInfo
    {
        public string Name{get;set;}
        public string Station{get;set;}
        public BusDir Dir { get; set; }
        public string TimeToArrive { get; set; }
    }

    public class BusListVM
    {
        public string ListName { get; set; }
        ObservableCollection<BusInfo> Buses { get; set; }
    }

    public static class Database
    {
        public static void SaveBuses()
        {

        }
    }


}

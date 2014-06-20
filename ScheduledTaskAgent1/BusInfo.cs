using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;
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

        public override string ToString()
        {
            return "{{Name=\"{0}\",Station=\"{1}\",Dir={2},TimeToArrive=\"{3}\" }}".Fmt(Name,Station,Dir,TimeToArrive);
        }
    }

    public class BusListVM
    {
        public string ListName { get; set; }
        public ObservableCollection<BusInfo> BusList { get; set; }
    }


    public class ListBusListVM : ObservableCollection<BusListVM>
    {
        public ObservableCollection<BusListVM> BusList { get { return this; }  }
    }

    public static class Database
    {
        public static ListBusListVM ListBusList{get;set;}

        public static void SaveBuses()
        {
            IsolatedStorageSettings.ApplicationSettings["ListBusList"] = ListBusList;
        }

        public static void LoadBuses() 
        {
            if (!IsolatedStorageSettings.ApplicationSettings.Contains("ListBusList"))
            {
                ListBusList = new ListBusListVM();
                return;
            }
            ListBusList = IsolatedStorageSettings.ApplicationSettings["ListBusList"] as ListBusListVM;
        }
    }


}

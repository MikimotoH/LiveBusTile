using LiveBusTile.ViewModels;
using Newtonsoft.Json;
using ScheduledTaskAgent1;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LiveBusTile.Services
{
    public static class DataService
    {
        public static ObservableCollection<BusTagVM> m_list;

        //public static ObservableCollection<BusTagVM> m_list = new ObservableCollection<BusTagVM>
        //{
        //        new BusTagVM{ tag="上班", busName="橘2", station="秀山國小", dir=BusDir.back, timeToArrive="12分"},
        //        new BusTagVM{ tag="上班", busName="275", station="秀山村", dir=BusDir.go, timeToArrive="無資料"},
        //        new BusTagVM{ tag="上班", busName="敦化幹線", station="秀景里", dir=BusDir.back, timeToArrive="未班車已過"},
        //        new BusTagVM{ tag="回家", busName="信義", station="公館", dir=BusDir.go, timeToArrive="已過站"},
        //        new BusTagVM{ tag="回家", busName="254", station="世貿", dir=BusDir.go, timeToArrive="22分"},
        //        new BusTagVM{ tag="回家", busName="藍12", station="台北101", dir=BusDir.go, timeToArrive="90分"},
        //};

        static Random rnd = new Random();
        static Dictionary<string, StationPair> m_all_buses;
        
        public static BusTag RandomBusTag()
        {
            if(m_all_buses==null)
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.NullValueHandling = NullValueHandling.Ignore;
                var sri = Application.GetResourceStream(new Uri("Assets/buses_simple.json", UriKind.Relative));
                using (StreamReader sr = new StreamReader(sri.Stream))
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    m_all_buses = serializer.Deserialize(reader, typeof(Dictionary<string, StationPair>)) as Dictionary<string, StationPair>;
                }
            }
            string busName = m_all_buses.Keys.ElementAt(rnd.Next(m_all_buses.Keys.Count));
            StationPair st = m_all_buses[busName];

            int k = rnd.Next(st.stations_go.Length + st.stations_back.Length);
            if(k < st.stations_go.Length)
                return new BusTag{ busName = busName, dir=BusDir.go, station=st.stations_go[k], tag="亂數"};
            else
                return new BusTag { busName = busName, dir = BusDir.back, station = st.stations_back[k - st.stations_go.Length], tag = "亂數" };
        }

        public static ObservableCollection<BusTagVM> GetBuses()
        {
            if (m_list==null)
            {
                List<BusTag> busList;
                JsonSerializer serializer = new JsonSerializer();
                serializer.NullValueHandling = NullValueHandling.Ignore;
                var sri = Application.GetResourceStream(new Uri("Assets/my_bustags.json", UriKind.Relative));
                using (StreamReader sr = new StreamReader(sri.Stream))
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    busList = serializer.Deserialize(reader, typeof(List<BusTag>)) as List<BusTag>;
                }
                m_list = new ObservableCollection<BusTagVM>();
                foreach (var b in busList)
                    m_list.Add(new BusTagVM(b));
            }
            return m_list;
        }
    }
}

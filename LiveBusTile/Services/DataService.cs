﻿using LiveBusTile.ViewModels;
using Newtonsoft.Json;
using ScheduledTaskAgent1;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LiveBusTile.Services
{
    public static class DataService
    {
        static ObservableCollection<BusTagVM> m_busTags;
        public static bool IsDesignTime = true;

        static DataService()
        {
            Debug.WriteLine("DataService ctor()");
        }
        
        public static ObservableCollection<BusTagVM> BusTags
        {
            get{
                if (m_busTags == null)
                    LoadData();
                return m_busTags;
            }
            set
            {
                m_busTags = value;
            }
        }

        public static void AddBus(BusTag bus)
        {
            if (m_busTags == null)
                LoadData();
            m_busTags.Add(new BusTagVM(bus));
        }

        public static void LoadData()
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;

            if (IsDesignTime)
            {
                using (StreamReader sr = new StreamReader(Application.GetResourceStream(new Uri("Data/default_bustags.json", UriKind.Relative)).Stream))
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    var buses = serializer.Deserialize(reader, typeof(List<BusTag>)) as List<BusTag>;
                    m_busTags = new ObservableCollection<BusTagVM>(buses.Select(x=>new BusTagVM(x)));
                }
                return;
            }

            if (!IsolatedStorageFile.GetUserStoreForApplication().FileExists((@"Shared\ShellContent\saved_buses.json")))
            {
                using (StreamReader sr = new StreamReader(Application.GetResourceStream(new Uri("Data/default_bustags.json", UriKind.Relative)).Stream))
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    var buses = serializer.Deserialize(reader, typeof(List<BusTag>)) as List<BusTag>;
                    m_busTags = new ObservableCollection<BusTagVM>(buses.Select(x => new BusTagVM(x)));
                }
                return;
            }

            using (StreamReader sr = new StreamReader(
                IsolatedStorageFile.GetUserStoreForApplication().OpenFile(@"Shared\ShellContent\saved_buses.json",
                FileMode.Open, FileAccess.Read)))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                var buses = serializer.Deserialize(reader, typeof(List<BusTag>)) as List<BusTag>;
                m_busTags = new ObservableCollection<BusTagVM>(buses.Select(x => new BusTagVM(x)));
            }
        }

        public static void SaveData()
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;

            using (StreamWriter sw = new StreamWriter(
                IsolatedStorageFile.GetUserStoreForApplication().OpenFile(@"Shared\ShellContent\saved_buses.json",
                FileMode.Create, FileAccess.Write)))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                var buses = m_busTags.Select(x => new BusTag { busName = x.busName, station = x.station, dir = x.dir, tag = x.tag }).ToArray();
                serializer.Serialize(writer, buses);
            }
        }

        #region AllBuses
        static Dictionary<string, StationPair> m_all_buses;
        public static Dictionary<string, StationPair> AllBuses
        {
            get
            {
                if (m_all_buses == null)
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.NullValueHandling = NullValueHandling.Ignore;
                    var sri = Application.GetResourceStream(new Uri("Data/buses_simple.json", UriKind.Relative));
                    using (StreamReader sr = new StreamReader(sri.Stream))
                    using (JsonReader reader = new JsonTextReader(sr))
                    {
                        m_all_buses = serializer.Deserialize(reader, typeof(Dictionary<string, StationPair>)) as Dictionary<string, StationPair>;
                    }
                }
                return m_all_buses;
            }
        }
        #endregion //AllBuses

        static Random rnd = new Random();
        public static BusTag RandomBusTag()
        {
            string busName = AllBuses.Keys.ElementAt(rnd.Next(AllBuses.Keys.Count));
            StationPair st = AllBuses[busName];

            int k = rnd.Next(st.stations_go.Length + st.stations_back.Length);
            if (k < st.stations_go.Length)
                return new BusTag { busName = busName, dir = BusDir.go, station = st.stations_go[k], tag = "亂數" };
            else
                return new BusTag { busName = busName, dir = BusDir.back, station = st.stations_back[k - st.stations_go.Length], tag = "亂數" };
        }
    }
}

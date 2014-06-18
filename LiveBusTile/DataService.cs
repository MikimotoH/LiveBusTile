using LiveBusTile.ViewModels;
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
using Log = ScheduledTaskAgent1.Logger;

namespace LiveBusTile
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

        public static void DeleteBus(BusTagVM item)
        {
            bool removeSuccess = m_busTags.Remove(item);
            Log.Debug("removeSuccess=" + removeSuccess);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        static void LoadDefaultData()
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;
            using (StreamReader sr = new StreamReader(Application.GetResourceStream(new Uri("Data/default_bustags.json", UriKind.Relative)).Stream))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                var buses = serializer.Deserialize(reader, typeof(BusTag[])) as BusTag[];
                m_busTags = new ObservableCollection<BusTagVM>(buses.Select(x => new BusTagVM(x)));
            }
        }

        static Object m_mutex = new Object();
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public static void LoadData()
        {
            lock (m_mutex)
            {
                Log.Msg("enter");
                JsonSerializer serializer = new JsonSerializer();
                serializer.NullValueHandling = NullValueHandling.Ignore;

                if (IsDesignTime
                    || !IsolatedStorageFile.GetUserStoreForApplication().FileExists((@"Shared\ShellContent\saved_buses.json")))
                {
                    LoadDefaultData();
                    Log.Msg("exit");
                    return;
                }

                using (StreamReader sr = new StreamReader(
                    IsolatedStorageFile.GetUserStoreForApplication().OpenFile(@"Shared\ShellContent\saved_buses.json",
                    FileMode.Open, FileAccess.Read, FileShare.Read)))
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    var buses = serializer.Deserialize(reader, typeof(List<BusTag>)) as List<BusTag>;
                    if (buses == null || buses.Count() == 0)
                    {
                        Log.Error("File \"{0}\" is corrupted!".Fmt(@"Shared\ShellContent\saved_buses.json"));
                        //LoadDefaultData();
                        m_busTags = new ObservableCollection<BusTagVM>();
                        Log.Msg("exit");
                        return;
                    }
                    m_busTags = new ObservableCollection<BusTagVM>(buses.Select(x => new BusTagVM(x)));
                }
                Log.Msg("exit");
            }
        }

        public static void SaveData()
        {
            lock (m_mutex)
            {
                Log.Msg("enter");
                try
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.NullValueHandling = NullValueHandling.Ignore;

                    using (StreamWriter sw = new StreamWriter(
                        IsolatedStorageFile.GetUserStoreForApplication().OpenFile(@"Shared\ShellContent\saved_buses.json",
                        FileMode.OpenOrCreate, FileAccess.Write, FileShare.None)))
                    using (JsonWriter writer = new JsonTextWriter(sw))
                    {
                        var buses = m_busTags.Select(x => x.BusTag).ToArray();

                        serializer.Serialize(writer, buses);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("ex="+ex.DumpStr());
                }
                Log.Msg("exit");
            }
        }

        #region AllBuses
        static Dictionary<string, StationPair> m_all_buses;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
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

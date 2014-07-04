using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace ScheduledTaskAgent1
{
    public static class Database
    {
        private static List<BusGroup> m_FavBusGroups;
        public static List<BusGroup> FavBusGroups
        {
            get
            {
                if (m_FavBusGroups == null)
                {
                    m_FavBusGroups = LoadFavBusGroups();
                }
                return m_FavBusGroups;
            }
        }

        static Database()
        {
            if (!IsolatedStorageSettings.ApplicationSettings.Contains("LastUpdatedTime"))
                IsolatedStorageSettings.ApplicationSettings["LastUpdatedTime"] = DateTime.MinValue;

            if (!IsolatedStorageSettings.ApplicationSettings.Contains("UseAsyncAwait"))
                IsolatedStorageSettings.ApplicationSettings["UseAsyncAwait"] = false;
            
            if (!IsolatedStorageSettings.ApplicationSettings.Contains("WiFiOnly"))
                IsolatedStorageSettings.ApplicationSettings["WiFiOnly"] = Convert.ToBoolean(ScheduledTaskAgent1.Resource1.IsWiFiOnly_Default);

        }


        public static void SaveFavBusGroups()
        {
            IsolatedStorageSettings.ApplicationSettings["LastUpdatedTime"] = DateTime.Now;
            IsolatedStorageSettings.ApplicationSettings["FavBusGroups"] = m_FavBusGroups;
        }

        public static DateTime LastUpdatedTime
        {
            get{
                if (!IsolatedStorageSettings.ApplicationSettings.Contains("LastUpdatedTime"))
                    IsolatedStorageSettings.ApplicationSettings["LastUpdatedTime"] = DateTime.MinValue;
                return (DateTime)IsolatedStorageSettings.ApplicationSettings["LastUpdatedTime"];
            }
        }

        public static List<BusGroup> DefaultFavBusGroups
        {
            get
            {
                return new List<BusGroup>
                {
                    new BusGroup
                    {
                        m_GroupName = "上班",
                        m_Buses = new List<BusInfo>
                        {
                            new BusInfo{m_Name="橘2", m_Dir=BusDir.go, m_Station="秀山國小", m_TimeToArrive="無資料"},
                            new BusInfo{m_Name="敦化幹線", m_Dir=BusDir.back, m_Station="秀景里", m_TimeToArrive="無資料"},
                        }
                    },
                    new BusGroup
                    {
                        m_GroupName = "回家",
                        m_Buses = new List<BusInfo>
                        { 
                            new BusInfo{m_Name="橘2", m_Dir=BusDir.back, m_Station="捷運永安市場站", m_TimeToArrive="無資料"} ,
                            new BusInfo{m_Name="275", m_Dir=BusDir.back, m_Station="忠孝敦化路口", m_TimeToArrive="無資料"} ,
                        }
                    }
                };
            }
        }

        public static List<BusGroup> LoadFavBusGroups()
        {
            List<BusGroup> ret = null;
            if (!IsolatedStorageSettings.ApplicationSettings.Contains("FavBusGroups"))
            {
                ret = DefaultFavBusGroups;
                IsolatedStorageSettings.ApplicationSettings["FavBusGroups"] = ret;
            }
            else
            {
                ret = IsolatedStorageSettings.ApplicationSettings["FavBusGroups"] as List<BusGroup>;
                if (ret == null)
                {
                    ret = DefaultFavBusGroups;
                    IsolatedStorageSettings.ApplicationSettings["FavBusGroups"] = ret;
                }
            }
            return ret;
        }

        #region serialization
        public const string field_separator = "   ";
        public static void ExportFavBusGroups()
        {
            using (StreamWriter sw = new StreamWriter(IsolatedStorageFile.GetUserStoreForApplication().
                OpenFile(@"Shared\ShellContent\FavBusGroups.txt", FileMode.OpenOrCreate,
                FileAccess.Write, FileShare.None)))
            {
                foreach (var y in m_FavBusGroups)
                {
                    sw.WriteLine(y.m_GroupName + field_separator + y.m_Buses.Count);
                    foreach (var x in y.m_Buses)
                        sw.WriteLine(field_separator +
                            field_separator.Joyn(new string[] { x.m_Name, x.m_Station, x.m_Dir.ToString(), x.m_TimeToArrive }));
                }
            }
        }


        public static List<BusGroup> ImportFavBusGroups()
        {
            using (StreamReader sr = new StreamReader(IsolatedStorageFile.GetUserStoreForApplication().
                OpenFile(@"Shared\ShellContent\FavBusGroups.txt", FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                var ret = new List<BusGroup>();
                string groupDecl;
                while ((groupDecl = sr.ReadLine()) != null)
                {
                    var groupcomps = groupDecl.Split(new string[] { field_separator }, StringSplitOptions.RemoveEmptyEntries);
                    int busCount = int.Parse(groupcomps[1]);
                    var bg = new BusGroup();
                    bg.m_GroupName = groupcomps[0];
                    bg.m_Buses = new List<BusInfo>();
                    for (int i = 0; i < busCount; ++i)
                    {
                        string busLine = sr.ReadLine();
                        var busComps = busLine.Split(new string[] { field_separator }, StringSplitOptions.RemoveEmptyEntries);

                        bg.m_Buses.Add(new BusInfo
                        {
                            m_Name = busComps[0],
                            m_Station = busComps[1],
                            m_Dir = (BusDir)Enum.Parse(typeof(BusDir), busComps[2]),
                            m_TimeToArrive = busComps[3]
                        });
                    }
                    ret.Add(bg);
                }
                return ret;
            }
        }
        #endregion


        static Dictionary<string, StationPair> m_all_buses = null;
        public static void LoadAllBuses()
        {
            m_all_buses = new Dictionary<string, StationPair>();
            var sri = Application.GetResourceStream(new Uri("Data/all_buses.txt", UriKind.Relative));
            using (StreamReader sr = new StreamReader(sri.Stream))
            {
                string busName;
                while ((busName = sr.ReadLine()) != null)
                {
                    var stp = new StationPair();
                    string stations_go_line = sr.ReadLine();
                    stp.stations_go = stations_go_line.Split(new string[] { field_separator }, StringSplitOptions.RemoveEmptyEntries);
                    string stations_back_line = sr.ReadLine();
                    stp.stations_back = stations_back_line.Split(new string[] { field_separator }, StringSplitOptions.RemoveEmptyEntries);
                    m_all_buses[busName] = stp;
                }
            }
        }

        public static Dictionary<string, StationPair> AllBuses
        {
            get
            {
                if (m_all_buses == null)
                    LoadAllBuses();
                return m_all_buses;
            }
        }

        public static int TotalFavBuses
        {
            get
            {
                return FavBusGroups.Sum(y => y.m_Buses.Count);
            }
        }

        public static BusInfo[] FavBuses
        {
            get
            {
                BusInfo[] buses = new BusInfo[TotalFavBuses];
                int k = 0;
                foreach (var y in FavBusGroups)
                {
                    foreach (var x in y.m_Buses)
                        buses[k++] = x;
                }
                return buses;
            }
        }

        public static bool IsLegalGroupName(string p)
        {
            if (String.IsNullOrEmpty(p)
                || String.IsNullOrWhiteSpace(p)
                || p.Contains(Database.field_separator)
                || p.IndexOfAny("/\\:;*?%\"'#!<>|%&=\t".ToArray()) != -1
                || p.Length > p.Trim().Length
                )
                return false;
            return true;
        }


        public static string BusKeyName(string busName)
        {
            if (Regex.IsMatch(busName, @"^\d{4}") && busName.Contains("→"))
                return "公路客運";
            else if (Regex.IsMatch(busName, @"^\d{1,4}[^\d]*$"))
                return "0000~9999";
            else if (Regex.IsMatch(busName, @"^紅"))
                return "紅";
            else if (Regex.IsMatch(busName, @"^藍"))
                return "藍";
            else if (Regex.IsMatch(busName, @"^棕"))
                return "棕";
            else if (Regex.IsMatch(busName, @"^綠"))
                return "綠";
            else if (Regex.IsMatch(busName, @"^橘"))
                return "橘";
            else if (Regex.IsMatch(busName, @"^小"))
                return "小";
            else if (Regex.IsMatch(busName, @"^市民"))
                return "市民";
            else if (Regex.IsMatch(busName, @"^F"))
                return "Ｆ";
            else if (Regex.IsMatch(busName, @"幹線"))
                return "幹線";
            else if (Regex.IsMatch(busName, @"先導") || Regex.IsMatch(busName, @"環狀"))
                return "先導環狀";
            else if (Regex.IsMatch(busName, @"通勤"))
                return "通勤";
            else
                return "其他";
        }
    }
}

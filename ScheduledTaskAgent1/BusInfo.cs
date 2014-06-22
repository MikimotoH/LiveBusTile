using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ScheduledTaskAgent1
{
    public class BusInfo : INotifyPropertyChanged
    {
        string m_Name;
        string m_Station;
        string m_TimeToArrive;

        public string Name { get { return m_Name; } set { if (m_Name != value) { m_Name = value; NotifyPropertyChanged("Name"); } } }
        public string Station { get { return m_Station; } set { if (m_Station != value) { m_Station = value; NotifyPropertyChanged("Station"); } } }
        public BusDir Dir { get; set; }
        public string TimeToArrive { get { return m_TimeToArrive; } set { if (m_TimeToArrive != value) { m_TimeToArrive = value; NotifyPropertyChanged("TimeToArrive"); } } }

        public override string ToString()
        {
            return "{{Name=\"{0}\",Station=\"{1}\",Dir={2},TimeToArrive=\"{3}\" }}".Fmt(Name,Station,Dir,TimeToArrive);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            if (null != PropertyChanged)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class BusGroup : INotifyPropertyChanged
    {
        string m_GroupName;
        public string GroupName { get { return m_GroupName; } set { if (m_GroupName != value) { m_GroupName = value; NotifyPropertyChanged("GroupName"); } } }
        public ObservableCollection<BusInfo> Buses { get; set; }
        
        public BusGroup()
        {
            Buses = new ObservableCollection<BusInfo>();
        }
        public override string ToString()
        {
            return "GroupName{0}, Buse={1}".Fmt(GroupName, Buses.DumpArray() );
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            if (null != PropertyChanged)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class StationPair
    {
        public string[] stations_go;
        public string[] stations_back;

        public string[] GetStations(BusDir m_dir)
        {
            return m_dir == BusDir.go ? stations_go : stations_back;
        }
    }


    public static class Database
    {
        static ObservableCollection<BusGroup> m_FavBusGroups;
        public static ObservableCollection<BusGroup> FavBusGroups
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

        public static void SaveFavBusGroups()
        {
            IsolatedStorageSettings.ApplicationSettings["FavBusGroups"] = FavBusGroups;
        }

        public static ObservableCollection<BusGroup> DefaultFavBusGroups
        {
            get
            {
                return new ObservableCollection<BusGroup>
                {
                    new BusGroup
                    {
                        GroupName = "上班",
                        Buses = new ObservableCollection<BusInfo>
                        {
                            new BusInfo{Name="橘2", Dir=BusDir.go, Station="秀山國小", TimeToArrive="無資料"},
                            new BusInfo{Name="敦化幹線", Dir=BusDir.back, Station="秀景里", TimeToArrive="無資料"},
                        }
                    },
                    new BusGroup
                    {
                        GroupName = "回家",
                        Buses = new ObservableCollection<BusInfo>
                        { 
                            new BusInfo{Name="橘2", Dir=BusDir.back, Station="捷運永安市場站", TimeToArrive="無資料"} ,
                            new BusInfo{Name="275", Dir=BusDir.back, Station="忠孝敦化路口", TimeToArrive="無資料"} ,
                        }
                    }
                };
            }
        }

        public static ObservableCollection<BusGroup> LoadFavBusGroups() 
        {
            if (!IsolatedStorageSettings.ApplicationSettings.Contains("FavBusGroups"))
            {
                IsolatedStorageSettings.ApplicationSettings["FavBusGroups"] = DefaultFavBusGroups;
            }
            return IsolatedStorageSettings.ApplicationSettings["FavBusGroups"] as ObservableCollection<BusGroup>;
        }

        #region serialization
        public const string field_separator = "   ";
        public static void ExportFavBusGroups()
        {
            using (StreamWriter sw = new StreamWriter(IsolatedStorageFile.GetUserStoreForApplication().
                OpenFile(@"Shared\ShellContent\FavBusGroups.txt",FileMode.OpenOrCreate, 
                FileAccess.Write, FileShare.None)))
            {
                foreach(var y in FavBusGroups)
                {
                    sw.WriteLine(y.GroupName + field_separator + y.Buses.Count);
                    foreach( var x in y.Buses)
                        sw.WriteLine(field_separator +
                            field_separator.Joyn(new string[] { x.Name, x.Station, x.Dir.ToString(), x.TimeToArrive }));
                }
            }
        }


        public static ObservableCollection<BusGroup> ImportFavBusGroups()
        {
            using(StreamReader sr = new StreamReader(IsolatedStorageFile.GetUserStoreForApplication().
                OpenFile(@"Shared\ShellContent\FavBusGroups.txt",FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                ObservableCollection<BusGroup> ret = new ObservableCollection<BusGroup>();
                string groupDecl;
                while ((groupDecl = sr.ReadLine()) != null)
                {
                    var groupcomps = groupDecl.Split(new string[] { field_separator }, StringSplitOptions.RemoveEmptyEntries);
                    int busCount = int.Parse(groupcomps[1]);
                    var bg = new BusGroup();
                    bg.GroupName = groupcomps[0];
                    bg.Buses = new ObservableCollection<BusInfo>();
                    for(int i=0; i<busCount; ++i)
                    {
                        string busLine = sr.ReadLine();
                        var busComps = busLine.Split(new string[] { field_separator }, StringSplitOptions.RemoveEmptyEntries);
                        var bi = new BusInfo{Name=busComps[0], Station=busComps[1], Dir=(BusDir)Enum.Parse(typeof(BusDir), busComps[2]), TimeToArrive=busComps[3]};
                        bg.Buses.Add(bi);
                    }
                    ret.Add(bg);
                }
                return ret;
            }
        }
        #endregion


        static Dictionary<string, StationPair> m_all_buses=null;
        public static void LoadAllBuses()
        {
            m_all_buses = new Dictionary<string, StationPair>();
            var sri = Application.GetResourceStream(new Uri("Data/all_buses.txt", UriKind.Relative));
            using (StreamReader sr = new StreamReader(sri.Stream))
            {
                string busName;
                while((busName = sr.ReadLine()) != null)
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
                return FavBusGroups.Sum(y => y.Buses.Count);
            }
        }

        public static BusInfo[] FavBuses
        { 
            get
            {
                BusInfo[] buses = new BusInfo[TotalFavBuses];
                int k=0;
                foreach (var y in FavBusGroups)
                {
                    foreach (var x in y.Buses)
                        buses[k++] = x;
                }
                return buses;
            }
        }

        public static bool IsLegalGroupName(string p)
        {
            if (String.IsNullOrEmpty(p)
                || String.IsNullOrWhiteSpace(p)
                || p.Contains(Database.field_separator))
                return false;
            return true;
        }
    }
}

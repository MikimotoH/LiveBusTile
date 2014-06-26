using ScheduledTaskAgent1;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveBusTile
{


    public class BusGroupVM : INotifyPropertyChanged
    {
        public BusGroupVM() { }
        public BusGroupVM(BusGroup bg)
        {
            m_base = bg;
        }

        BusGroup m_base;

        public BusGroupVM(string groupName, IEnumerable<BusInfo> busInfos)
        {
            m_base.m_GroupName = String.Copy(groupName);
            m_base.m_Buses = new List<BusInfo>(busInfos);
        }

        public string GroupName { 
            get { return m_base.m_GroupName; } 
            set { if (m_base.m_GroupName != value) { m_base.m_GroupName = value; NotifyPropertyChanged("GroupName"); } } 
        }

        public ObservableCollection<BusInfoVM> Buses {
            get { return m_base.m_Buses.Select(x => new BusInfoVM(x)).ToObservableCollection(); }
            set 
            {
                m_base.m_Buses = value.Select(b=>b.GetBase()).ToList();
                NotifyPropertyChanged("Buses");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            if (null != PropertyChanged)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public static class DatabaseVM
    {
        public static ObservableCollection<BusGroupVM> FavBusGroupsVM
        {
            get
            {
                return Database.FavBusGroups.Select(x => new BusGroupVM(x)).ToObservableCollection();
            }
        }
    }


    public class ExampleAllBuses : ObservableCollection<KeyedBusVM>
    {
        public ExampleAllBuses()
        {
            Add(new KeyedBusVM("綠", new string[] { "綠1", "綠2", "綠3", "綠4", "綠5" }));
            Add(new KeyedBusVM("幹線", new string[] { "敦化幹線", "信義幹線", "仁愛幹線", "信義幹線" }));
        }
    }

    public class ExampleBusInfos : ObservableCollection<BusInfoVM>
    {
        public ExampleBusInfos()
        {
            Add(new BusInfoVM { Name = "橘2", Station = "秀山國小", Dir = BusDir.go });
            Add(new BusInfoVM { Name = "敦化幹線", Station = "秀景里", Dir = BusDir.back });
        }
    }

}

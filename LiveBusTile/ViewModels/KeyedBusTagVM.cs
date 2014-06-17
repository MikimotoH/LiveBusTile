using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveBusTile.ViewModels
{
    public class KeyedBusTagVM : ObservableCollection<BusTagVM>
    {
        string m_key;
        public string Key { get { return m_key; } set { m_key = value; NotifyPropertyChanged("Key"); } }

        public KeyedBusTagVM()
        {
        }

        KeyedBusTagVM(IGrouping<string, BusTagVM> group) 
            :base(group)
        {
            this.m_key = group.Key;
        }


        public ObservableCollection<KeyedBusTagVM> GroupedBuses
        {
            get
            {
                IEnumerable<KeyedBusTagVM> groupedBuses =
                    from bus in DataService.BusTags
                    orderby bus.tag
                    group bus by bus.tag into busesByTag
                    select new KeyedBusTagVM(busesByTag);

                return new ObservableCollection<KeyedBusTagVM>(groupedBuses);
            }
        }


        public new event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}

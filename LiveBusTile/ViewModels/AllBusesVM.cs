using LiveBusTile.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveBusTile.ViewModels
{
    public class AllBusesVM : ObservableCollection<StringVM>
    {
        public AllBusesVM()
        {
        }

        public ObservableCollection<StringVM> AllBuses
        {
            get
            {
                return new ObservableCollection<StringVM>(DataService.AllBuses.Keys.Select(x => new StringVM { String = x }));
            }
        }

        public ObservableCollection<StringVM> SampledStations
        {
            get
            {
                return new ObservableCollection<StringVM>(DataService.AllBuses["1"].stations_go.Select(x => new StringVM { String=x}));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
    public class StringVM 
    {
        public StringVM() { }
        public StringVM(string s) { this.String = s; }
        public string String { get; set; }
    }
}

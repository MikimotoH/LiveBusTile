using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveBusTile.ViewModels
{
    public class AllBusesVM
    {
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
        public ObservableCollection<StringVM> SampledTags
        {
            get
            {
                return new ObservableCollection<StringVM> { new StringVM("上班"), new StringVM("回家") };
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

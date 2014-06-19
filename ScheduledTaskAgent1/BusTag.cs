using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduledTaskAgent1
{
    public class BusTag
    {
        public BusTag() { }
        public BusTag(BusTag b) { 
            busName = b.busName; 
            tag = b.tag; 
            dir = b.dir; 
            station = b.station; 
            timeToArrive = b.timeToArrive; 
        }

        public string busName ;
        public string tag ;
        public BusDir dir ;
        public string station ;
        public string timeToArrive;
        public override string ToString()
        {
            return "{{ busName=\"{0}\",station=\"{1}\",dir={2},tag=\"{3}\",timeToArrive=\"{4}\"}}".
                Fmt(busName, station, dir, tag, timeToArrive);
        }
    }

    public class BusTagVM : BusTag, INotifyPropertyChanged
    {
        public BusTagVM() { }
        public BusTagVM(BusTag b) : base(b) { }

        public BusTag BusTag { get { return new BusTag { busName = this.busName, station = this.station, tag = this.tag, dir = this.dir, timeToArrive = this.timeToArrive }; } }
        public new string busName { get { return base.busName; } set { if (base.busName != value) { base.busName = value; NotifyPropertyChanged("busName"); } } }
        public new string tag { get { return base.tag; } set { if (base.tag != value) { base.tag = value; NotifyPropertyChanged("tag"); } } }
        public new BusDir dir { get { return base.dir; } set { if (base.dir != value) { base.dir = value; NotifyPropertyChanged("dir"); } } }
        public new string station { get { return base.station; } set { if (base.station != value) { base.station = value; NotifyPropertyChanged("station"); } } }
        public new string timeToArrive { get { return base.timeToArrive; } set { if (base.timeToArrive != value) { base.timeToArrive = value; NotifyPropertyChanged("timeToArrive"); } } }

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
    }
}

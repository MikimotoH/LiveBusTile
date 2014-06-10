using ScheduledTaskAgent1;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveBusTile.ViewModels
{
    public class BusTagVM : INotifyPropertyChanged
    {
        public BusTagVM() { }
        public BusTagVM(BusTag b) { self = b; }
        BusTag self = new BusTag();
        public string busName { get { return self.busName; } set { self.busName = value; NotifyPropertyChanged("busName"); } }
        public string tag { get { return self.tag; } set { self.tag = value; NotifyPropertyChanged("tag"); } }
        public BusDir dir { get { return self.dir; } set { self.dir = value; NotifyPropertyChanged("dir"); } }
        public string station { get { return self.station; } set { self.station = value; NotifyPropertyChanged("station"); } }
        public string timeToArrive { get { return self.timeToArrive; } set { self.timeToArrive = value; NotifyPropertyChanged("timeToArrive"); } }

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
}

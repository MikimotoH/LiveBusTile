using ScheduledTaskAgent1;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace LiveBusTile.ViewModels
{
 
    public class BusTagVM : BusTag, INotifyPropertyChanged
    {
        public BusTagVM() { }
        public BusTagVM(BusTag b):base(b) { }
 
        public new string busName { get { return base.busName; } set { if (base.busName != value) { base.busName = value; NotifyPropertyChanged("busName"); } } }
        public new string tag { get { return base.tag; } set { if (base.tag != value) { base.tag = value; NotifyPropertyChanged("tag"); } } }
        public new BusDir dir { get { return base.dir; } set { if (base.dir != value) { base.dir = value; NotifyPropertyChanged("dir"); } } }
        public new string station { get { return base.station; } set { if (base.station != value) { base.station = value; NotifyPropertyChanged("station"); } } }

        string m_timeToArrive;
        public string timeToArrive { get { return m_timeToArrive; } set { if (m_timeToArrive != value) { m_timeToArrive = value; NotifyPropertyChanged("timeToArrive"); } } }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            if (null != PropertyChanged)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

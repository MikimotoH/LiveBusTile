using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduledTaskAgent1
{
    public class BusInfoVM : INotifyPropertyChanged
    {
        public BusInfoVM() { }
        public BusInfoVM(BusInfo b)
        {
            m_base = b;
        }
        BusInfo m_base;
        public BusInfo GetBase(){return m_base;}


        public string Name { get { return m_base.m_Name; } set { if (m_base.m_Name != value) { m_base.m_Name = value; NotifyPropertyChanged("Name"); } } }
        public string Station { get { return m_base.m_Station; } set { if (m_base.m_Station != value) { m_base.m_Station = value; NotifyPropertyChanged("Station"); } } }
        public BusDir Dir { get { return m_base.m_Dir; } set { if (m_base.m_Dir != value) { m_base.m_Dir = value; NotifyPropertyChanged("Dir"); } } }
        public string TimeToArrive { 
            get { return m_base.m_TimeToArrive; } 
            set { if (m_base.m_TimeToArrive != value) { m_base.m_TimeToArrive = value; NotifyPropertyChanged("TimeToArrive"); } } }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            if (null != PropertyChanged)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        
        public override string ToString()
        {
            return m_base.ToString();
        }

        public string DirWithDestStation
        {
            get
            {
                return m_base.DirWithDestStation;
            }
        }
    }
 
}

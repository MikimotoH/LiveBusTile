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

    public class StationPair
    {
        public string[] stations_go;
        public string[] stations_back;
    }
}

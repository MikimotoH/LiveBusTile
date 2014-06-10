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
        public string busName ;
        public string tag ;
        public BusDir dir ;
        public string station ;
        public string timeToArrive ;
    }

    public class StationPair
    {
        public string[] stations_go;
        public string[] stations_back;
    }
}

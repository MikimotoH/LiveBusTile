using ScheduledTaskAgent1;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveBusTile
{
    public class ExampleData : ListBusListVM
    {
        public ExampleData()
            :base()
        {            
            Add( new BusListVM
                {
                    ListName = "上班",
                    BusList = new ObservableCollection<BusInfo>
                    {
                        new BusInfo{Name="橘2", Dir=BusDir.go, Station="秀山國小", TimeToArrive="20分"},
                        new BusInfo{Name="敦化幹線", Dir=BusDir.back, Station="秀景里", TimeToArrive="快來了"},
                    }
                });
            
            Add( new BusListVM
                    {
                        ListName = "回家",
                        BusList = new ObservableCollection<BusInfo>
                        { 
                            new BusInfo{Name="橘2", Dir=BusDir.back, Station="捷運永安市場站", TimeToArrive="快來了"} ,
                            new BusInfo{Name="275", Dir=BusDir.back, Station="忠孝敦化路口", TimeToArrive="網路障礙"} ,
                        }
                    });
        }
    }
}

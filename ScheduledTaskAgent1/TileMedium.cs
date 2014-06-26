using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ScheduledTaskAgent1
{

    public partial class TileMedium : UserControl
    {
        public TileMedium()
        {
            InitializeComponent();
        }

        public ListBox ListBoxBuses { get { return this.lbBuses; } }
    }


    public class ExampleTileData : List<BusInfoVM>
    {
        public ExampleTileData()
        {
            Add(new BusInfoVM { Name = "橘2", Station = "秀山國小", TimeToArrive = "將到站" });
            Add(new BusInfoVM { Name = "敦化幹線", Station = "秀景里", TimeToArrive = "12分" });

            Add(new BusInfoVM { Name = "橘2", Station = "捷運永安市場站", TimeToArrive = "7分" });
            Add(new BusInfoVM { Name = "275", Station = "忠孝敦化路口", TimeToArrive = "無資料" });
        }
    }
}

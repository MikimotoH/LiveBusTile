using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Collections.ObjectModel;
using ScheduledTaskAgent1;

namespace LiveBusTile
{
    public partial class TileMediumTest : PhoneApplicationPage
    {
        public TileMediumTest()
        {
            InitializeComponent();
        }
    }

    public class ExampleTileData : ObservableCollection<BusInfoVM>
    {
        public ExampleTileData()
        {
            Add(new BusInfoVM { Name = "橘2", Station = "秀山國小", TimeToArrive = "將到站" });
            Add(new BusInfoVM { Name = "敦化幹線", Station = "秀景里", TimeToArrive = "12分" });

            Add(new BusInfoVM { Name = "橘2", Station = "捷運永安市場站", TimeToArrive = "7分" });
            Add(new BusInfoVM { Name = "275", Station = "忠孝敦化路口", TimeToArrive = "進站中" });
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.IO.IsolatedStorage;
using ScheduledTaskAgent1;
using System.Collections.ObjectModel;
using HtmlAgilityPack;
using System.Threading;
using System.Threading.Tasks;

namespace LiveBusTile
{

    public partial class AddStationBuses : PhoneApplicationPage
    {
        public AddStationBuses()
        {
            InitializeComponent();
        }

        string m_GroupName;
        

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            m_GroupName = NavigationContext.QueryString.GetValue("GroupName", "");
            tbStation.Text = NavigationContext.QueryString["Station"];
            lbBuses.ItemsSource = Database.AllStations[tbStation.Text]
                .OrderBy(b => b.bus, new StrNumComparer()).Select(x => new BusAndDirVM(x)).ToList();
        }


        private async void btnEnter_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (lbBuses.SelectedItems == null || lbBuses.SelectedItems.Count == 0)
            {
                MessageBox.Show("未選擇任何站牌");
                return;
            }
            string newGroupName = this.m_GroupName;
            if (newGroupName == "")
                newGroupName = await PromptForGroupName();

            BusGroup bg = Database.FavBusGroups.FirstOrDefault(x => x.m_GroupName == newGroupName);
            if (bg == null)
            {
                bg = new BusGroup { m_GroupName = newGroupName, m_Buses = new List<BusInfo>() };
                Database.FavBusGroups.Add(bg);
            }
            bg.m_Buses.AddRange( lbBuses.SelectedItems.Cast<BusAndDirVM>()
                .Select(x=> new BusInfo{m_Name= x.Base.bus, m_Dir=x.Base.dir, m_Station = this.tbStation.Text } ) );
            
            Database.SaveFavBusGroups();

            PhoneApplicationService.Current.State["Op"] = "Add";

            if (this.m_GroupName == "")
            {
                PhoneApplicationService.Current.State["ScrollToGroup"] = bg.m_GroupName;
                NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
            }
            else
                NavigationService.Navigate(new Uri("/GroupPage.xaml?GroupName=" + m_GroupName, UriKind.Relative));
        }

        private string GenTempGroupName()
        {
            int i = 1;
            for (; ; ++i)
            {
                string ret = "暫時" + i;
                if (Database.FavBusGroups.DoesNotHave(x => x.m_GroupName == ret))
                    return ret;
            }
        }

        Task<string> PromptForGroupName()
        {
            var tcs = new TaskCompletionSource<string>();

            IEnumerable<string> lsGroups = Database.FavBusGroups.Select(g => g.m_GroupName)
                .Concat(new string[] { "上班", "回家", GenTempGroupName() }).RemoveDuplicate();

            ListPicker listPicker = new ListPicker()
            {
                //Header = "Snooze for:",
                ItemsSource = lsGroups,
                //Margin = new Thickness(12, 42, 24, 18)
            };
            listPicker.UpdateLayout();
            listPicker.SelectedItem = listPicker.Items.FirstElement();
            

            CustomMessageBox messageBox = new CustomMessageBox()
            {
                //Title = "Title",
                //Caption = "Caption",
                Message = "選擇群組名稱：",
                Content = listPicker,
                //LeftButtonContent = "snooze",
                RightButtonContent = "確定",
                IsFullScreen = false
            };

            messageBox.Dismissing += (s1, e1) =>
            {
                if (listPicker.ListPickerMode == ListPickerMode.Expanded)
                {
                    e1.Cancel = true;
                }
            };
                        
            messageBox.Dismissed += (s2, e2) =>
            {
                switch (e2.Result)
                {
                    case CustomMessageBoxResult.RightButton:
                        tcs.SetResult(listPicker.SelectedItem.Cast<string>());
                        break;
                }
            };

            messageBox.Show();
            
            return tcs.Task;
        }

        private void tbSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            lbBuses.ItemsSource = Database.AllStations[tbStation.Text]
                .Where( x => x.bus.Contains(tbSearch.Text))
                .OrderBy(b => b.bus, new StrNumComparer()).Select(x => new BusAndDirVM(x)).ToList();
        }

        private void btnSearch_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if(tbSearch.Visibility == Visibility.Visible)
                tbSearch.Visibility = Visibility.Collapsed;
            else
                tbSearch.Visibility = Visibility.Visible;

        }
    }

    public class BusAndDirVM
    {
        public BusAndDirVM() { }
        public BusAndDirVM(BusAndDir x) { 
            m_base = x;
            m_destination = Database.AllBuses[m_base.bus].GetStations(m_base.dir).LastElement();
        }

        public BusAndDirVM(string bus, int dir, string destination) { m_base.bus = bus; m_base.dir = (BusDir)dir; m_destination = destination; }

        BusAndDir m_base = new BusAndDir();
        public BusAndDir Base { get { return m_base; } }

        string m_destination;
        public string Bus { get { return m_base.bus; } }
        public string Destination
        {
            get { 
                return m_destination;
            } 
        }
    }

    public class ExampleBusAndDirList : List<BusAndDirVM>
    {
        public ExampleBusAndDirList()
        {
            //臺大醫院
            AddRange(new BusAndDirVM[] 
            {
                new BusAndDirVM("2",0, "臺大醫院"),
	            new BusAndDirVM("15",0, "臺北郵局"),
	            new BusAndDirVM("18",0, "捷運麟光站"),
	            new BusAndDirVM("22",0, "228和平公園"),
	            new BusAndDirVM("37",1, "調度站松德站"),
	            new BusAndDirVM("208",0, "捷運劍南路站(植福)"),
	            new BusAndDirVM("222",0, "衡陽路"),
	            new BusAndDirVM("227",0, "安平路"),
	            new BusAndDirVM("227",1, "三重站"),
	            new BusAndDirVM("261",0, "市議會"),
	            new BusAndDirVM("261",1, "蘆洲總站"),
	            new BusAndDirVM("615",1, "捷運迴龍站"),
	            new BusAndDirVM("648",0, "臺大醫院"),
	            new BusAndDirVM("849",1, "觀光大橋"),
	            new BusAndDirVM("中山幹線",0, "捷運中正紀念堂站(中山)"),
	            new BusAndDirVM("中山幹線",1, "職能發展學院一"),
	            new BusAndDirVM("信義幹線",1, "北興宮"),
	            new BusAndDirVM("信義新幹線",1, "安康站"),
	            new BusAndDirVM("F317",0, "臺大醫院"),
            });
        }
    }
}
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
using System.ComponentModel;
using ScheduledTaskAgent1;
using LiveBusTile.Resources;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.IO.IsolatedStorage;
using HtmlAgilityPack;
using System.Threading;
using System.IO;
using System.Globalization;

namespace LiveBusTile
{
    public partial class MainPage : PhoneApplicationPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private ObservableCollection<GroupBusVM> GenFavGroupBusVM()
        {
            var vm = new ObservableCollection<GroupBusVM>();
            foreach (var y in Database.FavBusGroups)
            {
                vm.Add(new GroupBusVM(y.m_GroupName));
                foreach (var x in y.m_Buses)
                    vm.Add(new GroupBusVM(x, y.m_GroupName));
            }
            return vm;
        }



        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            tbLastUpdatedTime.Text = Database.LastUpdatedTime.ToString(TileUtil.CurSysTimeFormat);
            lbBus.ItemsSource = GenFavGroupBusVM();

            if ((string)PhoneApplicationService.Current.State.GetValue("Op", "") != "")
            {
                while (NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry();
                PhoneApplicationService.Current.State.Remove("Op");
            }
            string groupName = PhoneApplicationService.Current.State.GetValue("ScrollToGroup", "").Cast<string>();
            if ( groupName != "")
            {
                lbBus.UpdateLayout();
                object item = lbBus.Items.FirstOrDefault(o => o.Cast<GroupBusVM>().IsGroupHeader && o.Cast<GroupBusVM>().GroupName == groupName);
                if (item != null)
                    lbBus.ScrollIntoView(item);
            }
        }

        private void BusItem_Delete_Click(object sender, RoutedEventArgs e)
        {
            var gbvm = (sender as MenuItem).DataContext as GroupBusVM;
            BusInfo busInfo = gbvm.BusInfo;

            BusGroup group = Database.FavBusGroups.FirstOrDefault(g => g.m_GroupName == gbvm.GroupName);
            if (group == null)
            {
                AppLogger.Error("can not find group which contains busInfo=" + busInfo);
                return;
            }

            group.m_Buses.Remove(busInfo);
            if (group.m_Buses.Count == 0)
                Database.FavBusGroups.Remove(group);

            lbBus.ItemsSource = GenFavGroupBusVM();
        }

        private void BusItem_Details_Click(object sender, RoutedEventArgs e)
        {
            var gbvm = (sender as MenuItem).DataContext as GroupBusVM;
            GotoDetailsPage(gbvm.BusInfo, gbvm.GroupName);
        }

        void GotoDetailsPage(BusInfo busInfo, string groupName)
        {
            PhoneApplicationService.Current.State["busInfo"] = busInfo;
            PhoneApplicationService.Current.State["groupName"] = groupName;
            NavigationService.Navigate(new Uri(
                "/BusStationDetails.xaml", UriKind.Relative));
        }


        private void AppBar_Pin_Click(object sender, EventArgs e)
        {
            AppLogger.Debug("");
            Database.SaveFavBusGroups();
            GroupPage.CreateTile("");
        }

        class WCVM
        {
            public WebClient wc = new WebClient();
            public GroupBusVM vm;
            public WCVM(GroupBusVM vm)
            {
                this.vm = vm;
                this.wc.Headers = new WebHeaderCollection();
                this.wc.Headers[HttpRequestHeader.IfModifiedSince] = DateTime.UtcNow.ToString("R");
                this.wc.Headers["Cache-Control"] = "no-cache";
                this.wc.Headers["Pragma"] = "no-cache";
            }
        }
        
        WCVM[] m_WCVMs;

        private void AppBar_Refresh_Click(object sender, EventArgs e)
        {
            if (Database.FavBuses.Count() == 0)
                return;

            if (progbar.Visibility == Visibility.Visible)
            {
                m_WCVMs.DoForEach( wcvm => wcvm.wc.CancelAsync());
                return;
            }

            progbar.Visibility = Visibility.Visible;
            ApplicationBar.Buttons.Cast<ApplicationBarIconButton>().DoForEach(x => x.IsEnabled = false);
            ApplicationBarIconButton btnRefresh = sender as ApplicationBarIconButton;
            btnRefresh.IsEnabled = true;
            btnRefresh.IconUri = new Uri("/Images/AppBar.StopRefresh.png", UriKind.Relative);

            m_WCVMs = lbBus.Items.Cast<GroupBusVM>().Where(gb => !gb.IsGroupHeader).Select(b => new WCVM(b)).ToArray();
            int m_CompletedWCs = 0;
            int m_CancelledWCs = 0;
            int m_SucceededWCs = 0;
            
            foreach (var wcvm in m_WCVMs)
            {
                wcvm.wc.DownloadStringCompleted += (s, asyncCompletedEventArgs) =>
                {
                    int completedWCs = Interlocked.Increment(ref m_CompletedWCs);
                    int succeededWCs = Interlocked.Add(ref m_SucceededWCs, asyncCompletedEventArgs.Error == null?1:0);
                    int cancelledWCs = Interlocked.Add(ref m_CancelledWCs, asyncCompletedEventArgs.Cancelled ? 1 : 0);
                    AppLogger.Debug("completedWCs={0}, succeededWCs={1}, cancelledWCs={2}"
                        .Fmt(completedWCs, succeededWCs, cancelledWCs));

                    if (asyncCompletedEventArgs.Error == null)
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            GroupBusVM vm = asyncCompletedEventArgs.UserState as GroupBusVM;
                            vm.TimeToArrive = BusTicker.ParseHtmlBusTime(asyncCompletedEventArgs.Result);
                            tbLastUpdatedTime.Text = DateTime.Now.ToString(TileUtil.CurSysTimeFormat);
                        });
                    }
                    else
                        AppLogger.Error(asyncCompletedEventArgs.Error.DumpStr());

                    if (completedWCs == m_WCVMs.Length)
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            if(succeededWCs > 0)
                            {
                                Database.SaveFavBusGroups();

                                List<string> groupNames = Database.FavBusGroups.Select(x => x.m_GroupName).ToList();
                                groupNames.Insert(0, "");
                                foreach (var groupName in groupNames)
                                    TileUtil.UpdateTile2(groupName);
                            }
                            else if (cancelledWCs == 0)
                            {
                                MessageBox.Show(AppResources.NetworkFault);
                            }
                            
                            btnRefresh.IconUri = new Uri("/Images/AppBar.Refresh.png", UriKind.Relative);
                            ApplicationBar.Buttons.Cast<ApplicationBarIconButton>().DoForEach(x => x.IsEnabled = true);
                            progbar.Visibility = Visibility.Collapsed;
                        });
                    }
                };
                wcvm.wc.DownloadStringAsync(new Uri(BusTicker.Pda5284Url(wcvm.vm.BusInfo)), wcvm.vm);
            }
        }


        private void AppBar_AddBus_Click(object sender, EventArgs e)
        {
            AppLogger.Debug("");
            NavigationService.Navigate(new Uri("/AddBus.xaml", UriKind.Relative));
        }
        private void AppBar_AddStation_Click(object sender, EventArgs e)
        {
            AppLogger.Debug("");
            NavigationService.Navigate(new Uri("/AddStation.xaml", UriKind.Relative));
        }

        private void AppBar_Settings_Click(object sender, EventArgs e)
        {
            AppLogger.Debug("");
            NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));
        }

        private void AppBar_About_Click(object sender, EventArgs e)
        {
            AppLogger.Debug("");
            NavigationService.Navigate(new Uri("/About.xaml", UriKind.Relative));
        }


        private void lbBus_DoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            AppLogger.Debug("");
            var gbvm = (e.OriginalSource as FrameworkElement).DataContext as GroupBusVM;
            BusInfo busInfo = gbvm.BusInfo;
            if (busInfo == null)
            {
                NavigationService.Navigate(new Uri("/GroupPage.xaml?GroupName=" + gbvm.GroupName, UriKind.Relative));
                return;
            }
            
            GotoDetailsPage(busInfo, gbvm.GroupName);
        }

        private void GroupHeader_Delete_Click(object sender, RoutedEventArgs e)
        {
            var gbvm = (sender as MenuItem).DataContext as GroupBusVM;
            string groupName = gbvm.GroupName;

            bool bRemoveOK = Database.FavBusGroups.Remove(Database.FavBusGroups.FirstOrDefault(g=>g.m_GroupName==groupName));
            if (!bRemoveOK)
                AppLogger.Error("Database.FavBusGroups.Remove(m_BusGroup) failed");


            Database.SaveFavBusGroups();
            lbBus.ItemsSource = GenFavGroupBusVM();

            ShellTile tile = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString() == TileUtil.TileUri(groupName));
            if (tile != null)
                tile.Delete();
            TileUtil.UpdateTile2("");

        }



    }

    public class GroupBusVM : INotifyPropertyChanged
    {
        public GroupBusVM() { }
        public GroupBusVM(string groupName) { GroupName = groupName; }
        public GroupBusVM(BusInfo b, string groupName) { m_BusInfo = b; GroupName = groupName; }

        public string GroupName{get;set;}

        BusInfo m_BusInfo;
        public BusInfo BusInfo { get { return m_BusInfo; } }

        public string BusName { get { return m_BusInfo.m_Name; } set { if (m_BusInfo.m_Name != value) { m_BusInfo.m_Name = value; NotifyPropertyChanged("BusName"); } } }
        public string Station { get { return m_BusInfo.m_Station; } set { if (m_BusInfo.m_Station != value) { m_BusInfo.m_Station = value; NotifyPropertyChanged("Station"); } } }

        public string TimeToArrive {
            get { return m_BusInfo.m_TimeToArrive; }
            set { if (m_BusInfo.m_TimeToArrive != value) { m_BusInfo.m_TimeToArrive = value; NotifyPropertyChanged("TimeToArrive"); } } 
        }

        public bool IsGroupHeader { get { return m_BusInfo==null; } }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            if (null != PropertyChanged)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ExampleGroupBusVM : ObservableCollection<GroupBusVM>
    {
        public ExampleGroupBusVM()
        {
            Add(new GroupBusVM("上班") );
            Add(new GroupBusVM(new BusInfo { m_Name = "橘2", m_Station = "秀山國小", m_TimeToArrive = "無資料" }, "上班"));
            Add(new GroupBusVM(new BusInfo { m_Name = "敦化幹線", m_Station = "秀景里", m_TimeToArrive = "無資料" }, "上班"));

            Add(new GroupBusVM ("回家") );
            Add(new GroupBusVM(new BusInfo { m_Name = "橘2", m_Station = "捷運永安市場站", m_TimeToArrive = "無資料" }, "回家"));
            Add(new GroupBusVM(new BusInfo { m_Name = "275", m_Station = "忠孝敦化路口", m_TimeToArrive = "無資料" }, "回家"));
        }
    }

    public abstract class TemplateSelector : ContentControl
    {
        public abstract DataTemplate SelectTemplate(object item, DependencyObject container);

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            ContentTemplate = SelectTemplate(newContent, this);
        }
    }

    public class GroupBusTemplateSelector : TemplateSelector
    {
        public DataTemplate GroupHeader{get;set;}

        public DataTemplate BusInfo{get;set;}

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var gb = item as GroupBusVM;
            if (gb.IsGroupHeader)
                return GroupHeader;
            else
                return BusInfo;
        }
    }

}
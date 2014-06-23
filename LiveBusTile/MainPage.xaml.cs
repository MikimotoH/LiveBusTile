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

namespace LiveBusTile
{
    public partial class MainPage : PhoneApplicationPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private ObservableCollection<GroupBusVM> FavGroupBusVM
        {
            get
            {
                var vm = new ObservableCollection<GroupBusVM>();
                foreach (var y in Database.FavBusGroups)
                {
                    vm.Add(new GroupBusVM(y.GroupName));
                    foreach (var x in y.Buses)
                        vm.Add(new GroupBusVM(x, y.GroupName));
                }
                return vm;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            //App.m_AppLog.Debug("Screen Width = " + Application.Current.RootVisual.RenderSize.Width);

            lbBus.ItemsSource = FavGroupBusVM;

            if ((string)PhoneApplicationService.Current.State.GetValue("Op", "") == "Add")
            {
                while (NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry();
                PhoneApplicationService.Current.State.Remove("Op");
            }

        }

        private void BusItem_Delete_Click(object sender, RoutedEventArgs e)
        {
            var gbvm = (sender as MenuItem).DataContext as GroupBusVM;
            BusInfo busInfo = gbvm.BusInfo;

            var group = Database.FavBusGroups.FirstOrDefault(g => g.GroupName==gbvm.GroupName );
            if (group == null)
            {
                App.m_AppLog.Error("can not find group which contains busInfo=" + busInfo);
                return;
            }

            group.Buses.Remove(busInfo);
            if (group.Buses.Count == 0)
                Database.FavBusGroups.Remove(group);
            
            lbBus.ItemsSource = FavGroupBusVM;
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


        void UpdateTileJpg()
        {
            Database.SaveFavBusGroups();

            ScheduledTaskAgent1.ScheduledAgent.GenerateTileJpg(
                "\n".Joyn(Database.FavBuses.Select(x => x.Name + " " + x.TimeToArrive)));

            ShellTile tile = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains("DefaultTitle=FromTile"));
            var tileData = new StandardTileData
            {
                Title = DateTime.Now.ToString("HH:mm:ss"),
                BackgroundImage = new Uri("isostore:/" + @"Shared\ShellContent\Tile.jpg", UriKind.Absolute),
            };
            if (tile == null)
                ShellTile.Create(new Uri("/MainPage.xaml?DefaultTitle=FromTile", UriKind.Relative), tileData);
            else
                tile.Update(tileData);

        }

        private void AppBar_Pin_Click(object sender, EventArgs e)
        {
            App.m_AppLog.Debug("");
            UpdateTileJpg();
        }

        private async void AppBar_Refresh_Click(object sender, EventArgs e)
        {
            App.m_AppLog.Debug("enter sender=" + sender.GetType());
            if (Database.FavBuses.Count() == 0)
                return;

            prgbarWaiting.Visibility = Visibility.Visible;
            foreach (var btn in this.ApplicationBar.Buttons)
                (btn as ApplicationBarIconButton).IsEnabled = false;
            this.ApplicationBar.IsMenuEnabled = false;

            bool bIsNetworkOK = await ScheduledAgent.RefreshBusTime(Database.FavBuses);
            App.m_AppLog.Debug("bIsNetworkOK=" + bIsNetworkOK);

            foreach (var btn in this.ApplicationBar.Buttons)
                (btn as ApplicationBarIconButton).IsEnabled = true;
            this.ApplicationBar.IsMenuEnabled = true;
            prgbarWaiting.Visibility = Visibility.Collapsed;
            if (!bIsNetworkOK)
                MessageBox.Show(AppResources.NetworkFault);
            else
            {
                lbBus.ItemsSource = FavGroupBusVM;
                ShellTile tile = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains("DefaultTitle=FromTile"));
                if(tile!=null)
                    UpdateTileJpg();
            }

            App.m_AppLog.Debug("exit");
        }

        private void AppBar_AddBus_Click(object sender, EventArgs e)
        {
            App.m_AppLog.Debug("");
            NavigationService.Navigate(new Uri("/AddBus.xaml", UriKind.Relative));
        }

        private void AppBar_Settings_Click(object sender, EventArgs e)
        {
            App.m_AppLog.Debug("");
            NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));
        }

        private void AppBar_About_Click(object sender, EventArgs e)
        {
            App.m_AppLog.Debug("");
            NavigationService.Navigate(new Uri("/About.xaml", UriKind.Relative));
        }

        private void ListNameMenuItem_Rename_Click(object sender, RoutedEventArgs e)
        {
            App.m_AppLog.Debug("");
        }

        private void lbBus_DoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.m_AppLog.Debug("");
            var gbvm = (e.OriginalSource as FrameworkElement).DataContext as GroupBusVM;
            BusInfo busInfo = gbvm.BusInfo;
            if (busInfo == null)
            {
                NavigationService.Navigate(new Uri("/ChangeGroupName.xaml?groupName=" + gbvm.GroupName, UriKind.Relative));
                return;
            }
            
            GotoDetailsPage(busInfo, gbvm.GroupName);
        }



    }

    public class GroupBusVM : INotifyPropertyChanged
    {
        public GroupBusVM() { }
        public GroupBusVM(string groupName) { GroupName = groupName; }
        public GroupBusVM(BusInfo b, string groupName) { m_BusInfo = b; GroupName = groupName; }

        public string GroupName{get;set;}
        public string BusName { get { return m_BusInfo.Name; } set { if (m_BusInfo.Name != value) { m_BusInfo.Name = value; NotifyPropertyChanged("BusName"); } } }
        public string Station { get { return m_BusInfo.Station; } set { if (m_BusInfo.Station != value) { m_BusInfo.Station = value; NotifyPropertyChanged("Station"); } } }

        BusInfo m_BusInfo;
        public BusInfo BusInfo { get { return m_BusInfo; } }
        public string TimeToArrive { 
            get { return m_BusInfo.TimeToArrive; } 
            set { if (m_BusInfo.TimeToArrive != value) { m_BusInfo.TimeToArrive = value; NotifyPropertyChanged("TimeToArrive"); } } 
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
            Add(new GroupBusVM(new BusInfo { Name = "橘2", Station = "秀山國小", TimeToArrive = "無資料" }, "上班"));
            Add(new GroupBusVM(new BusInfo { Name = "敦化幹線", Station = "秀景里", TimeToArrive = "無資料" }, "上班"));

            Add(new GroupBusVM ("回家") );
            Add(new GroupBusVM(new BusInfo{Name = "橘2", Station = "捷運永安市場站", TimeToArrive = "無資料"}, "回家"));
            Add(new GroupBusVM(new BusInfo{Name = "275", Station = "忠孝敦化路口", TimeToArrive = "無資料" }, "回家"));
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
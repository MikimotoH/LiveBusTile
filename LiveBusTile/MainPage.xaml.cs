using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media;
using ScheduledTaskAgent1;
using System.Threading;
using System.Threading.Tasks;
using LiveBusTile.Resources;
using System.Collections.ObjectModel;

namespace LiveBusTile
{
    public partial class MainPage : PhoneApplicationPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            lbBusGroups.ItemsSource = Database.FavBusGroups;
            if((string)PhoneApplicationService.Current.State.GetValue("Op", "")== "Add")
            {
                while (NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry();
                PhoneApplicationService.Current.State.Remove("Op");
            }
        }



        private void BusItem_Delete_Click(object sender, RoutedEventArgs e)
        {
            BusInfo busInfo = (sender as MenuItem).DataContext as BusInfo;
            // TODO 
            // improve search speed

            var group = Database.FavBusGroups.FirstOrDefault( g => g.Buses.Contains(busInfo));
            if (group == null)
            {
                App.m_AppLog.Error("can not find group which contains busInfo="+busInfo);
                return;
            }
                
            group.Buses.Remove(busInfo);
            if(group.Buses.Count==0)
                Database.FavBusGroups.Remove(group);


            //foreach (var y in Database.FavBusGroups)
            //{
            //    foreach (var x in y.Buses )
            //    {
            //        if (x == busInfo)
            //        {
            //            y.Buses.Remove(x);
            //            if (y.Buses.Count == 0)
            //            {
            //                Database.FavBusGroups.Remove(y);
            //            }
            //            lbBusGroups.ItemsSource = Database.FavBusGroups.ToObservableCollection();
            //        }
            //    }
            //}

        }
        
        private void BusItem_Details_Click(object sender, RoutedEventArgs e)
        {
            GotoDetailsPage((sender as MenuItem).DataContext as BusInfo);
        }

        void GotoDetailsPage(BusInfo busInfo)
        {
            PhoneApplicationService.Current.State["busInfo"] = busInfo;
            var bg = Database.FavBusGroups.FirstOrDefault(x => x.Buses.Contains(busInfo));
            PhoneApplicationService.Current.State["busInfo.Group"]  = bg.GroupName;
            
            NavigationService.Navigate(new Uri(
                "/BusStationDetails.xaml", UriKind.Relative));
        }
               
        private void BusItem_Refresh_Click(object sender, RoutedEventArgs e)
        {
            AppBar_Refresh_Click(sender,  e);
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
            App.m_AppLog.Debug("enter sender="+sender.GetType());
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
                UpdateTileJpg();
            //lbBusGroups.ItemsSource = Database.FavBusGroups.ToObservableCollection();
                        
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

        private void lbBuses_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {

        }

        private void lbBuses_DoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ListBox lb = sender as ListBox;
            if (lb.SelectedItem == null)
                return;
            ListBoxItem lbi = lb.ItemContainerGenerator.ContainerFromItem(lb.SelectedItem) as ListBoxItem;
            App.m_AppLog.Debug("lbi.Content={0}".Fmt((lbi.Content as BusInfo)));
            BusInfo busInfo = lbi.Content as BusInfo;
            GotoDetailsPage(busInfo);
        }

        private void lbBuses_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            App.m_AppLog.Debug("MouseEventArgs={{ OriginalSource={0}, StylusDevice={{ DeviceType={1}, Inverted={2} }}, GetPosition(lbBusGroups)={3} }}"
                .Fmt(e.OriginalSource.GetType(), e.StylusDevice.DeviceType, e.StylusDevice.Inverted, e.GetPosition(lbBusGroups).ToString()));
        }


    }

    public class ExampleBusGroups : ObservableCollection<BusGroup>
    {
        public ExampleBusGroups()
        {
            Add(new BusGroup
            {
                GroupName = "上班",
                Buses = new ObservableCollection<BusInfo>
                {
                    new BusInfo{Name="橘2", Dir=BusDir.go, Station="秀山國小", TimeToArrive="無資料"},
                    new BusInfo{Name="敦化幹線", Dir=BusDir.back, Station="秀景里", TimeToArrive="無資料"},
                }
            });

            Add(new BusGroup
            {
                GroupName = "回家",
                Buses = new ObservableCollection<BusInfo>
                { 
                    new BusInfo{Name="橘2", Dir=BusDir.back, Station="捷運永安市場站", TimeToArrive="無資料"} ,
                    new BusInfo{Name="275", Dir=BusDir.back, Station="忠孝敦化路口", TimeToArrive="無資料"} ,
                }
            });
        }
    }

}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using ScheduledTaskAgent1;
using LiveBusTile.Resources;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO.IsolatedStorage;

namespace LiveBusTile
{
    public partial class GroupPage : PhoneApplicationPage
    {
        public GroupPage()
        {
            InitializeComponent();
        }
        BusGroup m_BusGroup;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            string groupName = NavigationContext.QueryString.GetValue("GroupName", "");
            m_BusGroup = Database.FavBusGroups.FirstOrDefault(x => x.m_GroupName == groupName);

            if (m_BusGroup == null)
            {
                if (PhoneApplicationService.Current.State.GetValue("Op", "") as string != ""){
                    if (NavigationService.CanGoBack)
                        NavigationService.GoBack();
                    else
                        NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
                    return;
                }
                else
                {
                    MessageBox.Show("不存在的群組「{0}」".Fmt(groupName));
                    App.m_AppLog.Error("Uri=\"{0}\" doesn't contain \"GroupName\".".Fmt(NavigationContext.ToString()));
                    App.Current.Terminate();
                }
            }

            tbLastUpdatedTime.Text = AppResources.LastUpdatedTime + " " +
                ((DateTime)IsolatedStorageSettings.ApplicationSettings.GetValue("LastUpdatedTime", DateTime.MinValue)).ToString("HH:mm:ss");

            tbGroupName.Text = m_BusGroup.m_GroupName;

            lbBusInfos.ItemsSource = m_BusGroup.m_Buses.Select(x => new BusInfoVM(x)).ToList();

            if ((string)PhoneApplicationService.Current.State.GetValue("Op", "") == "Add")
            {
                int backStackCount = (int)PhoneApplicationService.Current.State["HomeUri.BackStack.Count"];
                App.m_AppLog.Debug("NavigationService.BackStack=" + 
                    NavigationService.BackStack.DumpArray(x=>x.Source.ToString()));
                while (true){
                    var jo = NavigationService.RemoveBackEntry();
                    if (NavigationService.BackStack.Count() == backStackCount)
                        break;
                }
                PhoneApplicationService.Current.State.Remove("Op");
            }
        }


        private void AppBar_Pin_Click(object sender, EventArgs e)
        {
            App.m_AppLog.Debug("");
            Database.SaveFavBusGroups();
            CreateTile(m_BusGroup.m_GroupName);
        }

        private void AppBar_AddBus_Click(object sender, EventArgs e)
        {
            App.m_AppLog.Debug("");
            PhoneApplicationService.Current.State["HomeUri.BackStack.Count"] = NavigationService.BackStack.Count();

            NavigationService.Navigate(new Uri("/AddBus.xaml?GroupName="+m_BusGroup.m_GroupName, UriKind.Relative));
        }

        private async void AppBar_Refresh_Click(object sender, EventArgs e)
        {
            App.m_AppLog.Debug("enter sender=" + sender.GetType());
            if (Database.FavBuses.Count() == 0)
                return;

            prgbarWaiting.Visibility = Visibility.Visible;
            this.ApplicationBar.Buttons.DoForEach<ApplicationBarIconButton>(x => x.IsEnabled = false);
            this.ApplicationBar.IsMenuEnabled = false;

            
            Task<string>[] tasks = m_BusGroup.m_Buses.Select(b => BusTicker.GetBusDueTime(b)).ToArray();
            try
            {
                await Task.Run(() =>
                {
                    Task.WaitAll(tasks);
                });
            }
            catch (Exception ex)
            {
                App.m_AppLog.Error("Task.WaitAll(tasks) failed");
                App.m_AppLog.Error(ex.DumpStr());
            }

            int numSucceededTasks = 0;
            for (int i = 0; i < tasks.Length; ++i)
            {
                if (tasks[i].Status == TaskStatus.RanToCompletion)
                {
                    m_BusGroup.m_Buses[i].m_TimeToArrive = tasks[i].Result;
                    ++numSucceededTasks;
                }
            }
            App.m_AppLog.Debug("numSucceededTasks=" + numSucceededTasks);

            if (numSucceededTasks > 0)
            {
                Database.SaveFavBusGroups();
                IsolatedStorageSettings.ApplicationSettings["LastUpdatedTime"] = DateTime.Now;

                lbBusInfos.ItemsSource = m_BusGroup.m_Buses.Select(x => new BusInfoVM(x)).ToList();
                TileUtil.UpdateTile(m_BusGroup.m_GroupName);
                TileUtil.UpdateTile("");

                tbLastUpdatedTime.Text = AppResources.LastUpdatedTime + " " +
                    ((DateTime)IsolatedStorageSettings.ApplicationSettings["LastUpdatedTime"]).ToString("HH:mm:ss");
            }

            this.ApplicationBar.Buttons.DoForEach<ApplicationBarIconButton>(x => x.IsEnabled = true);
            this.ApplicationBar.IsMenuEnabled = true;
            prgbarWaiting.Visibility = Visibility.Collapsed;

            if (numSucceededTasks == 0)
                MessageBox.Show(AppResources.NetworkFault);

            App.m_AppLog.Debug("exit");
        }

        private void tbGroupName_DoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            tbGroupNameTextBox.Text = m_BusGroup.m_GroupName;
            tbGroupName.Visibility = Visibility.Collapsed;
            tbGroupNameTextBox.Visibility = Visibility.Visible;

            tbGroupNameTextBox.Focus();
            tbGroupNameTextBox.Select(tbGroupNameTextBox.Text.Length, 0);
            tbGroupNameTextBox.LostFocus += tbGroupNameTextBox_LostFocus;
        }

        public static void CreateTile(string groupName)
        {
            string uri = TileUtil.TileUri(groupName);
            var tile = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString() == uri);
            if (tile == null)
            {
                ShellTile.Create(new Uri(uri, UriKind.Relative),
                    new FlipTileData
                    {
                        Title = DateTime.Now.ToString("HH:mm:ss"),
                        BackgroundImage = TileUtil.GenerateTileJpg(groupName, false),
                        WideBackgroundImage = TileUtil.GenerateTileJpg(groupName, true),
                    },
                    true);
            }
            else
            {
                tile.Update(
                    new FlipTileData
                    {
                        Title = DateTime.Now.ToString("HH:mm:ss"),
                        BackgroundImage = TileUtil.GenerateTileJpg(groupName, false),
                        WideBackgroundImage = TileUtil.GenerateTileJpg(groupName, true),
                    });
            }
        }

        static void RenameTile(string oldGroupName, string newGroupName)
        {
            App.m_AppLog.Debug("oldGroupName=\"{0}\", newGroupName=\"{1}\"".Fmt(oldGroupName, newGroupName));

            ShellTile tile = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString() == TileUtil.TileUri(oldGroupName));
            if (tile == null)
            {
                App.m_AppLog.Error("Can't find TileUri=\"{0}\", ShellTile.ActiveTiles={1}".Fmt(TileUtil.TileUri(oldGroupName), 
                    ShellTile.ActiveTiles.DumpArray(x => x.NavigationUri.ToString() )));
                return;
            }

            Util.DeleteFileSafely(TileUtil.TileJpgPath(oldGroupName, false));
            Util.DeleteFileSafely(TileUtil.TileJpgPath(oldGroupName, true));           
            tile.Delete();

            ShellTile.Create(new Uri(TileUtil.TileUri(newGroupName), UriKind.Relative),
                new FlipTileData { 
                    Title = DateTime.Now.ToString("HH:mm:ss"),
                    BackgroundImage     = TileUtil.GenerateTileJpg(newGroupName, false),
                    WideBackgroundImage = TileUtil.GenerateTileJpg(newGroupName, true),//new Uri("isostore:/" + TileUtil.TileJpgPath(newGroupName, true), UriKind.Absolute),
                }, true);
        }

        void tbGroupNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            string newGroupName = tbGroupNameTextBox.Text.Trim();
            if (!Database.IsLegalGroupName(newGroupName))
            {
                MessageBox.Show(AppResources.IllegalGroupName.Fmt(tbGroupNameTextBox.Text));
            }
            else if(Database.FavBusGroups.FirstOrDefault(x=>x.m_GroupName == newGroupName) != null)
            {
                MessageBox.Show("群組名稱「{0}」已存在".Fmt(tbGroupNameTextBox.Text));
            }
            else
            {
                if (m_BusGroup.m_GroupName != newGroupName)
                {
                    string oldGroupName = m_BusGroup.m_GroupName;
                    m_BusGroup.m_GroupName = newGroupName;
                    Database.SaveFavBusGroups();

                    Deployment.Current.Dispatcher.BeginInvoke(() => 
                    {
                        RenameTile(oldGroupName, newGroupName);
                    });
                }
                tbGroupName.Text = newGroupName;
            }

            tbGroupName.Visibility = Visibility.Visible;
            tbGroupNameTextBox.Visibility = Visibility.Collapsed;
            tbGroupNameTextBox.LostFocus -= tbGroupNameTextBox_LostFocus;
        }

        private void btnGoToMainPage_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!NavigationService.CanGoBack)
                NavigationService.Navigate(new Uri("/MainPage.xaml?From=GroupPage.xaml", UriKind.Relative));
            else
                NavigationService.GoBack();
        }

        private void lbBusInfos_DoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.m_AppLog.Debug("");
            BusInfoVM busInfo = (e.OriginalSource as FrameworkElement).DataContext as BusInfoVM;
            GotoDetailsPage(busInfo.Base, m_BusGroup.m_GroupName);
        }

        void GotoDetailsPage(BusInfo busInfo, string groupName)
        {
            PhoneApplicationService.Current.State["busInfo"] = busInfo;
            PhoneApplicationService.Current.State["groupName"] = groupName;
            NavigationService.Navigate(new Uri(
                "/BusStationDetails.xaml", UriKind.Relative));
        }

    }
}
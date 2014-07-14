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
                    {
                        PhoneApplicationService.Current.State["Op"] = "Deleted";
                        NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
                    }
                    return;
                }
                else
                {
                    MessageBox.Show("不存在的群組「{0}」".Fmt(groupName));
                    AppLogger.Error("Uri=\"{0}\" doesn't contain \"GroupName\".".Fmt(NavigationContext.ToString()));
                    App.Current.Terminate();
                }
            }

            tbLastUpdatedTime.Text = Database.LastUpdatedTime.ToString(TileUtil.CurSysTimeFormat);

            tbGroupName.Text = m_BusGroup.m_GroupName;

            lbBusInfos.ItemsSource = m_BusGroup.m_Buses.Select(x => new BusInfoVM(x)).ToList();

            if ((string)PhoneApplicationService.Current.State.GetValue("Op", "") == "Add")
            {
                int backStackCount = (int)PhoneApplicationService.Current.State["HomeUri.BackStack.Count"];
                AppLogger.Debug("NavigationService.BackStack=" + 
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
            AppLogger.Debug("");
            Database.SaveFavBusGroups();
            CreateTile(m_BusGroup.m_GroupName);
        }

        private void AppBar_AddBus_Click(object sender, EventArgs e)
        {
            AppLogger.Debug("");
            PhoneApplicationService.Current.State["HomeUri.BackStack.Count"] = NavigationService.BackStack.Count();

            NavigationService.Navigate(new Uri("/AddBus.xaml?GroupName="+m_BusGroup.m_GroupName, UriKind.Relative));
        }
        private void AppBar_AddStation_Click(object sender, EventArgs e)
        {
            AppLogger.Debug("");
            PhoneApplicationService.Current.State["HomeUri.BackStack.Count"] = NavigationService.BackStack.Count();

            NavigationService.Navigate(new Uri("/AddStation.xaml?GroupName=" + m_BusGroup.m_GroupName, UriKind.Relative));
        }

        private async void AppBar_Refresh_Click(object sender, EventArgs e)
        {
            AppLogger.Debug("enter sender=" + sender.GetType());
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
                AppLogger.Error("Task.WaitAll(tasks) failed");
                AppLogger.Error(ex.DumpStr());
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
            AppLogger.Debug("m_finHttpReqs=" + numSucceededTasks);

            if (numSucceededTasks > 0)
            {
                Database.SaveFavBusGroups();

                lbBusInfos.ItemsSource = m_BusGroup.m_Buses.Select(x => new BusInfoVM(x)).ToList();

                tbLastUpdatedTime.Text = Database.LastUpdatedTime.ToString(TileUtil.CurSysTimeFormat);

                TileUtil.UpdateTile2(m_BusGroup.m_GroupName);
                TileUtil.UpdateTile2("");
            }

            this.ApplicationBar.Buttons.DoForEach<ApplicationBarIconButton>(x => x.IsEnabled = true);
            this.ApplicationBar.IsMenuEnabled = true;
            prgbarWaiting.Visibility = Visibility.Collapsed;

            if (numSucceededTasks == 0)
                MessageBox.Show(AppResources.NetworkFault);

            AppLogger.Debug("exit");
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
                        BackgroundImage = TileUtil.GenerateTileJpg2(groupName, false),
                        WideBackgroundImage = TileUtil.GenerateTileJpg2(groupName, true),
                    },
                    true);
            }
            else
            {
                tile.Update(
                    new FlipTileData
                    {
                        BackgroundImage = TileUtil.GenerateTileJpg2(groupName, false),
                        WideBackgroundImage = TileUtil.GenerateTileJpg2(groupName, true),
                    });
            }
        }

        static void RenameTile(string oldGroupName, string newGroupName)
        {
            AppLogger.Debug("oldGroupName=\"{0}\", newGroupName=\"{1}\"".Fmt(oldGroupName, newGroupName));

            ShellTile tile = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString() == TileUtil.TileUri(oldGroupName));
            if (tile != null)
            {
                //AppLogger.Error("Can't find TileUri=\"{0}\", ShellTile.ActiveTiles={1}".Fmt(TileUtil.TileUri(oldGroupName), 
                //    ShellTile.ActiveTiles.DumpArray(x => x.NavigationUri.ToString() )));
                Util.DeleteFileSafely(TileUtil.TileJpgPath(oldGroupName, false));
                Util.DeleteFileSafely(TileUtil.TileJpgPath(oldGroupName, true));
                tile.Delete();
            }

            try
            {
                ShellTile.Create(new Uri(TileUtil.TileUri(newGroupName), UriKind.Relative),
                    new FlipTileData
                    {
                        BackgroundImage = TileUtil.GenerateTileJpg2(newGroupName, false),
                        WideBackgroundImage = TileUtil.GenerateTileJpg2(newGroupName, true),
                    }, true);
            }
            catch (Exception ex)
            {
                AppLogger.Error("ShellTile.Create(new FlipTileData{}) failed, ex=" + ex.DumpStr());
            }
        }

        void LostFocus_Epilog()
        {
            tbGroupName.Visibility = Visibility.Visible;
            tbGroupNameTextBox.Visibility = Visibility.Collapsed;
            tbGroupNameTextBox.LostFocus -= tbGroupNameTextBox_LostFocus;
        }

        void tbGroupNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            string newGroupName = tbGroupNameTextBox.Text.Trim();
            if (newGroupName == this.m_BusGroup.m_GroupName)
                return;
            BusGroup existingGroup;
            if (!Database.IsLegalGroupName(newGroupName))
            {
                MessageBox.Show(AppResources.IllegalGroupName.Fmt(tbGroupNameTextBox.Text));
            }
            else if ((existingGroup = Database.FavBusGroups.FirstOrDefault(x => x.m_GroupName == newGroupName)) != null)
            {
                var answer = MessageBox.Show(AppResources.ConfirmMergeExistingGroup.Fmt(newGroupName), AppResources.ApplicationTitle,  MessageBoxButton.OKCancel);
                if (answer == MessageBoxResult.OK)
                {
                    //Merge Group
                    string oldGroupName = m_BusGroup.m_GroupName;
                    existingGroup.m_Buses.AddRange(m_BusGroup.m_Buses);
                    Database.FavBusGroups.Remove(m_BusGroup);
                    m_BusGroup = existingGroup;
                    tbGroupName.Text = m_BusGroup.m_GroupName;
                    lbBusInfos.ItemsSource = m_BusGroup.m_Buses.Select(x => new BusInfoVM(x)).ToList();

                    Database.SaveFavBusGroups();
                    LostFocus_Epilog();
                    RenameTile(oldGroupName, newGroupName);
                }

            }
            else
            {
                if (m_BusGroup.m_GroupName != newGroupName)
                {
                    string oldGroupName = m_BusGroup.m_GroupName;
                    m_BusGroup.m_GroupName = newGroupName;
                    tbGroupName.Text = newGroupName;

                    Database.SaveFavBusGroups();
                    LostFocus_Epilog();
                    RenameTile(oldGroupName, newGroupName);
                }
            }
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
            AppLogger.Debug("");
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
        private void AppBar_DeleteGroup_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("確定要刪除整個群組？", "", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                return;

            bool bRemoveOK = Database.FavBusGroups.Remove(m_BusGroup);
            if (!bRemoveOK)
                AppLogger.Error("Database.FavBusGroups.Remove(m_BusGroup) failed");
            
            Database.SaveFavBusGroups();
            ShellTile tile = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString() == TileUtil.TileUri(m_BusGroup.m_GroupName));
            if (tile != null)
                tile.Delete();
            TileUtil.UpdateTile2("");

            PhoneApplicationService.Current.State["Op"] = "Deleted";
            NavigationService.GoBack();
        }

    }
}
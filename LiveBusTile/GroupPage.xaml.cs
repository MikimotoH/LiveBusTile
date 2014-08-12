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
using System.Threading;

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

        class WCVM
        {
            public WebClient wc = new WebClient();
            public BusInfoVM vm;
            public WCVM(BusInfoVM vm)
            {
                this.vm = vm;
                this.wc.Headers = new WebHeaderCollection();
                this.wc.Headers[HttpRequestHeader.IfModifiedSince] = DateTime.UtcNow.ToString("R");
                this.wc.Headers["Cache-Control"] = "no-cache";
            }
        }

        WCVM[] m_WCVMs;

        private void AppBar_Refresh_Click(object sender, EventArgs e)
        {
            if (m_BusGroup.m_Buses.Count==0)
                return;
            if (prgbarWaiting.Visibility == Visibility.Visible)
            {
                m_WCVMs.DoForEach(wcvm => wcvm.wc.CancelAsync());
                return;
            }

            prgbarWaiting.Visibility = Visibility.Visible;
            ApplicationBar.Buttons.DoForEach<ApplicationBarIconButton>(x => x.IsEnabled = false);
            ApplicationBarIconButton btnRefresh = sender as ApplicationBarIconButton;
            btnRefresh.IsEnabled = true;
            btnRefresh.IconUri = new Uri("/Images/AppBar.StopRefresh.png", UriKind.Relative);

            m_WCVMs = lbBusInfos.Items.Cast<BusInfoVM>().Select(b => new WCVM(b)).ToArray();
            int m_CompletedWCs = 0;
            int m_CancelledWCs = 0;
            int m_SucceededWCs = 0;

            foreach (var wcvm in m_WCVMs)
            {
                wcvm.wc.DownloadStringCompleted += (s, asyncCompletedEventArgs) =>
                {
                    int completedWCs = Interlocked.Increment(ref m_CompletedWCs);
                    int succeededWCs = Interlocked.Add(ref m_SucceededWCs, asyncCompletedEventArgs.Error == null ? 1 : 0);
                    int cancelledWCs = Interlocked.Add(ref m_CancelledWCs, asyncCompletedEventArgs.Cancelled ? 1 : 0);
                    AppLogger.Debug("completedWCs={0}, succeededWCs={1}, cancelledWCs={2}"
                        .Fmt(completedWCs, succeededWCs, cancelledWCs));

                    if (asyncCompletedEventArgs.Error == null)
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            BusInfoVM vm = asyncCompletedEventArgs.UserState as BusInfoVM;
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
                            if (succeededWCs > 0)
                            {
                                Database.SaveFavBusGroups();                                
                                TileUtil.UpdateTile2(this.m_BusGroup.m_GroupName);
                                TileUtil.UpdateTile2("");
                            }
                            else if (cancelledWCs == 0)
                                MessageBox.Show(AppResources.NetworkFault);
                            

                            btnRefresh.IconUri = new Uri("/Images/AppBar.Refresh.png", UriKind.Relative);
                            ApplicationBar.Buttons.Cast<ApplicationBarIconButton>().DoForEach(x => x.IsEnabled = true);
                            prgbarWaiting.Visibility = Visibility.Collapsed;
                        });
                    }
                };
                wcvm.wc.DownloadStringAsync(new Uri(BusTicker.Pda5284Url(wcvm.vm.Base)), wcvm.vm);
            }
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
            if (tile == null)
                return;
            //AppLogger.Error("Can't find TileUri=\"{0}\", ShellTile.ActiveTiles={1}".Fmt(TileUtil.TileUri(oldGroupName), 
            //    ShellTile.ActiveTiles.DumpArray(x => x.NavigationUri.ToString() )));
            tile.Update(new FlipTileData
                {
                    BackgroundImage = TileUtil.GenerateTileJpg2(newGroupName, false),
                    WideBackgroundImage = TileUtil.GenerateTileJpg2(newGroupName, true),
                });
            Util.DeleteFileSafely(TileUtil.TileJpgPath(oldGroupName, false));
            Util.DeleteFileSafely(TileUtil.TileJpgPath(oldGroupName, true));
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
            {
                LostFocus_Epilog();
                return;
            }
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

        void DeleteThisGroup()
        {
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
        private void AppBar_DeleteGroup_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("確定要刪除整個群組？", "", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                return;
            DeleteThisGroup();
        }

        private void BusItem_Delete_Click(object sender, RoutedEventArgs e)
        {
            var bivm = (sender as MenuItem).DataContext as BusInfoVM;
            bool bRemoveOK = m_BusGroup.m_Buses.Remove(bivm.Base);
            if(!bRemoveOK)
                AppLogger.Error("m_BusGroup.m_Buses.Remove(bivm.Base={0}) failed".Fmt(bivm.Base));
            
            if (m_BusGroup.m_Buses.Count == 0)
            {
                DeleteThisGroup();
                return;
            }

            Database.SaveFavBusGroups();

            lbBusInfos.ItemsSource = m_BusGroup.m_Buses.Select(x => new BusInfoVM(x)).ToList();
            TileUtil.UpdateTile2(m_BusGroup.m_GroupName);
        }

        private void BusItem_Details_Click(object sender, RoutedEventArgs e)
        {
            var bivm = (sender as MenuItem).DataContext as BusInfoVM;
            GotoDetailsPage(bivm.Base, m_BusGroup.m_GroupName);
        }

    }
}
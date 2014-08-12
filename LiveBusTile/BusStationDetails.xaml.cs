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
using System.Diagnostics;
using System.Collections.ObjectModel;
using LiveBusTile.Resources;
using System.IO.IsolatedStorage;

namespace LiveBusTile
{
    public partial class BusStationDetails : PhoneApplicationPage
    {
        public BusStationDetails()
        {
            InitializeComponent();
        }

        BusInfo m_busInfo;
        string m_GroupName;
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            m_busInfo = PhoneApplicationService.Current.State["busInfo"] as BusInfo;
            tbBusName.Text = m_busInfo.m_Name;
            tbStation.Text = m_busInfo.m_Station;
            tbTimeToArrive.Text = m_busInfo.m_TimeToArrive;

            tbDir.Text = (m_busInfo.m_Dir == BusDir.go ? "往" : "返");
            m_GroupName = PhoneApplicationService.Current.State["groupName"] as string;
            tbGroup.Text = m_GroupName;

            tbLastUpdatedTime.Text = Database.LastUpdatedTime.ToString(TileUtil.CurSysTimeFormatWithSecs);
            
            var stpair = Database.AllBuses[tbBusName.Text];
            if (m_busInfo.m_Dir == BusDir.go && stpair.stations_go.Length > 0)
                tbDir.Text += stpair.stations_go.LastElement();
            else if (m_busInfo.m_Dir == BusDir.back && stpair.stations_back.Length > 0)
                tbDir.Text += stpair.stations_back.LastElement();
        }


        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            AppLogger.Debug("e.Cancel=" + e.Cancel);
            if(!Database.IsLegalGroupName(tbGroup.Text))
            {
                MessageBox.Show(AppResources.IllegalGroupName.Fmt(tbGroup.Text));
                tbGroup.Focus();
                //tbGroup.SelectAll();
                e.Cancel = false;
                return;
            }

            if (tbGroup.Text != m_GroupName)
            {
                AppLogger.Debug("m_GroupName={0}, tbGroup.Text={1}".Fmt(m_GroupName, tbGroup.Text));
                BusGroup old_group = Database.FavBusGroups.FirstOrDefault(x => x.m_GroupName == m_GroupName);
                BusGroup new_group = Database.FavBusGroups.FirstOrDefault(x => x.m_GroupName == tbGroup.Text);
                Debug.Assert(old_group != null);
                if (new_group == null)
                    Database.FavBusGroups.Add(new BusGroup{ m_GroupName = tbGroup.Text, m_Buses = new List<BusInfo> { m_busInfo } });
                else
                    new_group.m_Buses.Add(m_busInfo);

                bool bRemoveSuccess = old_group.m_Buses.Remove(m_busInfo);
                AppLogger.Debug("bRemoveSuccess=" + bRemoveSuccess);
                if (old_group.m_Buses.Count == 0)
                    Database.FavBusGroups.Remove(old_group);

                Database.SaveFavBusGroups();
            }


            base.OnBackKeyPress(e);
        }

        private async void AppBar_RefreshBusTime_Click(object sender, EventArgs e)
        {
            progbar.Visibility = Visibility.Visible;
            ApplicationBar.Buttons.Cast<ApplicationBarIconButton>().DoForEach(x =>x.IsEnabled=false);
            ApplicationBar.IsMenuEnabled = false;

            Action RefreshEpilog = () =>
            {
                ApplicationBar.Buttons.Cast<ApplicationBarIconButton>().DoForEach(x => x.IsEnabled = true);
                ApplicationBar.IsMenuEnabled = true;
                progbar.Visibility = Visibility.Collapsed;
            };


            string timeToArrive="";
            try
            {
                timeToArrive = await BusTicker.GetBusDueTime(m_busInfo);
            }
            catch(Exception ex)
            {
                AppLogger.Error("ex="+ex.DumpStr());
                MessageBox.Show(AppResources.NetworkFault );
                RefreshEpilog();
                return;
            }

            tbTimeToArrive.Text = timeToArrive;
            Database.FavBuses.FirstOrDefault(x => x == m_busInfo).m_TimeToArrive = timeToArrive; ;
            Database.SaveFavBusGroups();
            tbLastUpdatedTime.Text = Database.LastUpdatedTime.ToString(TileUtil.CurSysTimeFormatWithSecs);
            RefreshEpilog();
        }


                    
        private void AppBar_Delete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("確定要刪除此公車？", "", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                return;

            try
            {
                BusGroup bg = Database.FavBusGroups.FirstOrDefault(x
                    => x.m_GroupName == m_GroupName);

                bool bRemoveSuccess = bg.m_Buses.Remove(m_busInfo);
                if (!bRemoveSuccess) { MessageBox.Show("bg.Remove({0}) failed".Fmt(m_busInfo)); return; }
                if (bg.m_Buses.Count == 0)
                {
                    ShellTile tile = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString() == TileUtil.TileUri(m_GroupName));
                    if(tile != null)
                        tile.Delete();
                    else
                        AppLogger.Error("Failed to find Tile.NavigationUri==\"{0}\" ".Fmt(TileUtil.TileUri(m_GroupName)));
                    
                    Util.DeleteFileSafely(TileUtil.TileJpgPath(m_GroupName, false));
                    Util.DeleteFileSafely(TileUtil.TileJpgPath(m_GroupName, true));
                    
                    bRemoveSuccess = Database.FavBusGroups.Remove(bg);
                    if (!bRemoveSuccess) 
                        AppLogger.Error("Database.FavBusGroups.Remove({0}) failed".Fmt(bg.m_GroupName)); 
                }
                AppLogger.Debug("bRemoveSuccess=" + bRemoveSuccess);
                Database.SaveFavBusGroups();
                PhoneApplicationService.Current.State["Op"] = "Deleted";
                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                AppLogger.Error("m_busInfo={0}, m_GroupName={1} cannot be found!".Fmt(m_busInfo, m_GroupName));
                AppLogger.Error("Database.FavBusGroups=" + Database.FavBusGroups.DumpArray());
                AppLogger.Error("ex="+ex.DumpStr());
            }
        }

        private void GotoContextBusTime_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri(
                "/ContextBusTime.xaml?BusName={0}&Dir={1}&Station={2}"
                .Fmt(m_busInfo.m_Name, m_busInfo.m_Dir, m_busInfo.m_Station), 
                UriKind.Relative));
        }

        private void AppBar_Map_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/StationMap.xaml?Station="+m_busInfo.m_Station,UriKind.Relative));
        }
    }


    public class FavGroupNames : IEnumerable<string>
    {
        public static ICollection<string> Words
        {
            get
            {
                var groups = new HashSet<string>(Database.FavBusGroups.Select(x => x.m_GroupName));
                groups.Add("上班");
                groups.Add("回家");
                
                return (ICollection<string>)(groups.ToList());
            }
        }

        public IEnumerator<string> GetEnumerator()
        {
            return Words.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Words.GetEnumerator();
        }
    }
}
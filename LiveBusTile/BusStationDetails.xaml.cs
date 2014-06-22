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

namespace LiveBusTile
{
    public partial class BusStationDetails : PhoneApplicationPage
    {
        public BusStationDetails()
        {
            InitializeComponent();
        }

        BusInfo m_busInfo;
        string m_orig_group;
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            m_busInfo = PhoneApplicationService.Current.State["busInfo"] as BusInfo;
            tbBusName.Text = m_busInfo.Name;
            tbStation.Text = m_busInfo.Station;
            tbTimeToArrive.Text = m_busInfo.TimeToArrive;

            tbDir.Text = (m_busInfo.Dir == BusDir.go ? "往" : "返");
            m_orig_group = PhoneApplicationService.Current.State["busInfo.Group"] as string;
            tbGroup.Text = m_orig_group;

            
            var stpair = Database.AllBuses[tbBusName.Text];
            if (m_busInfo.Dir == BusDir.go && stpair.stations_go.Length>0)
            {
                tbDir.Text += stpair.stations_go.LastElement();
            }
            else if (m_busInfo.Dir == BusDir.back && stpair.stations_back.Length > 0)
            {
                tbDir.Text += stpair.stations_back.LastElement();
            }
        }


        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            App.m_AppLog.Debug("e.Cancel=" + e.Cancel);
            if(!Database.IsLegalGroupName(tbGroup.Text))
            {
                MessageBox.Show("不合法的群組名稱：「{0}」".Fmt(tbGroup.Text));
                tbGroup.Focus();
                tbGroup.SelectAll();
                return;
                
            }
            if (tbGroup.Text != m_orig_group)
            {
                App.m_AppLog.Debug("m_orig_group={0}, tbGroup.Text={1}".Fmt(m_orig_group, tbGroup.Text));
                BusGroup old_group = Database.FavBusGroups.FirstOrDefault(x => x.GroupName == m_orig_group);
                BusGroup new_group = Database.FavBusGroups.FirstOrDefault(x => x.GroupName == tbGroup.Text);
                Debug.Assert(old_group != null);
                if (new_group == null)
                    Database.FavBusGroups.Add(new BusGroup { GroupName = tbGroup.Text, Buses = new ObservableCollection<BusInfo> { m_busInfo } });
                else
                    new_group.Buses.Add(m_busInfo);

                old_group.Buses.Remove(m_busInfo);
                if (old_group.Buses.Count == 0)
                    Database.FavBusGroups.Remove(old_group);
                
            }
            base.OnBackKeyPress(e);
        }

        private async void AppBar_RefreshBusTime_Click(object sender, EventArgs e)
        {
            try
            {
                string timeToArrive = await BusTicker.GetBusDueTime(m_busInfo);
                tbTimeToArrive.Text = timeToArrive;
            }
            catch(Exception ex)
            {
                App.m_AppLog.Error("ex="+ex.DumpStr());
                MessageBox.Show(AppResources.NetworkFault );
            }
        }

        private void AppBar_Delete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("確定要刪除此公車？", "", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                return;

            try
            {
                BusGroup bg = Database.FavBusGroups.FirstOrDefault(x
                    => x.GroupName == m_orig_group);

                bool bRemoveSuccess = bg.Buses.Remove(m_busInfo);
                if (!bRemoveSuccess) { MessageBox.Show("bg.Remove({0}) failed".Fmt(m_busInfo)); return; }
                if (bg.Buses.Count == 0)
                {
                    bRemoveSuccess = Database.FavBusGroups.Remove(bg);
                    if (!bRemoveSuccess) { MessageBox.Show("Database.FavBusGroups.Remove({0}) failed".Fmt(bg.GroupName)); return; }
                }
                App.m_AppLog.Debug("bRemoveSuccess=" + bRemoveSuccess);
                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                App.m_AppLog.Error("m_busInfo={0}, m_orig_group={1} cannot be found!".Fmt(m_busInfo, m_orig_group));
                App.m_AppLog.Error("Database.FavBusGroups=" + Database.FavBusGroups.DumpArray());
                App.m_AppLog.Error("ex="+ex.DumpStr());
            }
        }
    }
}
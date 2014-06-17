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
using LiveBusTile.ViewModels;
using Log = ScheduledTaskAgent1.Logger;

namespace LiveBusTile
{
    public partial class BusStationDetails : PhoneApplicationPage
    {
        public BusStationDetails()
        {
            InitializeComponent();
        }
        string m_orig_tag;
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            tbBusName.Text = NavigationContext.QueryString["busName"];
            tbStation.Text = NavigationContext.QueryString["station"];
            tbDir.Text = NavigationContext.QueryString["dir"]=="go"?"往↓":"返↑";
            tbTag.Text = NavigationContext.QueryString["tag"];
            m_orig_tag = tbTag.Text;
        }
        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            Log.Debug("e.Cancel=" + e.Cancel);
            if (tbTag.Text != m_orig_tag)
            {
                Log.Debug("m_orig_tag={0}, tbTag.Text={1}".Fmt(m_orig_tag, tbTag.Text));
                BusDir dir = (tbDir.Text == "往↓" ? BusDir.go : BusDir.back);
                DataService.BusTags.First(x
                    => x.busName == tbBusName.Text
                    && x.station == tbStation.Text
                    && x.dir == dir
                    && x.tag == m_orig_tag).tag = tbTag.Text;
            }
            base.OnBackKeyPress(e);
            //NavigationService.GoBack();
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            BusDir dir = (tbDir.Text == "往↓" ? BusDir.go : BusDir.back);
            try
            {
                BusTagVM bt = DataService.BusTags.First(x
                    => x.busName == tbBusName.Text
                        && x.station == tbStation.Text
                        && x.dir == dir
                        && x.tag == m_orig_tag);

                bool bRemoveSuccess = DataService.BusTags.Remove(bt);
                Log.Debug("bRemoveSuccess=" + bRemoveSuccess);
                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                Log.Error("{0} {1} {2} {3} cannot be found!".Fmt(tbBusName.Text, tbStation.Text, dir, m_orig_tag));
                Log.Error("BusTags={" + ",".Joyn(DataService.BusTags.Select(x => x.ToString()) ) + "}");
                Log.Error("ex="+ex.DumpStr());
            }
        }
    }
}
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

namespace LiveBusTile
{
    public partial class ChangeGroupName : PhoneApplicationPage
    {
        public ChangeGroupName()
        {
            InitializeComponent();
        }
        string m_orig_groupName;
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            m_orig_groupName = NavigationContext.QueryString["groupName"];
            tbGroupName.Text = m_orig_groupName;

            lbBusInfos.ItemsSource = Database.FavBusGroups.FirstOrDefault(x => x.m_GroupName == m_orig_groupName).m_Buses.Select(x => new BusInfoVM(x)).ToList();
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if(!Database.IsLegalGroupName( tbGroupName.Text ))
            {
                MessageBox.Show(AppResources.IllegalGroupName.Fmt(tbGroupName.Text));
                tbGroupName.Focus();
                tbGroupName.SelectAll();
                e.Cancel=false;
                return;
            }

            var g = Database.FavBusGroups.FirstOrDefault(x => x.m_GroupName == m_orig_groupName);
            g.m_GroupName = tbGroupName.Text;
            Database.SaveFavBusGroups();
            base.OnBackKeyPress(e);
        }
    }


}
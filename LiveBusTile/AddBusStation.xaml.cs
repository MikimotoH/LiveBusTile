using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using LiveBusTile.Services;
using ScheduledTaskAgent1;
using LiveBusTile.ViewModels;
using Log = ScheduledTaskAgent1.Logger;

namespace LiveBusTile
{
    public partial class AddBusStation : PhoneApplicationPage
    {
        public AddBusStation()
        {
            InitializeComponent();
        }

        string m_busName;
        StationPair m_stationPair;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            m_busName = NavigationContext.QueryString["busName"];
            m_stationPair = DataService.AllBuses[m_busName];
            llsStations.ItemsSource = m_stationPair.stations_go.Select(x=>new StringVM(x)).ToList();
            tbTitle.Text += (" " + m_busName);
        }

        private void btnDir_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if( (btnDir.Tag as string) == "g")
            {
                if (m_stationPair.stations_back.Length == 0)
                    return;
                btnDir.Text = "返↑";
                btnDir.Tag = "b";
                llsStations.ItemsSource = m_stationPair.stations_back.Select(x => new StringVM(x)).ToList();
            }
            else if ((btnDir.Tag as string) == "b")
            {
                btnDir.Text = "往↓";
                btnDir.Tag = "g";
                llsStations.ItemsSource = m_stationPair.stations_go.Select(x => new StringVM(x)).ToList();
            }
        }

        private void tbBusName_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
                btnEnter_Tap(null, null);
        }

        private void btnEnter_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.RecusiveBack = true;
            NavigationService.Navigate(new Uri("/MainPage.xaml?Op=Add&busName={0}&station={1}&dir={2}&tag=新增".Fmt(m_busName, tbStation.Text,
                btnDir.Tag as string), UriKind.Relative));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private void llsStations_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Log.Debug("");
            if (llsStations.SelectedItem == null)
                return;
            Log.Debug("llsStations.SelectedItem=" + (llsStations.SelectedItem as StringVM).String);
            tbStation.Text = (llsStations.SelectedItem as StringVM).String;
        }



    }
}
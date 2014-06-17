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
        BusDir m_dir = BusDir.go;
        string[] CurStations()
        {
            return m_dir == BusDir.go ? m_stationPair.stations_go : m_stationPair.stations_back;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            m_busName = NavigationContext.QueryString["busName"];
            m_stationPair = DataService.AllBuses[m_busName];
            m_dir = BusDir.go;
            llsStations.ItemsSource = CurStations().Select(x=>new StringVM(x)).ToList();
            tbBusName.Text = m_busName;
        }

        private void btnDir_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if( m_dir == BusDir.go)
            {
                if (m_stationPair.stations_back.Length == 0)
                    return;
                btnDirText.Text = "返↑";
                m_dir = BusDir.back;
                llsStations.ItemsSource = CurStations().Select(x => new StringVM(x)).ToList();
            }
            else
            {
                btnDirText.Text = "往↓";
                m_dir = BusDir.go;
                llsStations.ItemsSource = CurStations().Select(x => new StringVM(x)).ToList();
            }
        }

        private void tbBusName_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
                btnEnter_Tap(null, null);
        }

        private void btnEnter_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!CurStations().Contains(tbStation.Text))
            {
                MessageBox.Show("不存在的站牌："+tbStation.Text);
                return;
            }
            NavigationService.Navigate(new Uri(
                "/AddBusStationTag.xaml?busName={0}&station={1}&dir={2}"
                .Fmt(m_busName, tbStation.Text, m_dir.ToString()), UriKind.Relative));
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
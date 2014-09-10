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
using System.IO.IsolatedStorage;
using ScheduledTaskAgent1;
using System.Collections.ObjectModel;
using HtmlAgilityPack;
using Windows.Devices.Geolocation;
using System.Device.Location;
using System.Diagnostics;

namespace LiveBusTile
{

    public partial class AddStation : PhoneApplicationPage
    {
        public AddStation()
        {
            InitializeComponent();
        }

        string m_GroupName;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            m_GroupName = NavigationContext.QueryString.GetValue("GroupName", "");
            lbAllStations.ItemsSource = Database.AllStations.Where(x => x.Key.Contains(tbStation.Text))
                .OrderByDescending(kv => kv.Value.Count).Select(kv => kv.Key).ToList();
        }


        bool m_prevent_TextChangeEvent=false;
        private void tbStation_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (m_prevent_TextChangeEvent)
                return;
            lbAllStations.ItemsSource = Database.AllStations.Where(x => x.Key.Contains(tbStation.Text))
                .OrderByDescending(kv => kv.Value.Count).Select(kv => kv.Key).ToList();
        }


        private void tbStation_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
                btnEnter_Tap(null, null);
        }

        private void btnEnter_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!Database.AllStations.Keys.Contains(tbStation.Text))
            {
                MessageBox.Show("不存在的站牌「{0}」".Fmt(tbStation.Text));
                return;
            }
            NavigationService.Navigate(new Uri("/AddStationBuses.xaml?Station={0}&GroupName={1}"
                .Fmt(tbStation.Text, m_GroupName), UriKind.Relative));
        }

        private void lbAllStations_DoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            AppLogger.Debug("");
            string s = null;
            try
            {
                s = (e.OriginalSource as FrameworkElement).DataContext as string;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex.DumpStr());
                return;
            }
            if (s == null)
                return;
            m_prevent_TextChangeEvent = true;
            tbStation.Text = s;
            tbStation.Focus();
            tbStation.Select(s.Length, 0);

            m_prevent_TextChangeEvent = false;
        }

        private void lbAllStations_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            lbAllStations_DoubleTap(sender, e);
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (!tbStation.Text.IsNone())
            {
                tbStation.Text = "";
                if(e!=null)
                    e.Cancel = true;
                return;
            }
            base.OnBackKeyPress(e);
        }
        class StatDist : IComparable
        {
            public string station;
            /// <summary>
            /// distance in meters
            /// </summary>
            public double dist;

            public StatDist(string station, double dist)
            {
                this.station = station;
                this.dist = dist;
            }

            public int CompareTo(object obj)
            {
                StatDist b = (StatDist)obj;
                return dist.CompareTo(b.dist);
            }
        }

        private async void AppBar_OrderByMyLocation(object sender, EventArgs e)
        {
            progbar.Visibility = System.Windows.Visibility.Visible;
            Geolocator geolocator = new Geolocator();
            geolocator.DesiredAccuracyInMeters = 50;

            try
            {
                Geoposition geoposition = await geolocator.GetGeopositionAsync(
                    maximumAge: TimeSpan.FromMinutes(5), 
                    timeout: TimeSpan.FromSeconds(10.0));
                Debug.WriteLine("geoposition.Coordinate = " + geoposition.Coordinate.ToString());
                GeoCoordinate myGeo = geoposition.Coordinate.ToGeoCoordinate();
                Debug.WriteLine("myGeo = " +  myGeo.ToString() );
                TWD97Coord myCoord = TWD97Coord.FromLatLng(myGeo);
                List<StatDist> sttdsts = Database.AllStationCoords.Select(t => new StatDist(t.station, t.twd97coord.DistanceFrom2(myCoord)) ).ToList();
                sttdsts.Sort();

                lbAllStations.ItemsSource = sttdsts.Select(k => k.station).Distinct().ToList();
                
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex.DumpStr());
            }
            progbar.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void ListBoxItem_Map_Click(object sender, RoutedEventArgs e)
        {
            string station  = (sender as MenuItem).DataContext as string;
            if (station.IsNullOrEmpty())
                return;
            NavigationService.Navigate(new Uri("/StationMap.xaml?Station=" + station, UriKind.Relative));
        }
    }
    public class ExampleAllStations : List<string>
    {
        public ExampleAllStations()
        {
            AddRange(new string[]{ "六合社區", "成功得和路口", "福和橋",
             "警信新村", "秀山里", "秀景里", "范厝",
            "忠孝敦化路口", "大安國中", "成功國宅", "鳳雛公園"});

        }
    }

}
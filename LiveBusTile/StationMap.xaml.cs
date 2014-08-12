using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Windows.Devices.Geolocation;
using System.IO.IsolatedStorage;
using ScheduledTaskAgent1;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Device.Location;
using Microsoft.Phone.Maps.Controls;
using System.Runtime.CompilerServices;
using Microsoft.Phone.Maps;
using Microsoft.Phone.Maps.Toolkit;
using System.Reflection;

namespace LiveBusTile
{
    public partial class StationMap : PhoneApplicationPage
    {
        public StationMap()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.MapExtensionsSetup(this.Map);
            string station = NavigationContext.QueryString.GetValue("Station", null);
            if (station != null)
                MapNavigateToStation(station);
            else
                MapNavigateToCurGeoLocation();
        }


        /// <summary>
        /// Setup the map extensions objects.
        /// All named objects inside the map extensions will have its references properly set
        /// </summary>
        /// <param name="map">The map that uses the map extensions</param>
        private void MapExtensionsSetup(Map map)
        {
            ObservableCollection<DependencyObject> children = MapExtensions.GetChildren(map);
            var runtimeFields = this.GetType().GetRuntimeFields();

            foreach (DependencyObject i in children)
            {
                var info = i.GetType().GetProperty("Name");

                if (info != null)
                {
                    string name = (string)info.GetValue(i);

                    if (name != null)
                    {
                        foreach (FieldInfo j in runtimeFields)
                        {
                            if (j.Name == name)
                            {
                                j.SetValue(this, i);
                                if (name == "UserLocationMarker")
                                    this.UserLocationMarker = (UserLocationMarker)i;
                                if (name == "RouteDirectionsPushPin")
                                    this.RouteDirectionsPushPin = (Pushpin)i;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private double userLocationMarkerZoomLevel = 16d;
        private async void MapNavigateToCurGeoLocation()
        {
            Geolocator geolocator = new Geolocator();
            geolocator.DesiredAccuracyInMeters = 50;
            try
            {
                Geoposition geoposition = await geolocator.GetGeopositionAsync();
                this.UserLocationMarker.GeoCoordinate = geoposition.Coordinate.ToGeoCoordinate();
                this.UserLocationMarker.Visibility = System.Windows.Visibility.Visible;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex.DumpStr());
            }
            this.Map.SetView(center: this.UserLocationMarker.GeoCoordinate, zoomLevel: this.userLocationMarkerZoomLevel);
        }
        private void MapNavigateToStation(string stationName = null)
        {
            ScheduledTaskAgent1.Database.StationCoord ttt = Database.AllStationCoords.FirstOrDefault(j => j.station == stationName);
            if (ttt == null)
                return;
            this.UserLocationMarker.GeoCoordinate = ttt.geocoord;
            this.UserLocationMarker.Visibility = System.Windows.Visibility.Visible;
            tbStation.Text = stationName;

            this.Map.SetView(center: this.UserLocationMarker.GeoCoordinate, zoomLevel: this.userLocationMarkerZoomLevel);
        }

        private void Map_Loaded(object sender, RoutedEventArgs e)
        {
            MapsSettings.ApplicationContext.ApplicationId = "7e07c5f2-8778-4a7e-8180-fce35d9a0f11";
            MapsSettings.ApplicationContext.AuthenticationToken = "td9tncyt4llpOn_wgjUqbQ";
        }
    }
}
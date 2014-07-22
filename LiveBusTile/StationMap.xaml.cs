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
        bool AskLocationConsent()
        {
            if (!IsolatedStorageSettings.ApplicationSettings.Contains("LocationConsent"))
            {
                MessageBoxResult result = MessageBox.Show(
                    "This app accesses your phone's location. Is that ok?",
                    "Location", MessageBoxButton.OKCancel);

                IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = (result == MessageBoxResult.OK);
                IsolatedStorageSettings.ApplicationSettings.Save();
            }
            return (bool)IsolatedStorageSettings.ApplicationSettings.Contains("LocationConsent");
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.MapExtensionsSetup(this.Map);
            //this.UserLocationMarker = (UserLocationMarker)this.FindName("UserLocationMarker");
            //this.RouteDirectionsPushPin = (Pushpin)this.FindName("RouteDirectionsPushPin");
            MapNavigateToStation(NavigationContext.QueryString.GetValue("Station", null));
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

        //public static GeoCoordinate ConvertGeocoordinate(Geocoordinate geocoordinate)
        //{
        //    return new GeoCoordinate
        //        (
        //            geocoordinate.Latitude,
        //            geocoordinate.Longitude,
        //            geocoordinate.Altitude ?? Double.NaN,
        //            geocoordinate.Accuracy,
        //            geocoordinate.AltitudeAccuracy ?? Double.NaN,
        //            geocoordinate.Speed ?? Double.NaN,
        //            geocoordinate.Heading ?? Double.NaN
        //        );
        //}

        private double userLocationMarkerZoomLevel = 16d;

        private async void MapNavigateToStation(string stationName = null)
        {
            if(stationName.IsNullOrEmpty())
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
            }
            else
            {
                ScheduledTaskAgent1.Database.StationCoord ttt = Database.AllStationCoords.FirstOrDefault(j => j.station == stationName);
                if (ttt == null)
                    return;
                this.UserLocationMarker.GeoCoordinate = ttt.geocoord;
                this.UserLocationMarker.Visibility = System.Windows.Visibility.Visible;
                tbStation.Text = stationName;
            }

            this.Map.SetView(center: this.UserLocationMarker.GeoCoordinate, zoomLevel: this.userLocationMarkerZoomLevel);
        }

        private void Map_Loaded(object sender, RoutedEventArgs e)
        {
            const string app_id = "7e07c5f2-8778-4a7e-8180-fce35d9a0f11";
            const string auth_token = "td9tncyt4llpOn_wgjUqbQ"; 
            MapsSettings.ApplicationContext.ApplicationId = app_id;
            MapsSettings.ApplicationContext.AuthenticationToken = auth_token;
        }
    }

    public class Store : INotifyPropertyChanged
    {
        /// <summary>
        /// Address of the store
        /// </summary>
        private string address;

        /// <summary>
        /// GeoCoordinate of the store
        /// </summary>
        private GeoCoordinate geoCoordinate;

        /// <summary>
        /// Whether the store is visible or not in the map
        /// </summary>
        private Visibility visibility;

        /// <summary>
        /// Event to be raised when a property value has changed
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the GeoCoordinate of the store
        /// </summary>
        [TypeConverter(typeof(GeoCoordinateConverter))]
        public GeoCoordinate GeoCoordinate
        {
            get
            {
                return this.geoCoordinate;
            }

            set
            {
                if (this.geoCoordinate != value)
                {
                    this.geoCoordinate = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the address of the store
        /// </summary>
        public string Address
        {
            get
            {
                return this.address;
            }

            set
            {
                if (this.address != value)
                {
                    this.address = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the store Pushpin is visible or not in the map
        /// </summary>
        public Visibility Visibility
        {
            get
            {
                return this.visibility;
            }

            set
            {
                if (this.visibility != value)
                {
                    this.visibility = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Generic NotifyPropertyChanged
        /// </summary>
        /// <param name="propertyName">Name of the property</param>
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
    public class StoreList : ObservableCollection<Store>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StoreList"/> class
        /// </summary>
        public StoreList()
        {
            this.LoadData();
        }

        /// <summary>
        /// Loads the current store data into the collection
        /// </summary>
        private void LoadData()
        {
            this.Add(new Store() { GeoCoordinate = new GeoCoordinate(25.0579309, 121.515496), Address = "靜修女中" });
            this.Add(new Store() { GeoCoordinate = new GeoCoordinate(25.072088, 121.5138805), Address = "酒泉重慶北路口" });
        }
    }
    public class MapsViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MapsViewModel"/> class
        /// </summary>
        public MapsViewModel()
        {
            this.StoreList = new StoreList();
        }

        /// <summary>
        /// Gets or sets the list of stores
        /// </summary>
        public StoreList StoreList { get; set; }
    }

}
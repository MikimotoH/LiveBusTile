using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;
using LiveBusTile.Resources;

namespace LiveBusTile
{
    public partial class Settings : PhoneApplicationPage
    {
        public Settings()
        {
            InitializeComponent();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            bool bWiFiOnly = Convert.ToBoolean(ScheduledTaskAgent1.Resource1.IsWiFiOnly_Default);
            IsolatedStorageSettings.ApplicationSettings.TryGetValue("WiFiOnly", out bWiFiOnly);
            tgWifiOnly.IsChecked = bWiFiOnly;
            //base.OnNavigatedTo(e);
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            IsolatedStorageSettings.ApplicationSettings["WiFiOnly"] = tgWifiOnly.IsChecked;
            //base.OnNavigatedFrom(e);
        }
    }
}
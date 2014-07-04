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
using ScheduledTaskAgent1;

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
            bool bWiFiOnly = IsolatedStorageSettings.ApplicationSettings.GetValue(
                "WiFiOnly", Convert.ToBoolean(ScheduledTaskAgent1.Resource1.IsWiFiOnly_Default));
            tgWifiOnly.IsChecked = bWiFiOnly;

            tgUseAsyncAwait.IsChecked = IsolatedStorageSettings.ApplicationSettings.GetValue("UseAsyncAwait", false);
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            IsolatedStorageSettings.ApplicationSettings["WiFiOnly"] = tgWifiOnly.IsChecked;
            IsolatedStorageSettings.ApplicationSettings["UseAsyncAwait"] = tgUseAsyncAwait.IsChecked;
            //base.OnNavigatedFrom(e);
        }
    }
}
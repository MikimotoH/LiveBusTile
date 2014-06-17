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
    public partial class AddBusStationTag : PhoneApplicationPage
    {
        public AddBusStationTag()
        {
            InitializeComponent();
        }

        string m_busName;
        string m_station;
        BusDir m_dir;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            m_busName = NavigationContext.QueryString["busName"];
            m_station = NavigationContext.QueryString["station"];
            m_dir =     (BusDir)Enum.Parse(typeof(BusDir), NavigationContext.QueryString["dir"]);
            tbBusName.Text = m_busName;
            tbStation.Text = m_station;

            llsTags.ItemsSource = DataService.BusTags.GroupBy(x => x.tag).Select(g => new StringVM(g.Key)).ToList();
        }

        private void llsTags_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (llsTags.SelectedItem == null)
                return;
            tbTag.Text = (llsTags.SelectedItem as StringVM).String;
        }

        private void tbTag_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                btnEnter_Tap(null, null);
        }

        private void btnEnter_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            //App.RecusiveBack = true;
            NavigationService.Navigate(new Uri("/MainPage.xaml?Op=Add&busName={0}&station={1}&dir={2}&tag={3}".Fmt(m_busName, m_station,
                m_dir.ToString(), tbTag.Text), UriKind.Relative));
        }
 

    }
}
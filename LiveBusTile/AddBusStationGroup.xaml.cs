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
using System.Collections.ObjectModel;
using LiveBusTile.Resources;

namespace LiveBusTile
{
    public partial class AddBusStationGroup : PhoneApplicationPage
    {
        public AddBusStationGroup()
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
            tbDir.Text = "到：" + Database.AllBuses[m_busName].GetStations(m_dir).LastElement();

            var groups = new HashSet<string>(Database.FavBusGroups.Select(x => x.GroupName));
            groups.Add("上班");
            groups.Add("回家");
            lbGroupNames.ItemsSource = groups.ToList();
        }

        private void lbGroupNames_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (lbGroupNames.SelectedItem == null)
                return;
            tbGroupName.Text = (lbGroupNames.SelectedItem as string);
        }

        private void tbGroupName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                btnEnter_Tap(sender, null);
        }

        private void btnEnter_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if(Database.IsLegalGroupName(tbGroupName.Text)==false)
            {
                MessageBox.Show(AppResources.IllegalGroupName.Fmt( tbGroupName.Text ));
                return;

            }
            if( String.IsNullOrEmpty(tbGroupName.Text) 
                || String.IsNullOrWhiteSpace(tbGroupName.Text) 
                || tbGroupName.Text.Contains(Database.field_separator ))
            {
            }
            var busInfo = new BusInfo{Name=m_busName, Station=m_station, Dir=m_dir} ;
            var existingGroup = Database.FavBusGroups.FirstOrDefault( x=> x.GroupName == tbGroupName.Text);
            if (existingGroup == null)
            {
                Database.FavBusGroups.Add(
                    new BusGroup
                    {
                        GroupName = tbGroupName.Text,
                        Buses = new ObservableCollection<BusInfo>{busInfo}
                    });
            }
            else
            {
                existingGroup.Buses.Add(busInfo);
            }
            PhoneApplicationService.Current.State["Op"] = "Add";
            NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
        }
 

    }

    public class ExampleGroupNames : List<string>
    {
        public ExampleGroupNames()
        {
            Add("上班");
            Add("回家");
        }

    }


}
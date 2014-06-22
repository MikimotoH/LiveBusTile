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

        //string[] CurStations()
        //{
        //    return m_dir == BusDir.go ? m_stationPair.stations_go : m_stationPair.stations_back;
        //}

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            m_busName = NavigationContext.QueryString["busName"];
            m_stationPair = Database.AllBuses[m_busName];
            m_dir = BusDir.go;
            tbBusName.Text = m_busName;

            lbStationsGo.ItemsSource = m_stationPair.stations_go.ToList();
            pivotItemGo.Header = "往：" + m_stationPair.stations_go.LastElement();
            lbStationsBack.ItemsSource = m_stationPair.stations_back.ToList();
            pivotItemBack.Header = "返：" + m_stationPair.stations_back.LastElement();
        }

        //private void btnDir_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        //{
        //    if( m_dir == BusDir.go)
        //    {
        //        if (m_stationPair.stations_back.Length == 0)
        //            return;
        //        btnDirText.Text = "返";
        //        m_dir = BusDir.back;
        //    }
        //    else
        //    {
        //        btnDirText.Text = "往";
        //        m_dir = BusDir.go;
        //    }
        //    lbStationsGo.ItemsSource = CurStations().ToList();
        //    tbGoTo.Text = "到：" + CurStations().LastElement();
        //}

        private void tbBusName_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
                btnEnter_Tap(null, null);
        }

        private void btnEnter_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (! m_stationPair.GetStations(m_dir).Contains(tbStation.Text))
            {
                MessageBox.Show("不存在的站牌："+tbStation.Text);
                return;
            }
            NavigationService.Navigate(new Uri(
                "/AddBusStationGroup.xaml?busName={0}&station={1}&dir={2}"
                .Fmt(m_busName, tbStation.Text, m_dir.ToString()), UriKind.Relative));
        }


        private void lbStationsGo_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.m_AppLog.Debug("");
            if (lbStationsGo.SelectedItem == null)
                return;
            App.m_AppLog.Debug("lbStationsGo.SelectedItem=" + (lbStationsGo.SelectedItem as string));
            tbStation.Text = (lbStationsGo.SelectedItem as string);
            m_dir = BusDir.go;
        }
        private void lbStationsBack_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.m_AppLog.Debug("");
            if (lbStationsBack.SelectedItem == null)
                return;
            App.m_AppLog.Debug("lbStationsBack.SelectedItem=" + (lbStationsBack.SelectedItem as string));
            tbStation.Text = (lbStationsBack.SelectedItem as string);
            m_dir = BusDir.back;
        }


    }

    public class ExampleBusStationsGo:List<string>
    {
        public ExampleBusStationsGo()
        {
            Add("捷運永安市場站");
            Add("永安市場");
            Add("八二三紀念公園");
            Add("得和路一");
            Add("得和路");
        }
    }

    public class ExampleBusStationsBack : List<string>
    {
        public ExampleBusStationsBack()
        {
            Add("自立路");
            Add("民生路");
            Add("秀朗國小");
            Add("得和路");
            Add("永安市場");
        }
    }

}
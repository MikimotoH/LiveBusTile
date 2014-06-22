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

namespace LiveBusTile
{
    public partial class AddBus : PhoneApplicationPage
    {
        public AddBus()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            lbAllBuses.ItemsSource = Database.AllBuses.Keys;
        }

        bool m_prevent_TextChangeEvent=false;
        private void tbBusName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (m_prevent_TextChangeEvent)
                return;
            List<string> newList = Database.AllBuses.Keys.Where(x => x.Contains(tbBusName.Text)).ToList();
            lbAllBuses.ItemsSource = newList;
        }


        private void tbBusName_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
                btnEnter_Tap(null, null);
        }

        private void btnEnter_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!Database.AllBuses.Keys.Contains(tbBusName.Text))
            {
                MessageBox.Show("不存在的公車名稱："+tbBusName.Text);
                return;
            }
            NavigationService.Navigate(new Uri("/AddBusStation.xaml?busName=" + tbBusName.Text, UriKind.Relative));
        }

        private void lbAllBuses_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.m_AppLog.Debug("");
            if (lbAllBuses.SelectedItem == null)
                return;
            App.m_AppLog.Debug("lbAllBuses.SelectedItem=" + (lbAllBuses.SelectedItem as string));
            m_prevent_TextChangeEvent = true;
            tbBusName.Text = (lbAllBuses.SelectedItem as string);
            m_prevent_TextChangeEvent = false;
        }
    }

    public class ExampleAllBuses: List<string>
    {
        public ExampleAllBuses()
        {
            Add("綠2");
            Add("敦化幹線");
        }
    }
}
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
using LiveBusTile.Services;
using Log = ScheduledTaskAgent1.Logger;
using LiveBusTile.ViewModels;

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
            llsBuses.ItemsSource = DataService.AllBuses.Keys.Select(x => new StringVM {String=x}).ToList();
        }

        private void btnInputMethod_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if( (btnInputMethod.Tag as string) == "n")
            {
                btnInputMethod.Source = new BitmapImage(new Uri("Images/Input.Char.png", UriKind.Relative));
                btnInputMethod.Tag = "a";
                tbBusName.InputScope = new InputScope { Names = { new InputScopeName { NameValue = InputScopeNameValue.Text } } };
            }
            else if( (btnInputMethod.Tag  as string) == "a")
            {
                btnInputMethod.Source = new BitmapImage(new Uri("Images/Input.Number.png", UriKind.Relative));
                btnInputMethod.Tag = "n";
                tbBusName.InputScope = new InputScope { Names = { new InputScopeName { NameValue = InputScopeNameValue.Number } } };
            }
        }

        bool m_prevent_TextChangeEvent=false;
        private void tbBusName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (m_prevent_TextChangeEvent)
                return;
            llsBuses.ItemsSource = 
                (from bus in DataService.AllBuses.Keys 
                 where bus.Contains(tbBusName.Text) 
                 select new StringVM { String = bus }).ToList();
        }


        private void tbBusName_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                btnEnter_Tap(null, null);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private void btnEnter_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/AddBusStation.xaml?busName=" + tbBusName.Text, UriKind.Relative));
        }

        private void llsBuses_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Log.Debug("");
            if (llsBuses.SelectedItem == null)
                return;
            Log.Debug("llsBuses.SelectedItem=" + (llsBuses.SelectedItem as StringVM).String);
            m_prevent_TextChangeEvent = true;
            tbBusName.Text = (llsBuses.SelectedItem as StringVM).String;
            m_prevent_TextChangeEvent = false;
        }


    }
}
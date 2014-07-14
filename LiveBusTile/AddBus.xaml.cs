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

namespace LiveBusTile
{

    public partial class AddBus : PhoneApplicationPage
    {
        public AddBus()
        {
            InitializeComponent();
        }

        string m_GroupName;

        async void InitBusList()
        {
            progbar.Visibility = Visibility.Visible;

            HttpClient hc = new HttpClient();
            string sHtml = await hc.GetStringAsync(new Uri("http://pda.5284.com.tw/MQS/businfo1.jsp"));
            HtmlDocument doc = new HtmlDocument();
            HtmlNode.ElementsFlags.Remove("option");
            doc.LoadHtml(sHtml);
            List<string> busNames = new List<string>();

            for (int i = 5; ; ++i)
            {
                HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("/html/body/center/table[1]/tr[{0}]/td/select/option".Fmt(i));
                if(nodes==null)
                    break;
                busNames.AddRange(nodes.Skip(1).Select(x => x.InnerText));
            }
            // remove duplicate items, make List items Unique
            busNames = busNames.RemoveDuplicate().ToList();

            Dispatcher.BeginInvoke(() => 
            {
                llsAllBuses.ItemsSource = busNames
                    .GroupBy(b => Database.BusKeyName(b))
                    .Select(g => new KeyedBusVM(g))
                    .OrderBy(g => g.Key)
                    .ToObservableCollection();            
            });
            progbar.Visibility = Visibility.Collapsed;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            m_GroupName = NavigationContext.QueryString.GetValue("GroupName", "");

            llsAllBuses.ItemsSource = Database.AllBuses.Keys
                .GroupBy(b => Database.BusKeyName(b))
                .Select(g => new KeyedBusVM(g))
                .OrderBy(g => g.Key)
                .ToObservableCollection();
            //InitBusList();
        }


        bool m_prevent_TextChangeEvent=false;
        private void tbBusName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (m_prevent_TextChangeEvent)
                return;

            llsAllBuses.ItemsSource = Database.AllBuses.Keys
                .Where(x => x.Contains(tbBusName.Text))
                .GroupBy(b => Database.BusKeyName(b))
                .Select(g => new KeyedBusVM(g))
                .ToObservableCollection();
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
            NavigationService.Navigate(new Uri("/AddBusStation.xaml?BusName={0}&GroupName={1}"
                .Fmt(tbBusName.Text, m_GroupName), UriKind.Relative));
        }

        private void llsAllBuses_DoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
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
            tbBusName.Text = s;
            tbBusName.Focus();
            tbBusName.Select(s.Length, 0);

            m_prevent_TextChangeEvent = false;
        }

        private void llsAllBuses_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            llsAllBuses_DoubleTap(sender, e);
            //AppLogger.Debug("e.OriginalSource=" + e.OriginalSource);
            //if (e.OriginalSource == null)
            //    return;
            //AppLogger.Debug("e.OriginalSource.GetType()=" + e.OriginalSource.GetType());
            //var fe = e.OriginalSource as FrameworkElement;
            //if (fe == null)
            //    return;
            //AppLogger.Debug("fe.DataContext=" + fe.DataContext);
            //if (fe.DataContext == null)
            //    return;
            //AppLogger.Debug("fe.DataContext.GetType()=" + fe.DataContext.GetType());
            //AppLogger.Debug("(e.OriginalSource as FrameworkElement).DataContext.GetType()=" + fe.DataContext.GetType());
            /*
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
            AppLogger.Debug("s=" + s);

            m_prevent_TextChangeEvent = true;
            tbBusName.Text = s;
            tbBusName.Focus();
            tbBusName.Select(s.Length, 0);

            m_prevent_TextChangeEvent = false;
             * */
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (!tbBusName.Text.IsNone())
            {
                tbBusName.Text = "";
                if(e!=null)
                    e.Cancel = true;
                return;
            }
            base.OnBackKeyPress(e);
        }
    }

}
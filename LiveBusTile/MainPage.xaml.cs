using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Collections.ObjectModel;
using System.ComponentModel;
using ScheduledTaskAgent1;
using LiveBusTile.Resources;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.IO.IsolatedStorage;
using HtmlAgilityPack;
using System.Threading;
using System.IO;

namespace LiveBusTile
{
    public partial class MainPage : PhoneApplicationPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private ObservableCollection<GroupBusVM> GenFavGroupBusVM()
        {
            var vm = new ObservableCollection<GroupBusVM>();
            foreach (var y in Database.FavBusGroups)
            {
                vm.Add(new GroupBusVM(y.m_GroupName));
                foreach (var x in y.m_Buses)
                    vm.Add(new GroupBusVM(x, y.m_GroupName));
            }
            return vm;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            tbLastUpdatedTime.Text = Database.LastUpdatedTime.ToString("HH:mm:ss");
            lbBus.ItemsSource = GenFavGroupBusVM();

            if ((string)PhoneApplicationService.Current.State.GetValue("Op", "") != "")
            {
                while (NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry();
                PhoneApplicationService.Current.State.Remove("Op");
            }

        }

        private void BusItem_Delete_Click(object sender, RoutedEventArgs e)
        {
            var gbvm = (sender as MenuItem).DataContext as GroupBusVM;
            BusInfo busInfo = gbvm.BusInfo;

            BusGroup group = Database.FavBusGroups.FirstOrDefault(g => g.m_GroupName == gbvm.GroupName);
            if (group == null)
            {
                AppLogger.Error("can not find group which contains busInfo=" + busInfo);
                return;
            }

            group.m_Buses.Remove(busInfo);
            if (group.m_Buses.Count == 0)
                Database.FavBusGroups.Remove(group);

            lbBus.ItemsSource = GenFavGroupBusVM();
        }

        private void BusItem_Details_Click(object sender, RoutedEventArgs e)
        {
            var gbvm = (sender as MenuItem).DataContext as GroupBusVM;
            GotoDetailsPage(gbvm.BusInfo, gbvm.GroupName);
        }

        void GotoDetailsPage(BusInfo busInfo, string groupName)
        {
            PhoneApplicationService.Current.State["busInfo"] = busInfo;
            PhoneApplicationService.Current.State["groupName"] = groupName;
            NavigationService.Navigate(new Uri(
                "/BusStationDetails.xaml", UriKind.Relative));
        }


        private void AppBar_Pin_Click(object sender, EventArgs e)
        {
            AppLogger.Debug("");
            Database.SaveFavBusGroups();
            GroupPage.CreateTile("");
        }

        private void AppBar_Refresh_Click(object sender, EventArgs e)
        {
            if (Database.FavBuses.Count() == 0)
                return;

            bool bUseAsyncAwait = IsolatedStorageSettings.ApplicationSettings.GetValue("UseAsyncAwait", false);

            if (bUseAsyncAwait == true)
                AppBar_Refresh_Click_UseAsyncAwait(sender, e);
            else
                AppBar_Refresh_HttpWebRequest();
        }

        static string ParseHtmlBusTime(string html){
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            try
            {
                HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes(
                    "/html/body/center/table/tr[6]/td");
                if (nodes.Count == 0)
                    return "節點找不到";
                return nodes[0].InnerText;
            }
            catch (Exception ex)
            {
                AppLogger.Error(html);
                AppLogger.Error(ex.DumpStr());
                return "解析異常";                
            }
        }

        class HttpData
        {
            public HttpWebRequest req;
            public int finHttpReqs;
            public int sucHttpResps;
        }

        private void ReadWebRequestCallback(IAsyncResult callbackResult)
        {
            HttpData httpData = (HttpData)callbackResult.AsyncState;
            HttpWebRequest myRequest = httpData.req;

            try
            {
                HttpWebResponse myResponse = (HttpWebResponse)myRequest.EndGetResponse(callbackResult);
                using (StreamReader httpwebStreamReader = new StreamReader(myResponse.GetResponseStream()))
                {
                    string results = httpwebStreamReader.ReadToEnd();
                    httpData.sucHttpResps++;
                    AppLogger.Msg("Succeeded Http Responses =" + httpData.sucHttpResps);

                    string timeToArrive = ParseHtmlBusTime(results);
                    BusInfo bus = Database.FavBuses[httpData.finHttpReqs];
                    AppLogger.Msg("bus={0}, timeToArrive={1}".Fmt(bus, timeToArrive));

                    Dispatcher.BeginInvoke(() =>
                    {
                        lbBus.Items.Cast<GroupBusVM>().FirstOrDefault(b => b.BusInfo == bus).TimeToArrive = timeToArrive;
                    });
                }
                myResponse.Close();
            }
            catch (WebException ex)
            {
                AppLogger.Error(ex.DumpStr());
            }


            httpData.finHttpReqs++;
            AppLogger.Msg("finished HttpReqs=" + httpData.finHttpReqs);
            if (httpData.finHttpReqs == Database.FavBuses.Length)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    Database.SaveFavBusGroups();

                    ApplicationBar.Buttons.Cast<ApplicationBarIconButton>().DoForEach(x => x.IsEnabled = true);
                    ApplicationBar.IsMenuEnabled = true;
                    progbar.Visibility = Visibility.Collapsed;

                    if (httpData.sucHttpResps > 0)
                    {
                        tbLastUpdatedTime.Text = Database.LastUpdatedTime.ToString("HH:mm:ss");
                        List<string> groupNames = Database.FavBusGroups.Select(x => x.m_GroupName).ToList();
                        groupNames.Insert(0, "");
                        foreach (var groupName in groupNames)
                        {
                            TileUtil.UpdateTile2(groupName);
                            AppLogger.Debug("UpdateTile(groupName=\"{0}\") - finished".Fmt(groupName));
                        }
                    }
                    else
                        MessageBox.Show(AppResources.NetworkFault);
                });

            }
            else
            {
                HttpWebRequest_BeginGetResponse(httpData.finHttpReqs, httpData.sucHttpResps);
            }
        }

        void HttpWebRequest_BeginGetResponse(int finHttpReqs, int sucHttpResps)
        {
            AppLogger.Debug("finHttpReqs={0}, sucHttpResps={1}".Fmt(finHttpReqs, sucHttpResps));
            BusInfo bus = Database.FavBuses[finHttpReqs];
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(new
                Uri(@"http://pda.5284.com.tw/MQS/businfo3.jsp?Mode=1&Dir={1}&Route={0}&Stop={2}"
                .Fmt(Uri.EscapeUriString(bus.m_Name), bus.m_Dir == BusDir.go ? 1 : 0, Uri.EscapeUriString(bus.m_Station))));
            if (req.Headers == null)
                req.Headers = new WebHeaderCollection();

            req.Headers["Cache-Control"] = "max-age=0";
            req.Headers["Pragma"] = "no-cache";
            req.BeginGetResponse(new AsyncCallback(ReadWebRequestCallback), new HttpData { req = req, finHttpReqs= finHttpReqs, sucHttpResps = sucHttpResps});
        }

        void AppBar_Refresh_HttpWebRequest()
        {
            progbar.Visibility = Visibility.Visible;
            ApplicationBar.Buttons.Cast<ApplicationBarIconButton>().DoForEach(x => x.IsEnabled = false);
            ApplicationBar.IsMenuEnabled = false;

            HttpWebRequest_BeginGetResponse(0,0);
        }



        private async void AppBar_Refresh_Click_UseAsyncAwait(object sender, EventArgs e)
        {
            AppLogger.Debug("enter sender=" + sender.GetType());
            if (Database.FavBuses.Count() == 0)
                return;
            
            progbar.Visibility = Visibility.Visible;
            ApplicationBar.Buttons.Cast<ApplicationBarIconButton>().DoForEach(x => x.IsEnabled = false);
            ApplicationBar.IsMenuEnabled = false;

            Task<string>[] tasks = Database.FavBuses.Select(b => BusTicker.GetBusDueTime(b)).ToArray();

            try
            {
                await Task.Run(() =>
                {
                    Task.WaitAll(tasks);
                });
            }
            catch (Exception ex)
            {
                AppLogger.Error("Task.WaitAll(tasks) failed");
                AppLogger.Error(ex.DumpStr());
            }

            int numSucceededTasks = 0;
            for (int i = 0; i < tasks.Length; ++i)
            {
                if (tasks[i].Status == TaskStatus.RanToCompletion)
                {
                    Database.FavBuses[i].m_TimeToArrive = tasks[i].Result;
                    ++numSucceededTasks;
                }
            }
            AppLogger.Debug("m_finHttpReqs=" + numSucceededTasks);

            if (numSucceededTasks > 0)
            {
                Database.SaveFavBusGroups();
                lbBus.ItemsSource = GenFavGroupBusVM();
                tbLastUpdatedTime.Text = Database.LastUpdatedTime.ToString("HH:mm:ss");

                List<string> groupNames = Database.FavBusGroups.Select(x => x.m_GroupName).ToList();
                groupNames.Insert(0, "");
                foreach (var groupName in groupNames)
                {
                    try
                    {
                        TileUtil.UpdateTile2(groupName);
                        AppLogger.Debug("UpdateTile(groupName=\"{0}\") - finished".Fmt(groupName));
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error("TileUtil.UpdateTile( groupName={0} ) failed\n".Fmt(groupName) + ex.DumpStr());
                    }
                }
            }
            
            ApplicationBar.Buttons.Cast<ApplicationBarIconButton>().DoForEach(x => x.IsEnabled = true);
            ApplicationBar.IsMenuEnabled = true;
            progbar.Visibility = Visibility.Collapsed;

            if (numSucceededTasks == 0)
                MessageBox.Show(AppResources.NetworkFault);

            AppLogger.Debug("exit");
        }

        private void AppBar_AddBus_Click(object sender, EventArgs e)
        {
            AppLogger.Debug("");
            NavigationService.Navigate(new Uri("/AddBus.xaml", UriKind.Relative));
        }

        private void AppBar_Settings_Click(object sender, EventArgs e)
        {
            AppLogger.Debug("");
            NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));
        }

        private void AppBar_About_Click(object sender, EventArgs e)
        {
            AppLogger.Debug("");
            NavigationService.Navigate(new Uri("/About.xaml", UriKind.Relative));
        }

        private void ListNameMenuItem_Rename_Click(object sender, RoutedEventArgs e)
        {
            AppLogger.Debug("");
        }

        private void lbBus_DoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            AppLogger.Debug("");
            var gbvm = (e.OriginalSource as FrameworkElement).DataContext as GroupBusVM;
            BusInfo busInfo = gbvm.BusInfo;
            if (busInfo == null)
            {
                NavigationService.Navigate(new Uri("/GroupPage.xaml?GroupName=" + gbvm.GroupName, UriKind.Relative));
                return;
            }
            
            GotoDetailsPage(busInfo, gbvm.GroupName);
        }



    }

    public class GroupBusVM : INotifyPropertyChanged
    {
        public GroupBusVM() { }
        public GroupBusVM(string groupName) { GroupName = groupName; }
        public GroupBusVM(BusInfo b, string groupName) { m_BusInfo = b; GroupName = groupName; }

        public string GroupName{get;set;}

        BusInfo m_BusInfo;
        public BusInfo BusInfo { get { return m_BusInfo; } }

        public string BusName { get { return m_BusInfo.m_Name; } set { if (m_BusInfo.m_Name != value) { m_BusInfo.m_Name = value; NotifyPropertyChanged("BusName"); } } }
        public string Station { get { return m_BusInfo.m_Station; } set { if (m_BusInfo.m_Station != value) { m_BusInfo.m_Station = value; NotifyPropertyChanged("Station"); } } }

        public string TimeToArrive {
            get { return m_BusInfo.m_TimeToArrive; }
            set { if (m_BusInfo.m_TimeToArrive != value) { m_BusInfo.m_TimeToArrive = value; NotifyPropertyChanged("TimeToArrive"); } } 
        }

        public bool IsGroupHeader { get { return m_BusInfo==null; } }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            if (null != PropertyChanged)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ExampleGroupBusVM : ObservableCollection<GroupBusVM>
    {
        public ExampleGroupBusVM()
        {
            Add(new GroupBusVM("上班") );
            Add(new GroupBusVM(new BusInfo { m_Name = "橘2", m_Station = "秀山國小", m_TimeToArrive = "無資料" }, "上班"));
            Add(new GroupBusVM(new BusInfo { m_Name = "敦化幹線", m_Station = "秀景里", m_TimeToArrive = "無資料" }, "上班"));

            Add(new GroupBusVM ("回家") );
            Add(new GroupBusVM(new BusInfo { m_Name = "橘2", m_Station = "捷運永安市場站", m_TimeToArrive = "無資料" }, "回家"));
            Add(new GroupBusVM(new BusInfo { m_Name = "275", m_Station = "忠孝敦化路口", m_TimeToArrive = "無資料" }, "回家"));
        }
    }

    public abstract class TemplateSelector : ContentControl
    {
        public abstract DataTemplate SelectTemplate(object item, DependencyObject container);

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            ContentTemplate = SelectTemplate(newContent, this);
        }
    }

    public class GroupBusTemplateSelector : TemplateSelector
    {
        public DataTemplate GroupHeader{get;set;}

        public DataTemplate BusInfo{get;set;}

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var gb = item as GroupBusVM;
            if (gb.IsGroupHeader)
                return GroupHeader;
            else
                return BusInfo;
        }
    }

}
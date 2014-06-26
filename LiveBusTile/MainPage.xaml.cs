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
            //App.m_AppLog.Debug("Screen Width = " + Application.Current.RootVisual.RenderSize.Width);

            lbBus.ItemsSource = GenFavGroupBusVM();

            if ((string)PhoneApplicationService.Current.State.GetValue("Op", "") == "Add")
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
                App.m_AppLog.Error("can not find group which contains busInfo=" + busInfo);
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
            App.m_AppLog.Debug("");
            Database.SaveFavBusGroups();


            var tileData = new StandardTileData
            {
                Title = DateTime.Now.ToString("HH:mm:ss"),
            };
            ShellTile.Create(new Uri("/MainPage.xaml?DefaultTitle=FromTile", UriKind.Relative), tileData);

            ScheduledAgent.UpdateTileJpg();
        }

        private async void AppBar_Refresh_Click(object sender, EventArgs e)
        {
            App.m_AppLog.Debug("enter sender=" + sender.GetType());
            if (Database.FavBuses.Count() == 0)
                return;
            
            prgbarWaiting.Visibility = Visibility.Visible;
            this.ApplicationBar.Buttons.DoForEach<ApplicationBarIconButton>(x => x.IsEnabled = false);
            this.ApplicationBar.IsMenuEnabled = false;

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
                App.m_AppLog.Error("Task.WaitAll(tasks) failed");
                App.m_AppLog.Error(ex.DumpStr());
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
            App.m_AppLog.Debug("numSucceededTasks=" + numSucceededTasks);

            if (numSucceededTasks > 0)
            {
                Database.SaveFavBusGroups();
                lbBus.ItemsSource = GenFavGroupBusVM();
                ScheduledAgent.UpdateTileJpg();
            }
            
            //foreach (var btn in this.ApplicationBar.Buttons)
            //    (btn as ApplicationBarIconButton).IsEnabled = true;
            this.ApplicationBar.Buttons.DoForEach<ApplicationBarIconButton>(x => x.IsEnabled = true);
            this.ApplicationBar.IsMenuEnabled = true;
            prgbarWaiting.Visibility = Visibility.Collapsed;

            if (numSucceededTasks == 0)
                MessageBox.Show(AppResources.NetworkFault);

            App.m_AppLog.Debug("exit");
        }

        private void AppBar_AddBus_Click(object sender, EventArgs e)
        {
            App.m_AppLog.Debug("");
            NavigationService.Navigate(new Uri("/AddBus.xaml", UriKind.Relative));
        }

        private void AppBar_Settings_Click(object sender, EventArgs e)
        {
            App.m_AppLog.Debug("");
            NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));
        }

        private void AppBar_About_Click(object sender, EventArgs e)
        {
            App.m_AppLog.Debug("");
            NavigationService.Navigate(new Uri("/About.xaml", UriKind.Relative));
        }

        private void ListNameMenuItem_Rename_Click(object sender, RoutedEventArgs e)
        {
            App.m_AppLog.Debug("");
        }

        private void lbBus_DoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.m_AppLog.Debug("");
            var gbvm = (e.OriginalSource as FrameworkElement).DataContext as GroupBusVM;
            BusInfo busInfo = gbvm.BusInfo;
            if (busInfo == null)
            {
                NavigationService.Navigate(new Uri("/ChangeGroupName.xaml?groupName=" + gbvm.GroupName, UriKind.Relative));
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
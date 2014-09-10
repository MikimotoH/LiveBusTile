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
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

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
            tbLastUpdatedTime.Text = Database.LastUpdatedTime.ToString(TileUtil.CurSysTimeFormat);
            lbBus.ItemsSource = GenFavGroupBusVM();

            if ((string)PhoneApplicationService.Current.State.GetValue("Op", "") != "")
            {
                while (NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry();
                PhoneApplicationService.Current.State.Remove("Op");
            }
            string groupName = PhoneApplicationService.Current.State.GetValue("ScrollToGroup", "").Cast<string>();
            if ( groupName != "")
            {
                lbBus.UpdateLayout();
                object item = lbBus.Items.FirstOrDefault(o => o.Cast<GroupBusVM>().IsGroupHeader && o.Cast<GroupBusVM>().GroupName == groupName);
                if (item != null)
                    lbBus.ScrollIntoView(item);
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
            if (gbvm == null)
                return;
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

        class WCVM
        {
            public WebClient wc = new WebClient();
            public GroupBusVM vm;
            public WCVM(GroupBusVM vm)
            {
                this.vm = vm;
                this.wc.Headers = new WebHeaderCollection();
                this.wc.Headers[HttpRequestHeader.IfModifiedSince] = DateTime.UtcNow.ToString("R");
                this.wc.Headers["Cache-Control"] = "no-cache";
                this.wc.Headers["Pragma"] = "no-cache";
            }
        }
        
        WCVM[] m_WCVMs;

        private void AppBar_Refresh_Click(object sender, EventArgs e)
        {
            if (Database.FavBuses.Count() == 0)
                return;

            if (progbar.Visibility == Visibility.Visible)
            {
                m_WCVMs.DoForEach( wcvm => wcvm.wc.CancelAsync());
                return;
            }

            progbar.Visibility = Visibility.Visible;
            ApplicationBar.Buttons.Cast<ApplicationBarIconButton>().DoForEach(x => x.IsEnabled = false);
            ApplicationBarIconButton btnRefresh = sender as ApplicationBarIconButton;
            btnRefresh.IsEnabled = true;
            btnRefresh.IconUri = new Uri("/Images/AppBar.StopRefresh.png", UriKind.Relative);

            m_WCVMs = lbBus.Items.Cast<GroupBusVM>().Where(gb => !gb.IsGroupHeader).Select(b => new WCVM(b)).ToArray();
            int m_CompletedWCs = 0;
            int m_CancelledWCs = 0;
            int m_SucceededWCs = 0;
            
            foreach (var wcvm in m_WCVMs)
            {
                wcvm.wc.DownloadStringCompleted += (s, asyncCompletedEventArgs) =>
                {
                    int completedWCs = Interlocked.Increment(ref m_CompletedWCs);
                    int succeededWCs = Interlocked.Add(ref m_SucceededWCs, asyncCompletedEventArgs.Error == null?1:0);
                    int cancelledWCs = Interlocked.Add(ref m_CancelledWCs, asyncCompletedEventArgs.Cancelled ? 1 : 0);
                    AppLogger.Debug("completedWCs={0}, succeededWCs={1}, cancelledWCs={2}"
                        .Fmt(completedWCs, succeededWCs, cancelledWCs));

                    if (asyncCompletedEventArgs.Error == null)
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            GroupBusVM vm = asyncCompletedEventArgs.UserState as GroupBusVM;
                            vm.TimeToArrive = BusTicker.ParseHtmlBusTime(asyncCompletedEventArgs.Result);
                            tbLastUpdatedTime.Text = DateTime.Now.ToString(TileUtil.CurSysTimeFormat);
                        });
                    }
                    else
                        AppLogger.Error(asyncCompletedEventArgs.Error.DumpStr());

                    if (completedWCs == m_WCVMs.Length)
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            if(succeededWCs > 0)
                            {
                                Database.SaveFavBusGroups();

                                List<string> groupNames = Database.FavBusGroups.Select(x => x.m_GroupName).ToList();
                                groupNames.Insert(0, "");
                                foreach (var groupName in groupNames)
                                    TileUtil.UpdateTile2(groupName);
                            }
                            else if (cancelledWCs == 0)
                            {
                                MessageBox.Show(AppResources.NetworkFault);
                            }
                            
                            btnRefresh.IconUri = new Uri("/Images/AppBar.Refresh.png", UriKind.Relative);
                            ApplicationBar.Buttons.Cast<ApplicationBarIconButton>().DoForEach(x => x.IsEnabled = true);
                            progbar.Visibility = Visibility.Collapsed;
                        });
                    }
                };
                wcvm.wc.DownloadStringAsync(new Uri(BusTicker.Pda5284Url(wcvm.vm.BusInfo)), wcvm.vm);
            }
        }


        private void AppBar_AddBus_Click(object sender, EventArgs e)
        {
            AppLogger.Debug("");
            NavigationService.Navigate(new Uri("/AddBus.xaml", UriKind.Relative));
        }
        private void AppBar_AddStation_Click(object sender, EventArgs e)
        {
            AppLogger.Debug("");
            NavigationService.Navigate(new Uri("/AddStation.xaml", UriKind.Relative));
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


        private void lbBus_DoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            AppLogger.Debug("");
            var gbvm = (e.OriginalSource as FrameworkElement).DataContext as GroupBusVM;
            if (gbvm == null) 
                return;
            BusInfo busInfo = gbvm.BusInfo;
            if (busInfo == null)
            {
                NavigationService.Navigate(new Uri("/GroupPage.xaml?GroupName=" + gbvm.GroupName, UriKind.Relative));
                return;
            }
            
            GotoDetailsPage(busInfo, gbvm.GroupName);
        }

        private void GroupHeader_Delete_Click(object sender, RoutedEventArgs e)
        {
            var gbvm = (sender as MenuItem).DataContext as GroupBusVM;
            if (gbvm == null)
                return;
            string groupName = gbvm.GroupName;

            bool bRemoveOK = Database.FavBusGroups.Remove(Database.FavBusGroups.FirstOrDefault(g=>g.m_GroupName==groupName));
            if (!bRemoveOK)
                AppLogger.Error("Database.FavBusGroups.Remove(m_BusGroup) failed");


            Database.SaveFavBusGroups();
            lbBus.ItemsSource = GenFavGroupBusVM();

            ShellTile tile = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString() == TileUtil.TileUri(groupName));
            if (tile != null)
                tile.Delete();
            TileUtil.UpdateTile2("");

        }

        public static bool m_SelectionMode = false;
        private void AppBar_Selection_ModeEnter(object sender, EventArgs e)
        {
            ApplicationBar.IsMenuEnabled = false;
            var btns = ApplicationBar.Buttons.Cast<ApplicationBarIconButton>().ToArray();
            
            btns[0].IconUri = new Uri("/Images/Btn-Move-Down.png", UriKind.Relative);
            btns[0].Text = "移下";
            btns[0].IsEnabled = false;
            btns[0].Click -= AppBar_Pin_Click;
            btns[0].Click += AppBar_Selection_MoveDown; 
            
            btns[1].IconUri = new Uri("/Images/Btn-Move-Up.png", UriKind.Relative);
            btns[1].Text = "移上";
            btns[1].IsEnabled = false;
            btns[1].Click -= AppBar_Refresh_Click;
            btns[1].Click += AppBar_Selection_MoveUp;
            
            btns[2].IconUri = new Uri("/Images/AppBar.Delete.png", UriKind.Relative);
            btns[2].Text = "刪除";
            btns[2].IsEnabled = false;
            btns[2].Click -= AppBar_AddBus_Click;
            btns[2].Click += AppBar_Selection_Remove;

            btns[3].IconUri = new Uri("/Images/AppBar.ExitMode.Png", UriKind.Relative);
            btns[3].Text = "離開管理模式";
            btns[3].Click -= AppBar_AddStation_Click;
            btns[3].Click += AppBar_Selection_ModeExit;

            var items = ApplicationBar.MenuItems.Cast<ApplicationBarMenuItem>().ToArray();
            items[1].IsEnabled = false;

            lbBus.SelectedItem = null;
            //while (lbBus.SelectedItems !=null && lbBus.SelectedItems.Count>0)
            //    lbBus.SelectedItems.RemoveAt(0);

            m_SelectionMode = true;
        }

        void AppBar_Selection_ModeExit(object sender, EventArgs e)
        {
            lbBus.SelectedItem = null;
            //while (lbBus.SelectedItems != null && lbBus.SelectedItems.Count > 0)
            //    lbBus.SelectedItems.RemoveAt(0);
            m_SelectionMode = false;

            var btns = ApplicationBar.Buttons.Cast<ApplicationBarIconButton>().ToArray();

            btns[0].IconUri = new Uri("/Images/AppBar.Pin.png", UriKind.Relative);
            btns[0].Text = "釘至桌面";
            btns[0].IsEnabled = true;
            btns[0].Click -= AppBar_Selection_MoveDown;
            btns[0].Click += AppBar_Pin_Click ;

            btns[1].IconUri = new Uri("/Images/AppBar.Refresh.png", UriKind.Relative);
            btns[1].Text = "刷新時間";
            btns[1].IsEnabled = true;
            btns[1].Click -= AppBar_Selection_MoveUp;
            btns[1].Click += AppBar_Refresh_Click ;

            btns[2].IconUri = new Uri("/Images/AppBar.AddBus.png", UriKind.Relative);
            btns[2].Text = "新增巴士";
            btns[2].IsEnabled = true;
            btns[2].Click -= AppBar_Selection_Remove;
            btns[2].Click += AppBar_AddBus_Click;

            btns[3].IconUri = new Uri("/Images/AppBar.AddStation.png", UriKind.Relative);
            btns[3].Text = "新增站牌";
            btns[3].Click -= AppBar_Selection_ModeExit ;
            btns[3].Click += AppBar_AddStation_Click;

            ApplicationBar.IsMenuEnabled = true;

            var items = ApplicationBar.MenuItems.Cast<ApplicationBarMenuItem>().ToArray();
            items[1].IsEnabled = true;
        }


        void AppBar_Selection_MoveDown(object sender, EventArgs e)
        {
            var all_items = (ObservableCollection<GroupBusVM>)lbBus.ItemsSource;
            var selected_items = lbBus.SelectedItems.Cast<GroupBusVM>().ToArray();
            var selected_indices = selected_items.Select(x => all_items.IndexOf(x)).ToArray();
            var selected = Enumerable.Zip(selected_items, selected_indices, (x, y) => new { groupBusVM = x, index = y })
                .OrderByDescending(x => x.index).ToArray();
            if (selected.Length == 0)
                return;

            bool changed = false;
            foreach (var s in selected)
            {
                var group = Database.FavBusGroups.LastOrDefault(x => x.m_GroupName == s.groupBusVM.GroupName);
                if (s.groupBusVM.IsGroupHeader)
                {
                    int groupIndex = Database.FavBusGroups.LastIndexOf( group );
                    if (groupIndex == Database.FavBusGroups.Count - 1)
                        break;
                    Database.FavBusGroups.SwapByIndex(groupIndex, groupIndex+1);
                    changed = true;
                }
                else
                {
                    int busIndex = group.m_Buses.LastIndexOf(s.groupBusVM.BusInfo);
                    if (busIndex == group.m_Buses.Count - 1)
                        break;
                    group.m_Buses.SwapByIndex(busIndex, busIndex+1);
                    changed = true;                    
                }
            }

            if (changed)
            {
                Database.SaveFavBusGroups();

                var vm = GenFavGroupBusVM();
                var resultedIndices = selected.Select(x => vm.IndexOf(x.groupBusVM)).ToArray();
                
                lbBus.ItemsSource = vm;
                lbBus.UpdateLayout();
                foreach (var i in resultedIndices)
                {
                    var listBoxItem = lbBus.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                    if (listBoxItem != null)
                    {
                        listBoxItem.IsSelected = true;
                    }
                }
            }
        }

        void AppBar_Selection_MoveUp(object sender, EventArgs e)
        {
            var all_items = (ObservableCollection<GroupBusVM>)lbBus.ItemsSource;
            var selected_items = lbBus.SelectedItems.Cast<GroupBusVM>().ToArray();
            var selected_indices  = selected_items.Select(x => all_items.IndexOf(x)).ToArray();
            var selected = Enumerable.Zip(selected_items, selected_indices, (x, y) => new { groupBusVM=x, index=y}).OrderBy(x=>x.index).ToArray();
            if (selected.Length == 0)
                return;

            bool changed = false;
            foreach (var s in selected)
            {
                var group = Database.FavBusGroups.FirstOrDefault(x => x.m_GroupName == s.groupBusVM.GroupName);
                if (s.groupBusVM.IsGroupHeader)
                {
                    int groupIndex = Database.FavBusGroups.IndexOf(group);
                    if (groupIndex == 0)
                        break;
                    Database.FavBusGroups.SwapByIndex(groupIndex, groupIndex - 1);
                    changed = true;
                }
                else
                {
                    int busIndex = group.m_Buses.IndexOf(s.groupBusVM.BusInfo);
                    if (busIndex == 0)
                        break;
                    group.m_Buses.SwapByIndex(busIndex, busIndex - 1);
                    changed = true;
                }
            }

            if (changed)
            {
                Database.SaveFavBusGroups();

                var vm = GenFavGroupBusVM();
                var resultedIndices = selected.Select(x => vm.IndexOf(x.groupBusVM)).ToArray();

                lbBus.ItemsSource = vm;
                lbBus.UpdateLayout();
                foreach (var i in resultedIndices)
                {
                    var listBoxItem = lbBus.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                    if (listBoxItem != null)
                    {
                        listBoxItem.IsSelected = true;
                    }
                }
            }
        }


        void AppBar_Selection_Remove(object sender, EventArgs e)
        {
            var all_items = (ObservableCollection<GroupBusVM>)lbBus.ItemsSource;
            var selected_items = lbBus.SelectedItems.Cast<GroupBusVM>().ToArray();
            var selected_indices = selected_items.Select(x => all_items.IndexOf(x)).ToArray();
            var selected = Enumerable.Zip(selected_items, selected_indices, (x, y) => new { groupBusVM = x, index = y })
                .OrderByDescending(x => x.index).ToArray();

            if (selected.Length == 0)
                return;

            foreach (var s in selected)
            {
                var group = Database.FavBusGroups.LastOrDefault(x => x.m_GroupName == s.groupBusVM.GroupName);
                if (s.groupBusVM.IsGroupHeader)
                    Database.FavBusGroups.Remove(group);
                else
                    group.m_Buses.Remove(s.groupBusVM.BusInfo);
            }

            Database.SaveFavBusGroups();
            lbBus.ItemsSource = GenFavGroupBusVM();
        }

        private void lbBus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(m_SelectionMode==false)
            {
                lbBus.SelectedItem = null;
                return;
            }

            if (lbBus.SelectedItems != null &&  lbBus.SelectedItems.Count>0)
            {
                var btn  = ApplicationBar.Buttons.Cast<ApplicationBarIconButton>().ToArray();
                btn[0].IsEnabled = true;
                btn[1].IsEnabled = true;
                btn[2].IsEnabled = true;

                //try
                //{
                //    var indices = lbBus.SelectedItems.Cast<GroupBusVM>().Select(
                //        v => lbBus.ItemContainerGenerator.IndexFromContainer((lbBus.ItemContainerGenerator.ContainerFromItem(v))))
                //        .OrderBy(j => j).ToArray();
                //    Debug.WriteLine("lbBus.SelectedItems Indices = " + indices.DumpArray());
                //}
                //catch (Exception ex)
                //{
                //    AppLogger.Error(ex.DumpStr());
                //}

            }
            else
            {
                var btn = ApplicationBar.Buttons.Cast<ApplicationBarIconButton>().ToArray();
                btn[0].IsEnabled = false;
                btn[1].IsEnabled = false;
                btn[2].IsEnabled = false;
            }

            Debug.WriteLine("lbBus_SelectionChanged() e.AddedItems.Count={0}, e.RemovedItems.Count={1}, lbBus.SelectedItems.Count={2}",
                e.AddedItems.Count, e.RemovedItems.Count, lbBus.SelectedItems!=null?lbBus.SelectedItems.Count:0);
        }


        //private void Border_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        //{
        //    Debug.WriteLine("Border_Tap");
        //    var border = sender as Border;
        //    if (border == null)
        //        return;
        //    var gbvm = border.DataContext as GroupBusVM;
        //    if (gbvm == null)
        //        return;
        //    if (m_SelectionMode == false)
        //    {
        //        Debug.WriteLine("m_SelectionMode == false");
        //        return;
        //    }

        //}

        private void lbiBorder_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Debug.WriteLine("lbiBorder_Tap, sender={0}  e.OriginalSource={1}", sender.GetType(), e.OriginalSource.GetType());
            var cp = sender as ContentPresenter;
            var gbvm = cp.DataContext as GroupBusVM;

            ListBoxItem item = lbBus.ItemContainerGenerator.ContainerFromItem(gbvm).Cast<ListBoxItem>();
            if (item.IsSelected)
            {
                item.BorderThickness = new Thickness(3);
                item.BorderBrush = new SolidColorBrush(Color.FromArgb(0xff, 210, 105, 30));
            }
            else
            {
                item.BorderThickness = new Thickness(0);
                item.BorderBrush = new SolidColorBrush(Color.FromArgb(0xff, 210, 105, 30));
            }
            Debug.WriteLine("item.IsSelected=" + item.IsSelected);
        }

        private async void GroupHeader_Rename_Click(object sender, RoutedEventArgs e)
        {
            var gbvm = (sender as MenuItem).DataContext as GroupBusVM;
            if (gbvm == null)
                return;
            string newGroupName = await PromptForGroupName(gbvm.GroupName);
            Database.FavBusGroups.FirstOrDefault(x => x.m_GroupName == gbvm.GroupName).m_GroupName = newGroupName;
            Database.SaveFavBusGroups();

            lbBus.ItemsSource = GenFavGroupBusVM();
        }

        Task<string> PromptForGroupName(string oldName)
        {
            var tcs = new TaskCompletionSource<string>();

            TextBox textBox = new TextBox
            {
                Text = oldName,
            };
            textBox.SelectAll();

            CustomMessageBox messageBox = new CustomMessageBox()
            {
                Message = "更改群組名稱：",
                Content = textBox,
                RightButtonContent = "確定",
                LeftButtonContent = "取消",
                IsFullScreen = false
            };

            messageBox.Dismissing += (s1, e1) =>
            {
                if(!Database.IsLegalGroupName(textBox.Text))
                {
                    e1.Cancel = true;
                }
                else if (textBox.Text != oldName && 
                    Database.FavBusGroups.FirstOrDefault(x => x.m_GroupName == textBox.Text) != null)
                {
                    e1.Cancel = true;
                }
            };

            messageBox.Dismissed += (s2, e2) =>
            {
                switch (e2.Result)
                {
                    case CustomMessageBoxResult.LeftButton:
                        tcs.SetResult(oldName);
                        break;
                    case CustomMessageBoxResult.RightButton:
                        tcs.SetResult( textBox.Text );
                        break;
                }
            };

            messageBox.Show();

            return tcs.Task;
        }

        private void BusItem_ContextBusTime_Click(object sender, RoutedEventArgs e)
        {
            var gbvm = (sender as MenuItem).DataContext as GroupBusVM;
            if (gbvm == null)
                return;
            NavigationService.Navigate(new Uri(
                "/ContextBusTime.xaml?BusName={0}&Dir={1}&Station={2}"
                .Fmt(gbvm.BusInfo.m_Name, gbvm.BusInfo.m_Dir, gbvm.BusInfo.m_Station),
                UriKind.Relative));
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

        public override bool Equals(object obj)
        {
            GroupBusVM b = (GroupBusVM)obj;
            if (b == null ||
                this.IsGroupHeader != b.IsGroupHeader )
                return false;
            if (this.IsGroupHeader)
                return GroupName.Equals(b.GroupName);
            else
                return m_BusInfo.Equals(b.m_BusInfo) && GroupName.Equals(b.GroupName);
        }
        public override int GetHashCode()
        {
            if (this.IsGroupHeader)
                return GroupName.GetHashCode();
            else
                return m_BusInfo.GetHashCode() ^ GroupName.GetHashCode();
        }
        public override string ToString()
        {
            if (IsGroupHeader)
                return GroupName;
            else
                return m_BusInfo.ToString();
        }

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
    public class BooleanToThicknessConverter : IValueConverter
    {
        static Thickness Thickness0 = new Thickness(0d);
        static Thickness Thickness3 = new Thickness(3d);
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(MainPage.m_SelectionMode)
            {
                return System.Convert.ToBoolean(value) ? Thickness3 : Thickness0;
            }
            return Thickness0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var thickness = (Thickness)value;
            if(thickness==null)
            {
                Debug.WriteLine("thickness == null");
                return false;
            }
            return thickness.Left > 0;
        }
    }
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToBoolean(value) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(Visibility.Visible);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using LiveBusTile.Resources;
using LiveBusTile.ViewModels;
using System.Windows.Controls.Primitives;
using Microsoft.Phone.Scheduler;
using System.Diagnostics;
using Log = ScheduledTaskAgent1.Logger;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.IO;
using ScheduledTaskAgent1;
using System.Threading;
using System.Collections.ObjectModel;


namespace LiveBusTile
{
    public partial class MainPage : PhoneApplicationPage
    {
        public MainPage()
        {
            InitializeComponent();

            //// Set the data context of the listbox control to the sample data
            //this.Loaded += new RoutedEventHandler( MainPage_Loaded );
            
            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

        
        // Load data for the ViewModel Items
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Log.Debug("e=" + e.DumpStr());
            Log.Debug("NavigationContext.QueryString=" + NavigationContext.QueryString.DumpStr());
            //Log.Debug("App.RecusiveBack=" + App.RecusiveBack);


            if (NavigationContext.QueryString.GetValue("Op", "") == "Add" 
                //&& App.RecusiveBack==true 
                && e.NavigationMode == NavigationMode.New)
            {
                while (NavigationService.CanGoBack)
                {
                    JournalEntry jo = NavigationService.RemoveBackEntry();
                    Log.Debug("jo.Source=" + jo.Source);
                }

                DataService.AddBus(new BusTag
                {
                    busName = NavigationContext.QueryString["busName"],
                    station = NavigationContext.QueryString["station"],
                    dir = (BusDir)Enum.Parse(typeof(BusDir), NavigationContext.QueryString["dir"]),
                    tag = NavigationContext.QueryString["tag"]
                });
                //App.RecusiveBack = false;
                //DataService.SaveData();
            }
            DataContext = new KeyedBusTagVM();

            Log.Debug("exit");
            //NavigationContext.QueryString.Clear();
        }


        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Log.Debug("e="+e.DumpStr());
            Log.Debug("NavigationContext.QueryString=" + NavigationContext.QueryString.DumpStr());

            Log.Debug("exit");
            //NavigationContext.QueryString.Clear();
        }
        
        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {

            //Log.Debug("NavigationService.CanGoBack="+NavigationService.CanGoBack);
            //if (NavigationService.CanGoBack)
            //{
            //    Log.Debug("NavigationService.BackStack[" + NavigationService.BackStack.Count() + "]={"
            //        + ", ".Joyn(NavigationService.BackStack.Select(j => j.Source.ToString())) + "}");
            //while (NavigationService.CanGoBack)
            //{
                //JournalEntry jo = NavigationService.RemoveBackEntry();
                //Log.Debug("jo.Source=" + jo.Source);
                //Log.Debug("NavigationService.BackStack[" + NavigationService.BackStack.Count() + "]={"
                //    + ", ".Joyn(NavigationService.BackStack.Select(j => j.Source.ToString())) + "}");
            //}
            //    Log.Debug("NavigationService.BackStack.Count()=" + NavigationService.BackStack.Count());
            //}

            //Log.Debug("NavigationService.BackStack[" + NavigationService.BackStack.Count() + "]={"
            //    + ", ".Joyn(NavigationService.BackStack.Select(j => j.Source.ToString())) + "}");
            //Log.Debug("NavigationService.CanGoBack=" + NavigationService.CanGoBack);

            base.OnBackKeyPress(e);
        }


        public static void StartPeriodicAgent()
        {
            string taskName = "refreshBusTileTask";
            // Obtain a reference to the period task, if one exists
            PeriodicTask refreshBusTileTask = ScheduledActionService.Find(taskName) as PeriodicTask;

            // If the task already exists and background agent is enabled for the
            // app, remove the task and then add it again to update 
            // the schedule.
            if (refreshBusTileTask != null)
            {
                RemoveAgent();
            }
            refreshBusTileTask = new PeriodicTask(taskName);
            refreshBusTileTask.Description = "Refresh Bus Due Time on Tile at Hub (HomeScreen)";

            // Place the call to add a periodic agent. This call must be placed in 
            // a try block in case the user has disabled agents.
            try
            {
                ScheduledActionService.Add(refreshBusTileTask);

                ScheduledActionService.LaunchForTest(taskName, TimeSpan.FromSeconds(1));
                Log.Debug("ScheduledActionService.LaunchForTest(taskName, TimeSpan.FromSeconds(1))");
            }
            catch (InvalidOperationException exception)
            {
                Log.Error(exception.ToString());
                if (exception.Message.Contains("BNS Error: The action is disabled"))
                {
                    Log.Error("Background agents for this application have been disabled by the user.");
                }
                else if (exception.Message.Contains("BNS Error: The maximum number of ScheduledActions of this type have already been added."))
                {
                    // No user action required. The system prompts the user when the hard limit of periodic tasks has been reached.
                    Log.Error("BNS Error: The maximum number of ScheduledActions of this type have already been added.");
                }
                else
                {
                    Log.Error("An InvalidOperationException occurred.\n" + exception.ToString());
                }
            }
            catch (SchedulerServiceException e)
            {
                Log.Error(e.ToString());
            }
            finally
            {
                // Determine if there is a running periodic agent and update the UI.
                //refreshBusTileTask = ScheduledActionService.Find(taskName) as PeriodicTask;
                //if (refreshBusTileTask != null)
                //{
                //}
            }
        }

        public static void RemoveAgent()
        {
            string taskName = "refreshBusTileTask";
            Log.Debug("taskName="+taskName);
            PeriodicTask refreshBusTileTask = ScheduledActionService.Find(taskName) as PeriodicTask;
            if (refreshBusTileTask == null)
            {
                Log.Debug("ScheduledActionService.Find("+taskName+") returns null.");
                return;
            }

            try
            {
                Log.Debug("ScheduledActionService.Remove("+taskName+")");
                ScheduledActionService.Remove(taskName);
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }
        private void ApplicationBarMenuItem_Click(object sender, EventArgs e)
        {
            var mi = sender as ApplicationBarMenuItem;
            HandleApplicationBar(mi.Text);
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            var btn = sender as ApplicationBarIconButton;
            HandleApplicationBar(btn.Text);
        }

        void HandleApplicationBar(string text)
        {
            switch (text)
            {
                case "釘至桌面":
                    {
                        DataService.SaveData();
                        var busTags = (from bus in DataService.BusTags orderby bus.tag select bus).ToArray();
                        ScheduledTaskAgent1.ScheduledAgent.GenerateTileJpg(
                            "\n".Joyn(busTags.Select(x => x.busName + " " + x.timeToArrive)));

                        ShellTile tile = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains("DefaultTitle=FromTile"));
                        var tileData = new StandardTileData
                        {
                            Title = DateTime.Now.ToString("HH:mm:ss"),
                            BackgroundImage = new Uri("isostore:/" + @"Shared\ShellContent\Tile.jpg", UriKind.Absolute),
                        };
                        if (tile == null)
                            ShellTile.Create( new Uri("/MainPage.xaml?DefaultTitle=FromTile", UriKind.Relative),  tileData);
                        else
                            tile.Update(tileData);
                    }
                    break;

                case "刷新時間":
                    RefreshBusTime();
                    break;
                case "新增巴士":
                    NavigationService.Navigate(new Uri("/AddBus.xaml", UriKind.Relative));
                    break;
                //case "add station":
                    //NavigationService.Navigate(new Uri("/AddStation.xaml", UriKind.Relative));
                    //DataService.AddBus(DataService.RandomBusTag());
                    //DataContext = new KeyedBusTagVM();
                    //break;
                case "關於…":
                    NavigationService.Navigate(new Uri("/About.xaml", UriKind.Relative));
                    break;
                case "設定":
                    NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));
                    break;
                default:
                    break;
            }
        }



        Action<Action> runAtUI = (a) => { Deployment.Current.Dispatcher.BeginInvoke(a); };

        void RefreshBusTime()
        {
            prgbarWaiting.Visibility = Visibility.Visible;
            foreach (var btn in this.ApplicationBar.Buttons)
                (btn as ApplicationBarIconButton).IsEnabled = false;
            this.ApplicationBar.IsMenuEnabled = false;

            var busTags = DataService.BusTags;
            ScheduledAgent.RefreshBusTime(DataService.BusTags);
            foreach (var btn in this.ApplicationBar.Buttons)
                (btn as ApplicationBarIconButton).IsEnabled = true;
            this.ApplicationBar.IsMenuEnabled = true;
            prgbarWaiting.Visibility = Visibility.Collapsed;
        }

        private void Item_Delete_Click(object sender, RoutedEventArgs e)
        {
            BusTagVM bt = (sender as MenuItem).DataContext as BusTagVM;
            DataService.DeleteBus(bt);
            DataContext = new KeyedBusTagVM();
        }

        private void Item_Details_Click(object sender, RoutedEventArgs e)
        {
            GotoDetailsPage((sender as MenuItem).DataContext as BusTagVM);
        }

        private void BusCatLLS_DoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (BusCatLLS.SelectedItem == null)
            {
                Log.Debug("e={{ OriginalSource={0}, Handled={1} }}".Fmt(e.OriginalSource, e.Handled));
                return;
            }
            GotoDetailsPage(BusCatLLS.SelectedItem as BusTagVM);
        }
        void GotoDetailsPage(BusTagVM bt)
        {
            NavigationService.Navigate(new Uri(
                "/BusStationDetails.xaml?busName={0}&station={1}&dir={2}&tag={3}"
                .Fmt(bt.busName, bt.station, bt.dir, bt.tag), UriKind.Relative));
        }




        // Sample code for building a localized ApplicationBar
        //private void BuildLocalizedApplicationBar()
        //{
        //    // Set the page's ApplicationBar to a new instance of ApplicationBar.
        //    ApplicationBar = new ApplicationBar();

        //    // Create a new button and set the text value to the localized string from AppResources.
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // Create a new menu item with the localized string from AppResources.
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}
    }

}
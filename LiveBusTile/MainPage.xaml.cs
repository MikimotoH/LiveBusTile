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
using LiveBusTile.Services;
using System.Threading;


namespace LiveBusTile
{
    public partial class MainPage : PhoneApplicationPage
    {
        static PeriodicTask refreshBusTileTask;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            //// Set the data context of the listbox control to the sample data
            //this.Loaded += new RoutedEventHandler( MainPage_Loaded );
            
            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

        const string m_tileImgPath = @"Shared\ShellContent\Tile.jpg";

        
        // Load data for the ViewModel Items
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (NavigationContext.QueryString.Count==0)
            {
                this.DataContext = new KeyedBusTagVM();
                ShellTile tile = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains("DefaultTitle=FromTile"));

                //if (tile == null)
                //    ShellTile.Create(new Uri("/MainPage.xaml?DefaultTitle=FromTile", UriKind.Relative), new StandardTileData { Title = DateTime.Now.ToString("HH:mm:ss") });

            }
            else if(NavigationContext.QueryString.ContainsKey("Op"))
            {
                var newBusTag = new BusTag
                {
                    busName = NavigationContext.QueryString["busName"],
                    station = NavigationContext.QueryString["station"],
                    dir = (BusDir)int.Parse(NavigationContext.QueryString["dir"]),
                    tag = NavigationContext.QueryString["tag"]
                };
                lock (ScheduledAgent.m_busTagLock)
                {
                    ScheduledAgent.m_busTags.Add(newBusTag);
                }
                BusCatLLS.DataContext = 
                this.DataContext = new KeyedBusTagVM();
            }
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
        }

        void timer_Tick(object sender, EventArgs e)
        {
        }



        private void PinToStart_Click(object sender, RoutedEventArgs e)
        {
            //StartPeriodicAgent(refreshBusTileTaskName);
        }


        const string refreshBusTileTaskName = "refreshBusTileTask";
        const string refreshBusTileTaskDesc = "Refresh Bus Due Time on Tile at Hub (HomeScreen)";
        public static void StartPeriodicAgent()
        {
            string taskName = refreshBusTileTaskName;
            // Obtain a reference to the period task, if one exists
            refreshBusTileTask = ScheduledActionService.Find(taskName) as PeriodicTask;

            // If the task already exists and background agent is enabled for the
            // app, remove the task and then add it again to update 
            // the schedule.
            if (refreshBusTileTask != null)
            {
                Log.Debug("RemoveAgent()");
                RemoveAgent();
            }
            refreshBusTileTask = new PeriodicTask(taskName);

            // The description is required for periodic agents. This is the string that the user
            // will see in the background services Settings page on the phone.
            refreshBusTileTask.Description = refreshBusTileTaskDesc;

            // Place the call to add a periodic agent. This call must be placed in 
            // a try block in case the user has disabled agents.
            try
            {
                ScheduledActionService.Add(refreshBusTileTask);
                // If debugging is enabled, use LaunchForTest to launch the agent in one minute.
                ScheduledActionService.LaunchForTest(taskName, TimeSpan.FromMilliseconds(1000));
                Log.Debug("ScheduledActionService.LaunchForTest(taskName, TimeSpan.FromMilliseconds(1000))");
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
            string taskName = refreshBusTileTaskName;
            //refreshBusTileTask = ScheduledActionService.Find(taskName) as PeriodicTask;

            // If the task already exists and background agent is enabled for the
            // app, remove the task and then add it again to update 
            // the schedule.
            //if (refreshBusTileTask != null)
            //{
                //Log.Debug("RemoveAgent()");
                //RemoveAgent();
            //}

            try
            {
                Log.Debug("ScheduledActionService.Remove(busName)");
                ScheduledActionService.Remove(taskName);
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }
        static Random random = new Random();
        Func<string> rndMinutes = () => random.Next(60).ToString("D2")+"分";
        Func<string> rndHours = () => random.Next(60).ToString("D2") + "小時";
        Func<string> rndStation = () => random.Next(99).ToString("D2") + "站";
        Func<string> rndBus = () => "公車"+random.Next(1000);
        Func<BusDir> rndDir = () => random.Next(1) == 1 ? BusDir.go : BusDir.back;

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            var btn = sender as ApplicationBarIconButton;
            switch (btn.Text)
            {
                case "refresh":
                    new Thread(() =>
                    {
                        RefreshBusTime();
                    }).Start();
                    
                    break;
                case "add bus":
                    //NavigationService.Navigate(new Uri("/AddBus.xaml", UriKind.Relative));
                    DataService.m_list.Add(new BusTagVM( DataService.RandomBusTag()));
                    DataContext = new KeyedBusTagVM();
                    break;
                case "add station":
                    //NavigationService.Navigate(new Uri("/AddStation.xaml", UriKind.Relative));
                    DataService.m_list.Add(new BusTagVM( DataService.RandomBusTag()));
                    DataContext = new KeyedBusTagVM();
                    break;
                default:
                    break;
            }
        }

        static Task<string> GetBusDueTime(BusTagVM bus)
        {
            return BusTicker.GetBusDueTime(bus.busName, bus.station, bus.dir);
        }

        void RefreshBusTime()
        {
            //prgbarWaiting.Visibility = Visibility.Visible;
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                foreach (var btn in this.ApplicationBar.Buttons)
                    (btn as ApplicationBarIconButton).IsEnabled = false;
                this.ApplicationBar.IsMenuEnabled = false;
            });

            Task<string>[] tasks = new Task<String>[DataService.m_list.Count];
            for (int i = 0; i < DataService.m_list.Count; ++i)
                tasks[i] = GetBusDueTime(DataService.m_list[i]);

            Task.WaitAll(tasks);
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                for (int i = 0; i < DataService.m_list.Count; ++i)
                    DataService.m_list[i].timeToArrive = tasks[i].Result;
                this.DataContext = new KeyedBusTagVM();
                //prgbarWaiting.Visibility = Visibility.Collapsed;

                foreach (var btn in this.ApplicationBar.Buttons)
                    (btn as ApplicationBarIconButton).IsEnabled = true;
                this.ApplicationBar.IsMenuEnabled = true;

            });
            
            //Log.Debug("WaitAll begin");
            //foreach (var bus in DataService.m_list)
            //{
            //    bus.timeToArrive = GetBusDueTime(bus);
            //    Log.Debug(String.Format("{0} {1} {2}", bus.busName, bus.station, bus.timeToArrive));
            //}
            //Log.Debug("WaitAll end");            
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
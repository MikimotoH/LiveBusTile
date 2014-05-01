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


namespace LiveBusTile
{
    public partial class MainPage : PhoneApplicationPage
    {
        PeriodicTask refreshBusTileTask;
        const string refreshBusTileTaskName = "refreshBusTileTask";
        const string refreshBusTileTaskDesc = "Refresh Bus Due Time on Tile at Hub (HomeScreen)";
        
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Set the data context of the listbox control to the sample data
            //DataContext = App.ViewModel;

            this.Loaded += new RoutedEventHandler( MainPage_Loaded );
            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

        //void Test2()
        //{
        //    StackTrace st = new StackTrace(true);
        //    Debug.WriteLine(" Stack trace for current level: {0}", st.ToString());
        //    StackFrame sf = st.GetFrame(0);
        //    Debug.WriteLine(" File: {0}", sf.GetFileName());
        //    Debug.WriteLine(" Method: {0}", sf.GetMethod().Name);
        //    Debug.WriteLine(" Line Number: {0}", sf.GetFileLineNumber());
        //    Debug.WriteLine(" Column Number: {0}", sf.GetFileColumnNumber());
        //}

        const string m_tileImgPath = @"Shared\ShellContent\Tile.jpg";
        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine("{0} Test2() start", DateTime.Now.ToString("HH:mm:ss.fff"));
            //Test2();
            //Debug.WriteLine("{0} Test2() end", DateTime.Now.ToString("HH:mm:ss.fff"));
            var rtbus = new RunTimeBusCatVM();
            rtbus.UpdateFromWebAsync(this);

            var vm = new BusCatViewModel();
            this.DataContext = vm;
            ShellTile tile = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains("DefaultTitle=FromTile"));

            if (tile == null)
            {
                ShellTile.Create(new Uri("/MainPage.xaml?DefaultTitle=FromTile", UriKind.Relative), 
                    new StandardTileData { Title = DateTime.Now.ToString("HH:mm:ss") });
            }

            StartPeriodicAgent(refreshBusTileTaskName);
        }

        // Load data for the ViewModel Items
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
            }
        }

        // Handle selection changed on LongListSelector
        private void MainLongListSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If selected item is null (no selection) do nothing
            if (BusCatLLS.SelectedItem == null)
                return;

            // Navigate to the new page
            NavigationService.Navigate(new Uri("/DetailsPage.xaml?selectedItem=" + (BusCatLLS.SelectedItem as ItemViewModel).LineOne, UriKind.Relative));

            // Reset selected item to null (no selection)
            BusCatLLS.SelectedItem = null;
        }

        private void PinToStart_Click(object sender, RoutedEventArgs e)
        {
            //StartPeriodicAgent(refreshBusTileTaskName);
        }


        private void StartPeriodicAgent(string taskName)
        {
            // Obtain a reference to the period task, if one exists
            refreshBusTileTask = ScheduledActionService.Find(taskName) as PeriodicTask;

            // If the task already exists and background agent is enabled for the
            // app, remove the task and then add it again to update 
            // the schedule.
            if (refreshBusTileTask != null)
            {
                Log.Debug("RemoveAgent(taskName)");
                RemoveAgent(taskName);
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
                ScheduledActionService.LaunchForTest(taskName, TimeSpan.FromMilliseconds(5));
                Log.Debug("ScheduledActionService.LaunchForTest(taskName, TimeSpan.FromMilliseconds(5))");
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

        private void RemoveAgent(string name)
        {
            try
            {
                ScheduledActionService.Remove(name);
                Log.Debug("ScheduledActionService.Remove(busName)");
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
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
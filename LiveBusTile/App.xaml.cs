using System;
using System.Diagnostics;
using System.Resources;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using LiveBusTile.Resources;
using ScheduledTaskAgent1;
using System.IO.IsolatedStorage;
using System.IO;
using System.Collections.ObjectModel;
using Microsoft.Phone.Scheduler;
using System.Windows.Threading;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LiveBusTile
{
    public partial class App : Application
    {
        
        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public static PhoneApplicationFrame RootFrame { get; private set; }

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            AppLogger.Debug("App ctor()");
            //Log.Debug("App ctor() m_RecusiveBack=" + m_RecusiveBack);
            //Log.Debug("App ctor() {0} {1}".Fmt(Debugger.IsAttached, Application.Current.ApplicationLifetimeObjects.Count));
            IsolatedStorageFile.GetUserStoreForApplication().CreateDirectory(@"Shared\ShellContent");

            // Global handler for uncaught exceptions.
            UnhandledException += Application_UnhandledException;

            // Standard XAML initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            // Language display initialization
            InitializeLanguage();

            // Show graphics profiling information while debugging.
            if (Debugger.IsAttached)
            {
                // Display the current frame rate counters
                Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode,
                // which shows areas of a page that are handed off to GPU with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Prevent the screen from turning off while under the debugger by disabling
                // the application's idle detection.
                // Caution:- Use this under debug mode only. Application that disables user idle detection will continue to run
                // and consume battery power when the user is not using the phone.
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }
            RemovePeriodicAgent();
        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            AppLogger.Msg("e=" + e.ToString());
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            AppLogger.Msg("e.IsApplicationInstancePreserved=" + e.IsApplicationInstancePreserved);
            RunningInBackground = false;
            Database.LoadFavBusGroups();

            if (m_timer != null)
            {
                m_timer.Dispose();
                //m_timer.Stop();
                m_timer = null;
            }
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            AppLogger.Msg("e.Reason=" + e.Reason);
            Database.SaveFavBusGroups();
            StartPeriodicAgent();
            //StartTimer();
        }

        //System.Windows.Threading.DispatcherTimer m_timer;
        System.Threading.Timer m_timer;
        //void StartTimer()
        //{
        //    if(m_timer!=null)
        //        return;
        //    m_timer = new System.Threading.Timer(new System.Threading.TimerCallback(timer_Tick), null, 3*1000, 60 * 1000);
        //    //m_timer = new DispatcherTimer();
        //    //m_timer.Interval = TimeSpan.FromSeconds(60);
        //    //m_timer.Tick+=timer_Tick;
        //    //m_timer.Start();
        //}

        //void timer_Tick(object sender)
        //{
        //    AppLogger.Debug("Database.FavBuses.Count()=" + Database.FavBuses.Count());
        //    if (Database.FavBuses.Count() == 0)
        //        return;
        //    try
        //    {
        //        Task<string>[] tasks = Database.FavBuses.Select(b => BusTicker.GetBusDueTime(b)).ToArray();
        //        AppLogger.Debug("tasks.Length=" + tasks.Length);
        //        Task.WaitAll(tasks);
        //        for(int i=0; i<Database.FavBuses.Length; ++i)
        //        {
        //            if(tasks[i].Status == TaskStatus.RanToCompletion)
        //                Database.FavBuses[i].m_TimeToArrive = tasks[i].Result;
        //        }
        //        Database.SaveFavBusGroups();

        //        ShellTile tile = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString() == ScheduledAgent.TileUri(""));
        //        if (tile != null)
        //        {
        //            Deployment.Current.Dispatcher.BeginInvoke(() =>
        //            {
        //                try
        //                {
        //                    ScheduledAgent.UpdateTile("");
        //                }
        //                catch (Exception ex)
        //                {
        //                    AppLogger.Error(ex.DumpStr());
        //                }
        //            });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        AppLogger.Error(ex.DumpStr());
        //    }
        //    AppLogger.Debug("exit");
        //}
        

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            AppLogger.Msg("Application_Closing, e=" + e);
            Database.SaveFavBusGroups();
            StartPeriodicAgent();
            // Ensure that required application state is persisted here.
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            AppLogger.Error("e={{ Exception={0}, e.Handled={1}, e.Uri={2} }}".Fmt(e.Exception, e.Handled, e.Uri));
            if (Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            AppLogger.Error("e.ExceptionObject={0}\n Handled={1}".Fmt(
                e.ExceptionObject.DumpStr(), e.Handled));
            if (Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                Debugger.Break();
            }
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            //AppLogger.Debug("phoneApplicationInitialized=" + phoneApplicationInitialized);
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Handle reset requests for clearing the backstack
            RootFrame.Navigated += CheckForResetNavigation;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            //AppLogger.Debug("e=" + e.DumpStr());
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        private void CheckForResetNavigation(object sender, NavigationEventArgs e)
        {

            // If the app has received a 'reset' navigation, then we need to check
            // on the next navigation to see if the page stack should be reset
            if (e.NavigationMode == NavigationMode.Reset)
                RootFrame.Navigated += ClearBackStackAfterReset;
        }

        private void ClearBackStackAfterReset(object sender, NavigationEventArgs e)
        {
            // Unregister the event so it doesn't get called again
            RootFrame.Navigated -= ClearBackStackAfterReset;

            // Only clear the stack for 'new' (forward) and 'refresh' navigations
            if (e.NavigationMode != NavigationMode.New && e.NavigationMode != NavigationMode.Refresh)
                return;

            // For UI consistency, clear the entire page stack
            while (RootFrame.RemoveBackEntry() != null)
            {
                ; // do nothing
            }
            
        }

        #endregion

        // Initialize the app's font and flow direction as defined in its localized resource strings.
        //
        // To ensure that the font of your application is aligned with its supported languages and that the
        // FlowDirection for each of those languages follows its traditional direction, ResourceLanguage
        // and ResourceFlowDirection should be initialized in each resx file to match these values with that
        // file's culture. For example:
        //
        // AppResources.es-ES.resx
        //    ResourceLanguage's value should be "es-ES"
        //    ResourceFlowDirection's value should be "LeftToRight"
        //
        // AppResources.ar-SA.resx
        //     ResourceLanguage's value should be "ar-SA"
        //     ResourceFlowDirection's value should be "RightToLeft"
        //
        // For more info on localizing Windows Phone apps see http://go.microsoft.com/fwlink/?LinkId=262072.
        //
        private void InitializeLanguage()
        {
            try
            {
                // Set the font to match the display language defined by the
                // ResourceLanguage resource string for each supported language.
                //
                // Fall back to the font of the neutral language if the Display
                // language of the phone is not supported.
                //
                // If a compiler error is hit then ResourceLanguage is missing from
                // the resource file.
                RootFrame.Language = XmlLanguage.GetLanguage(AppResources.ResourceLanguage);

                // Set the FlowDirection of all elements under the root frame based
                // on the ResourceFlowDirection resource string for each
                // supported language.
                //
                // If a compiler error is hit then ResourceFlowDirection is missing from
                // the resource file.
                FlowDirection flow = (FlowDirection)Enum.Parse(typeof(FlowDirection), AppResources.ResourceFlowDirection);
                RootFrame.FlowDirection = flow;
            }
            catch
            {
                // If an exception is caught here it is most likely due to either
                // ResourceLangauge not being correctly set to a supported language
                // code or ResourceFlowDirection is set to a value other than LeftToRight
                // or RightToLeft.

                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                throw;
            }
        }

        static bool m_RunningInBackground = false;
        public static bool RunningInBackground { get { return m_RunningInBackground; } set { m_RunningInBackground = value; } }

        private void Application_RunningInBackground(object sender, RunningInBackgroundEventArgs e)
        {
            AppLogger.Msg("e=" + e.ToString());
            RunningInBackground = true;
            StartPeriodicAgent();
            //StartTimer();
        }


        //public const string m_taskName = "refreshBusTileTask";
        public void RemovePeriodicAgent()
        {
            AppLogger.Debug("m_taskName=" + ScheduledAgent.m_taskName);
            PeriodicTask refreshBusTileTask = ScheduledActionService.Find(ScheduledAgent.m_taskName) as PeriodicTask;
            if (refreshBusTileTask == null)
                return;
            try
            {
                AppLogger.Debug("ScheduledActionService.Remove(\"{0}\")".Fmt(ScheduledAgent.m_taskName));
                ScheduledActionService.Remove(ScheduledAgent.m_taskName);
            }
            catch (Exception e)
            {
                AppLogger.Error(e.ToString());
            }
        }


        public void StartPeriodicAgent()
        {
            RemovePeriodicAgent();
            PeriodicTask refreshBusTileTask = new PeriodicTask(ScheduledAgent.m_taskName);
            refreshBusTileTask.Description = "Refresh Bus Due Time on Tile at Hub (HomeScreen)";

            // Place the call to add a periodic agent. This call must be placed in 
            // a try block in case the user has disabled agents.
            try
            {
                ScheduledActionService.Add(refreshBusTileTask);
                ScheduledAgent.LaunchIn30sec(ScheduledAgent.m_taskName);
            }
            catch (InvalidOperationException exception)
            {
                AppLogger.Error(exception.DumpStr());
                if (exception.Message.Contains("BNS Error: The action is disabled"))
                {
                    AppLogger.Error("Background agents for this application have been disabled by the user.");
                }
                else if (exception.Message.Contains("BNS Error: The maximum number of ScheduledActions of this type have already been added."))
                {
                    // No user action required. The system prompts the user when the hard limit of periodic tasks has been reached.
                    AppLogger.Error("BNS Error: The maximum number of ScheduledActions of this type have already been added.");
                }
                else
                {
                    AppLogger.Error("An InvalidOperationException occurred.\n" + exception.ToString());
                }
            }
            catch (SchedulerServiceException e)
            {
                AppLogger.Error(e.DumpStr());
            }
            catch(Exception e2)
            {
                AppLogger.Error(e2.DumpStr());
            }
        }

    }
}
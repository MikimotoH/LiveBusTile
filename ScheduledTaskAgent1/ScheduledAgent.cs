using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.CompilerServices;


namespace ScheduledTaskAgent1
{
    public class ScheduledAgent : ScheduledTaskAgent
    {
        /// <remarks>
        /// ScheduledAgent constructor, initializes the UnhandledException handler
        /// </remarks>
        static ScheduledAgent()
        {
            // Subscribe to the managed exception handler
            Deployment.Current.Dispatcher.BeginInvoke(delegate
            {
                Application.Current.UnhandledException += UnhandledException;
            });
        }

        /// Code to execute on Unhandled Exceptions
        private static void UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            Logger.Error("ScheduledAgent.UnhandledException()\n e.ExceptionObject={0}\n\n e.Handled={1}"
                .Fmt(e.ExceptionObject.DumpStr(), e.Handled));
            if (Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                Debugger.Break();
            }
        }

        public const string m_taskName = "refreshBusTileTask";

        public static void LaunchIn30sec(string taskName)
        {
#if ENABLE_LAUNCHFORTEST
            try
            {
                ScheduledActionService.LaunchForTest(taskName, TimeSpan.FromSeconds(30));
                Logger.Msg("LaunchForTest - finish)");
            }
            catch (Exception ex)
            {
                Logger.Error("ScheduledActionService.LaunchForTest() failed, ex=" + ex.DumpStr());
            }
#endif
        }

 

        /// <summary>
        /// 71 is WiFi & 6 is Ethernet(LAN),   243 & 244 is 3G/Mobile
        /// </summary>
        public enum IanaInterfaceType :uint
        {
            WiFi = 71u,
            LAN = 6u,
            _3G = 243u,
            _3GMobile = 244u,
        }

        public static bool WifiConnected()
        {
            var profile = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();
            uint interfaceType = profile.NetworkAdapter.IanaInterfaceType;
            return (interfaceType == (uint)IanaInterfaceType.WiFi || interfaceType == (uint)IanaInterfaceType.LAN);
        }

        void Epilog(ScheduledTask task, 
            [CallerMemberName] string func = "",
            [CallerFilePath] string path = "",
            [CallerLineNumber] int line = 0)
        {
            System.Diagnostics.Debug.WriteLine("{0}<{1}>{2}:{3}:{4} [{5}] ScheduledAgent.Epilog() enter", 
                DateTime.Now.ToString("yyMMdd_HH:mm:ss.fff"), LogLevel.Debug, 
                Path.GetFileName(path), func, line, System.Threading.Thread.CurrentThread.ManagedThreadId);

#if ENABLE_LAUNCHFORTEST
            LaunchIn30sec(task.Name);
#endif
            Logger.Close();
            this.NotifyComplete();
        }

        protected override void OnInvoke(ScheduledTask task)
        {
            try
            {
                Logger.Msg("task="+task.DumpStr());

                bool bWiFiOnly = Convert.ToBoolean(Resource1.IsWiFiOnly_Default);
                IsolatedStorageSettings.ApplicationSettings.TryGetValue("WiFiOnly", out bWiFiOnly);
                if ( (bWiFiOnly && !WifiConnected())
                    || ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString() == TileUtil.TileUri("")) == null)
                {
                    return;
                }

                Task<string>[] tasks = Database.FavBuses.Select(b => BusTicker.GetBusDueTime(b)).ToArray();
                Task.WaitAll(tasks);

                int numCompletedTask = 0;
                for (int i = 0; i < tasks.Length; ++i)
                {
                    if (tasks[i].Status == TaskStatus.RanToCompletion)
                    {
                        Database.FavBuses[i].m_TimeToArrive = tasks[i].Result;
                        ++numCompletedTask;
                    }
                }
                if (numCompletedTask == 0)
                {
                    return;
                }


                var uiFinished = new AutoResetEvent(false);

                Database.SaveFavBusGroups();
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        List<string> groupNames = Database.FavBusGroups.Select(x => x.m_GroupName).ToList();
                        groupNames.Insert(0, "");
                        foreach (var groupName in groupNames)
                        {
                            try
                            {
                                TileUtil.UpdateTile(groupName);
                                Logger.Debug("UpdateTile(groupName=\"{0}\") - finished".Fmt(groupName));
                            }
                            catch (Exception e)
                            {
                                Logger.Error("TileUtil.UpdateTile( groupName={0} ) failed\n".Fmt(groupName) + e.DumpStr());
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        Logger.Error(e.DumpStr());
                    }
                    finally
                    {
                        uiFinished.Set();
                    }
                });
                uiFinished.WaitOne();
            }
            catch (Exception e)
            {
                Logger.Error(e.DumpStr());
            }
            finally
            {
                Epilog(task);
            }
        }


    }
}

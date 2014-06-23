using System.Diagnostics;
using System.Windows;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO.IsolatedStorage;
using System.IO;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Log = ScheduledTaskAgent1.Logger;
using System.Collections.ObjectModel;


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
            Log.Error("ScheduledAgent.UnhandledException() \n{0}", e.ToString());
            Log.Error("ScheduledAgent.UnhandledException() \n{0}", e.ToString());
            if (Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                //Debugger.Break();
            }
        }

        const string m_tileImgPath = @"Shared\ShellContent\Tile.jpg";

        static void UpdateTileImage(BusInfo[] buses)
        {
            Log.Debug("");
            GenerateTileJpg("\n".Joyn(buses.Select(x => x.Name + " " + x.TimeToArrive)));
            ShellTile tile = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains("DefaultTitle=FromTile"));

            if (tile != null)
                tile.Update(new StandardTileData {
                    BackgroundImage = new Uri("isostore:/" + m_tileImgPath, UriKind.Absolute),
                    Title = DateTime.Now.ToString("HH:mm:ss"),
                });
        }

        public static void GenerateTileJpg(string msg)
        {
            Log.Debug("");
            var grid = new Grid()
            {
                Background = new SolidColorBrush(Colors.Orange),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Height = 336,
                Width = 336,
                Margin = new Thickness(0, 0, 0, 0),
            };

            var tbMsg = new TextBlock
            {
                Text = msg,
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 42,
                TextAlignment = TextAlignment.Left,
            };

            grid.Children.Add(tbMsg);

            grid.Measure(new Size(grid.Width, grid.Height));
            grid.Arrange(new Rect(new Point(0, 0), new Size(grid.Width, grid.Height)));
            grid.Width = 336;
            grid.Height = 336;

            try
            {
                using (var stream = IsolatedStorageFile.GetUserStoreForApplication().OpenFile(m_tileImgPath, FileMode.Create))
                {
                    WriteableBitmap bitmap = new WriteableBitmap(grid, null);
                    bitmap.Render(grid, null);
                    bitmap.Invalidate();
                    bitmap.SaveJpeg(stream, (int)grid.Width, (int)grid.Height, 0, 100);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.DumpStr());
                return;
            }
        }


        public static bool WifiConnected()
        {
            var profile = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();
            var interfaceType = profile.NetworkAdapter.IanaInterfaceType;
            // 71 is WiFi & 6 is Ethernet(LAN),   243 & 244 is 3G/Mobile
            return (interfaceType == 71 || interfaceType == 6);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="busList"></param>
        /// <returns> true means Network is OK. false means Network has problem</returns>
        public static async Task<bool> RefreshBusTime(BusInfo[] busList)
        {
            var tasks = busList.Select(b => BusTicker.GetBusDueTime(b)).ToList();
            var waIdx = Enumerable.Range(0, busList.Length).ToList();
            bool bNetworkIsOK = true;

            while (tasks.Count > 0)
            {
                int j = await Task.Run(() =>
                {
                    return Task.WaitAny(tasks.ToArray());
                });
                
                //Debug.WriteLine("Task.WaitAny() returns "+j);

                for (int i = tasks.Count - 1; i >= 0; --i)
                {
                    if (tasks.Count == 0)
                        break;
                    if (tasks[i].IsCompleted)
                    {
                        int fIdx = waIdx[i];
                        if (tasks[i].Status == TaskStatus.RanToCompletion)
                        {
                            busList[fIdx].TimeToArrive = tasks[i].Result;
                            Log.Debug("bus: {{ {0},{1},\"{2}\" }}".Fmt(busList[fIdx].Name, busList[fIdx].Station, busList[fIdx].TimeToArrive));
                        }
                        else
                        {
                            bNetworkIsOK = false;
                        }

                        waIdx.RemoveAt(i);
                        tasks.RemoveAt(i);
                    }
                }
            }
            return bNetworkIsOK;
        }
 
        protected override async void OnInvoke(ScheduledTask task)
        {
            try
            {
                Log.Create(false, "AgentLog.txt");
                ShellTile tile = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains("DefaultTitle=FromTile"));

                bool bWiFiOnly = false;
                IsolatedStorageSettings.ApplicationSettings.TryGetValue("WiFiOnly", out bWiFiOnly);
                if ( (bWiFiOnly && !WifiConnected())
                    || tile==null)
                {
                    ScheduledActionService.LaunchForTest(task.Name, TimeSpan.FromSeconds(1));
                    this.NotifyComplete();
                    return;
                }

                BusInfo[] buses= Database.FavBuses;

                Log.Debug("buses=" + buses.DumpArray());
                bool bNetworkIsOK = await RefreshBusTime(buses);

                Database.SaveFavBusGroups();

 
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        Log.Debug("UpdateTileImage()");
                        UpdateTileImage(buses);
                        Log.Debug("Deployment.Current.Dispatcher.BeginInvoke exit");
                    }
                    catch (Exception e)
                    {
                        Log.Error(e.ToString());
                    }
                    finally
                    {
                        Log.Debug("ScheduledActionService.LaunchForTest - start");
                        ScheduledActionService.LaunchForTest(task.Name, TimeSpan.FromSeconds(25));
                        Log.Debug("ScheduledActionService.LaunchForTest - finish");
                        Log.Close();
                        this.NotifyComplete();
                    }
                });
            }
            catch (Exception e)
            {
                Log.Error(e.DumpStr());
            }
            finally
            {
                Log.Debug("ScheduledActionService.LaunchForTest - start");
                ScheduledActionService.LaunchForTest(task.Name, TimeSpan.FromSeconds(25));
                Log.Debug("ScheduledActionService.LaunchForTest - finish");
                Log.Close();
                this.NotifyComplete();
            }
        }

    }
}

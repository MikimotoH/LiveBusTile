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
using System.Threading;


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
            Log.Error("ScheduledAgent.UnhandledException()\n e.ExceptionObject={0}\n\n e.Handled={1}"
                .Fmt(e.ExceptionObject.DumpStr(), e.Handled));
            if (Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                Debugger.Break();
            }
        }

        const string m_tileImgPath = @"Shared\ShellContent\Tile.jpg";


        public static void UpdateTileJpg(string uri = "/MainPage.xaml?DefaultTitle=FromTile")
        {
            Log.Debug("");
#if DEBUG
            foreach (var t in ShellTile.ActiveTiles)
            {
                Log.Debug("NavigationUri=" + t.NavigationUri.ToString());
            }
#endif
            ShellTile tile = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString() == uri);
            if (tile == null)
                return;

            GenerateTileJpg();
            var tileData = new StandardTileData
            {
                Title = DateTime.Now.ToString("HH:mm:ss"),
                BackgroundImage = new Uri("isostore:/" + m_tileImgPath, UriKind.Absolute),
            };
            tile.Update(tileData);
        }

        //public static void GenerateTileJpgFromUserControl()
        //{
        //    Log.Debug("");
        //    const int width = 336;
        //    const int height = 336;

        //    TileMedium ctrl = new TileMedium();
        //    EventWaitHandle uiEnded = new AutoResetEvent(false);
        //    Deployment.Current.Dispatcher.BeginInvoke(() =>
        //    {
        //        ctrl.ListBoxBuses.ItemsSource = Database.FavBuses.Select(x => new BusInfoVM(x)).ToObservableCollection();
        //        Log.Debug("ctrl = {{ ActualWidth={0},ActualHeight={1} }}".Fmt(ctrl.ActualWidth, ctrl.ActualHeight));
        //        ctrl.UpdateLayout();
        //        ctrl.Measure(new Size(width, height));
        //        ctrl.Arrange(new Rect(new Point(0, 0), new Size(width, height)));
        //        Log.Debug("ctrl = {{ ActualWidth={0},ActualHeight={1} }}".Fmt(ctrl.ActualWidth, ctrl.ActualHeight));
        //        uiEnded.Set();
        //    });
        //    uiEnded.WaitOne();


        //    try
        //    {
        //        using (var stream = IsolatedStorageFile.GetUserStoreForApplication().OpenFile(m_tileImgPath, FileMode.Create))
        //        {
        //            WriteableBitmap bitmap = new WriteableBitmap(ctrl, null);
        //            bitmap.Render(ctrl, null);
        //            bitmap.Invalidate();
        //            bitmap.SaveJpeg(stream, width, height, 0, 100);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex.DumpStr());
        //        return;
        //    }
        //}
        

        public static void GenerateTileJpg()
        {
            //string msg = "\n".Joyn(Database.FavBuses.Select(x => x.m_Name + " " + x.m_TimeToArrive));
            Log.Debug("");
            const int width = 336;
            const int height= 336;
            const int fontSize = 40;
            var grid = new Grid()
            {
                Width = width,
                Height = height,
                Background = new SolidColorBrush(Colors.Orange),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                //Margin = new Thickness(0, 0, 0, 0),
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(width - fontSize * 2.5) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(        fontSize * 2.5) });
            for (int iRow = 0; iRow < Database.FavBuses.Count(); ++iRow)
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(fontSize) });

            for (int iRow = 0; iRow < Database.FavBuses.Count(); ++iRow)
            {
                // Column 0
                var tbBusName = new TextBlock
                {
                    Text = Database.FavBuses[iRow].m_Name,
                    Foreground = new SolidColorBrush(Colors.White),
                    FontSize = fontSize,
                    TextAlignment = TextAlignment.Left,
                };
                Grid.SetRow(tbBusName, iRow);
                Grid.SetColumn(tbBusName, 0);

                // Column 1
                var tbTime = new TextBlock
                {
                    Text = Database.FavBuses[iRow].m_TimeToArrive,
                    Foreground = new SolidColorBrush(Colors.Red),
                    FontSize = fontSize,
                    TextAlignment = TextAlignment.Right,
                };
                Grid.SetRow(tbTime, iRow);
                Grid.SetColumn(tbTime, 1);

                grid.Children.Add(tbBusName);
                grid.Children.Add(tbTime);
            }
            Log.Debug("grid = {{ ActualWidth={0},ActualHeight={1} }}".Fmt(grid.ActualWidth, grid.ActualHeight));
            grid.UpdateLayout();
            grid.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            grid.Arrange(new Rect(new Point(0, 0), new Size(width, height)));
            Log.Debug("grid = {{ ActualWidth={0},ActualHeight={1} }}".Fmt(grid.ActualWidth, grid.ActualHeight));

            try
            {
                using (var stream = IsolatedStorageFile.GetUserStoreForApplication().OpenFile(m_tileImgPath, FileMode.Create))
                {
                    WriteableBitmap bitmap = new WriteableBitmap(grid, null);
                    bitmap.Render(grid, null);
                    bitmap.Invalidate();
                    bitmap.SaveJpeg(stream, width, height, 0, 100);
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

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="buses"></param>
        ///// <returns> true means Network is OK. false means Network has problem</returns>
        //public static async Task<bool> RefreshBusTime(BusInfo[] buses)
        //{
        //    var tasks = buses.Select(b => BusTicker.GetBusDueTime(b)).ToList();
        //    var waIdx = Enumerable.Range(0, buses.Length).ToList();
        //    bool bNetworkIsOK = true;

        //    while (tasks.Count > 0)
        //    {
        //        int j = await Task.Run(() =>
        //        {
        //            return Task.WaitAny(tasks.ToArray());
        //        });
                
        //        Debug.WriteLine("Task.WaitAny() returns "+j);

        //        for (int iRow = tasks.Count - 1; iRow >= 0; --iRow)
        //        {
        //            Log.Debug("iRow={0}, tasks.Count={1}".Fmt(iRow, tasks.Count));
        //            if (tasks.Count == 0)
        //                break;
        //            Log.Debug("tasks[iRow={0}].IsCompleted={1}".Fmt(iRow, tasks[iRow].IsCompleted));
        //            if (tasks[iRow].IsCompleted)
        //            {
        //                int fIdx = waIdx[iRow];
        //                Log.Debug("fIdx={0}, waIdx={1}".Fmt(fIdx, waIdx.DumpArray()));
        //                if (tasks[iRow].Status == TaskStatus.RanToCompletion)
        //                {
        //                    buses[fIdx].m_TimeToArrive = tasks[iRow].Result;
        //                    Log.Debug("bus: {{ {0},{1},\"{2}\" }}".Fmt(buses[fIdx].m_Name, buses[fIdx].m_Station, buses[fIdx].m_TimeToArrive));
        //                }
        //                else
        //                {
        //                    bNetworkIsOK = false;
        //                }

        //                waIdx.RemoveAt(iRow);
        //                tasks.RemoveAt(iRow);
        //            }
        //        }
        //    }
        //    return bNetworkIsOK;
        //}
 
        protected override void OnInvoke(ScheduledTask task)
        {
            try
            {
                Log.Create(FileMode.Append, "AgentLog.txt");
                Log.Msg("task="+task.DumpStr());

                bool bWiFiOnly = Convert.ToBoolean(Resource1.IsWiFiOnly_Default);
                IsolatedStorageSettings.ApplicationSettings.TryGetValue("WiFiOnly", out bWiFiOnly);
                if ( (bWiFiOnly && !WifiConnected())
                    || ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains("DefaultTitle=FromTile"))==null )
                {
#if DEBUG
                    ScheduledActionService.LaunchForTest(task.Name, TimeSpan.FromSeconds(60));
                    Log.Msg("LaunchForTest");
#endif
                    Log.Close();
                    this.NotifyComplete();
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

                if (numCompletedTask > 0)
                {
                    Database.SaveFavBusGroups();
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        try
                        {
                            UpdateTileJpg();
                            Log.Debug("UpdateTileJpg() - finished");
                        }
                        catch (Exception e)
                        {
                            Log.Error(e.ToString());
                        }
                        finally
                        {
#if DEBUG
                            ScheduledActionService.LaunchForTest(task.Name, TimeSpan.FromSeconds(60));
                            Log.Msg("LaunchForTest - finish");
#endif
                            Log.Close();
                            this.NotifyComplete();
                        }
                    });
                }


 

            }
            catch (Exception e)
            {
                Log.Error(e.DumpStr());
            }
            finally
            {
#if DEBUG
                ScheduledActionService.LaunchForTest(task.Name, TimeSpan.FromSeconds(60));
                Log.Msg("LaunchForTest - finish");
#endif
                Log.Close();
                this.NotifyComplete();
            }
        }

    }
}

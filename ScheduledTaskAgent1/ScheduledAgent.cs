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
using Newtonsoft.Json;


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

        static void UpdateTileImage(string lastUpdateTime)
        {
            ShellTile tile = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains("DefaultTitle=FromTile"));
            StandardTileData tileData = new StandardTileData
            {
                BackgroundImage = new Uri("isostore:/" + m_tileImgPath, UriKind.Absolute),
                Title = lastUpdateTime,
            };

            if (tile != null)
                tile.Update(tileData);
        }

        static void SaveTileJpg1(string stationName, string bus1, string dueTime1, string bus2, string dueTime2)
        {
            var grid = new Grid()
            {
                Background = new SolidColorBrush(Colors.Orange),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Height = 336, Width = 336,
                Margin = new Thickness(0, 0, 0, 0),
            };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });


            var tbStation = new TextBlock
            {
                Text = stationName,
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 40,
                TextAlignment = TextAlignment.Center,
            };
            var tbBusColumn = new TextBlock
            {
                Text = bus1+"\n"+bus2,
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 40,
                TextAlignment = TextAlignment.Left,
            };
            var tbDueTimeColumn = new TextBlock
            {
                Text = dueTime1 + "\n" + dueTime2,
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 40,
                TextAlignment = TextAlignment.Right,
            };

            grid.Children.Add(tbStation);
            grid.Children.Add(tbBusColumn);
            grid.Children.Add(tbDueTimeColumn);

            Grid.SetColumn(tbStation, 0);
            Grid.SetRow(tbStation, 0);
            Grid.SetColumnSpan(tbStation, 2);

            Grid.SetColumn(tbBusColumn, 0);
            Grid.SetRow(tbBusColumn, 1);
            Grid.SetColumn(tbDueTimeColumn, 1);
            Grid.SetRow(tbDueTimeColumn, 1);

            grid.Measure(new Size(grid.Width, grid.Height));
            grid.Arrange(new Rect(new Point(0,0), new Size(grid.Width, grid.Height)));

            using (var stream = IsolatedStorageFile.GetUserStoreForApplication().OpenFile(m_tileImgPath, FileMode.Create))
            {
                WriteableBitmap bitmap = new WriteableBitmap(grid, null);
                bitmap.Render(grid, null);
                bitmap.Invalidate();
                bitmap.SaveJpeg(stream, (int)grid.Width, (int)grid.Height, 0, 100);
            }
        }

        static void SaveTileJpg(string msg)
        {
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

            using (var stream = IsolatedStorageFile.GetUserStoreForApplication().OpenFile(m_tileImgPath, FileMode.Create))
            {
                WriteableBitmap bitmap = new WriteableBitmap(grid, null);
                bitmap.Render(grid, null);
                bitmap.Invalidate();
                bitmap.SaveJpeg(stream, (int)grid.Width, (int)grid.Height, 0, 100);
            }
        }


        public static bool WifiConnected()
        {
            var profile = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();
            var interfaceType = profile.NetworkAdapter.IanaInterfaceType;
            // 71 is WiFi & 6 is Ethernet(LAN)
            return (interfaceType == 71 || interfaceType == 6);
        }

        public static object m_busTagLock = new Object();
        public static List<BusTag> m_busTags;

        public static List<BusTag> GetBusTags()
        {
            lock (m_busTagLock)
            {
                if (m_busTags == null)
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.NullValueHandling = NullValueHandling.Ignore;
                    var sri = Application.GetResourceStream(new Uri("my_bustags.json", UriKind.Relative));
                    using (StreamReader sr = new StreamReader(sri.Stream))
                    using (JsonReader reader = new JsonTextReader(sr))
                    {
                        m_busTags = serializer.Deserialize(reader, typeof(List<BusTag>)) as List<BusTag>;
                    }
                }
                return m_busTags;
            }
        }

        protected override void OnInvoke(ScheduledTask task)
        {
            try
            {
                var busTags = GetBusTags();

                //Log.Create(false);
                Log.Debug("enter OnInvoke");

                Task<string>[] tasks = busTags.Select(bsd => BusTicker.GetBusDueTime(bsd.busName, bsd.station, bsd.dir)).ToArray();
                Log.Debug("WaitAll begin");
                Task.WaitAll(tasks);
                Log.Debug("WaitAll end");

                for (int i = 0; i < tasks.Length; ++i)
                    busTags[i].timeToArrive = tasks[i].Result;
                
                foreach (var bus in busTags)
                    Log.Debug(String.Format("{0} {1} {2} {3}", bus.busName, bus.station, bus.dir, bus.timeToArrive));                

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    Log.Debug("Deployment.Current.Dispatcher.BeginInvoke enter");
                    try
                    {   
                        ScheduledAgent.SaveTileJpg(String.Join("\n", busTags.Select(b => b.busName + " "+ b.timeToArrive)));
                        Log.Debug("SaveTileJpg()");
                        ScheduledAgent.UpdateTileImage(DateTime.Now.ToString("HH:mm:ss"));
                        Log.Debug("UpdateTileImage()");
                        ScheduledActionService.LaunchForTest(task.Name, TimeSpan.FromSeconds(30));
                        Log.Debug("Deployment.Current.Dispatcher.BeginInvoke exit");
                    }
                    catch (Exception e)
                    {
                        Log.Error(e.ToString());
                    }
                    finally
                    {
                        this.NotifyComplete();
                        //Log.Close();
                    }
                });
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }

    }
}

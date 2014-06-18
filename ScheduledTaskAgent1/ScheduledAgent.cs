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

        static void UpdateTileImage(BusTag[] busTags)
        {
            Log.Debug("");
            GenerateTileJpg("\n".Joyn(busTags.Select(x => x.busName + " " + x.timeToArrive)));
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


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        private void SaveBusTags(BusTag[] busTags)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;

            try
            {
                using (StreamWriter sw = new StreamWriter(
                    IsolatedStorageFile.GetUserStoreForApplication().OpenFile(@"Shared\ShellContent\saved_buses.json",
                    FileMode.OpenOrCreate, FileAccess.Write, FileShare.None)))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, busTags);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.DumpStr());
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public BusTag[] LoadBusTags()
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;

            if (!IsolatedStorageFile.GetUserStoreForApplication().FileExists((@"Shared\ShellContent\saved_buses.json")))
            {
                using (StreamReader sr = new StreamReader(Application.GetResourceStream(new Uri("Data/default_bustags.json", UriKind.Relative)).Stream))
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    return serializer.Deserialize(reader, typeof(BusTag[])) as BusTag[];
                }
            }

            using (StreamReader sr = new StreamReader(
                IsolatedStorageFile.GetUserStoreForApplication().OpenFile(@"Shared\ShellContent\saved_buses.json",
                FileMode.Open, FileAccess.Read, FileShare.Read)))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                return serializer.Deserialize(reader, typeof(BusTag[])) as BusTag[];
            }
        }


        protected override void OnInvoke(ScheduledTask task)
        {
            try
            {
                var busTags = LoadBusTags();
                busTags = (from bus in busTags orderby bus.tag select bus).ToArray();

                //Log.Create(false);
                Log.Debug("busTags=" + busTags.DumpArray());

                var tasks = busTags.Select(b => BusTicker.GetBusDueTime(b)).ToList();
                var waIdx = Enumerable.Range(0, busTags.Length).ToList();

                while (tasks.Count > 0)
                {
                    Task.WaitAny(tasks.ToArray());
                    for (int i = tasks.Count - 1; i >= 0; --i)
                    {
                        if (tasks.Count == 0)
                            break;
                        if (tasks[i].IsCompleted)
                        {
                            int fIdx = waIdx[i];
                            busTags[fIdx].timeToArrive = tasks[i].Result;
                            waIdx.RemoveAt(i);
                            tasks.RemoveAt(i);
                        }
                    }
                }

                SaveBusTags(busTags);
                
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    Log.Debug("Deployment.Current.Dispatcher.BeginInvoke enter");
                    try
                    {   
                        UpdateTileImage(busTags);
                        Log.Debug("UpdateTileImage()");
                        Log.Debug("Deployment.Current.Dispatcher.BeginInvoke exit");
                    }
                    catch (Exception e)
                    {
                        Log.Error(e.ToString());
                    }
                    finally
                    {
                        ScheduledActionService.LaunchForTest(task.Name, TimeSpan.FromSeconds(30));
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

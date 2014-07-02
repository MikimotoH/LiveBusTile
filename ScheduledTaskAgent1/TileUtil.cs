using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Log = ScheduledTaskAgent1.Logger;

namespace ScheduledTaskAgent1
{
    public class TileUtil
    {
        public const string m_TileFolder = @"Shared\ShellContent";

        public static string TileJpgPath(string groupName, bool isWide)
        {
            if (groupName=="")
                return @"{0}\{1}MainTile.jpg".Fmt(m_TileFolder, isWide ? "Wide" : "");
            else
                return @"{0}\{1}GroupTile={2}.jpg".Fmt(m_TileFolder, isWide ? "Wide" : "", groupName);
        }

        public static string TileUri(string groupName)
        {
            if (groupName=="")
                return "/MainPage.xaml?DefaultTitle=FromTile";
            else
                return "/GroupPage.xaml?GroupName=" + groupName;
        }
        static int TileWidth(bool isWide)
        {
            return isWide ? 691 : 336;
        }
        const int Tile_FontSize = 40;
        const int TileHeight = 336;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="groupName"> empty string means all groups in MainPage; otherwise it has to be a legal GroupName defined in Database.IsLegalGroupName()</param>
        /// <param name="isWide"></param>
        /// <returns> Uri(@"isostore:/Shared\ShellContent\GroupTile=回家.jpg", UriKind.Absolute) </returns>
        public static Uri GenerateTileJpg(string groupName, bool isWide)
        {
            Logger.Debug("");
            int width = TileWidth(isWide);
            int height = TileHeight;
            int fontSize = Tile_FontSize;
            var grid = new Grid()
            {
                Width = width,
                Height = height,
                Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xFD, 0xA9, 0x0F)),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };

            if (isWide)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(fontSize * 6) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(width - fontSize * 8.5) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(fontSize * 2.5) });
            }
            else
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(width - fontSize * 2.5) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(fontSize * 2.5) });
            }

            bool hasGroupName = groupName != "";
            BusInfo[] busList = hasGroupName
                ? Database.FavBusGroups.FirstOrDefault(x => x.m_GroupName == groupName).m_Buses.ToArray()
                : Database.FavBuses;

            if (hasGroupName)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(fontSize + 4) });
                var tbGroupName = new TextBlock
                {
                    Text = groupName,
                    Foreground = new SolidColorBrush(Colors.Black),
                    FontSize = fontSize + 4,
                    TextAlignment = TextAlignment.Center,
                };
                Grid.SetRow(tbGroupName, 0);
                Grid.SetColumn(tbGroupName, 0);
                Grid.SetColumnSpan(tbGroupName, 2 + isWide.ToInt());
                grid.Children.Add(tbGroupName);
            }


            for (int iRow = hasGroupName.ToInt(); iRow < hasGroupName.ToInt() + busList.Length; ++iRow)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(fontSize) });
                BusInfo bus = busList[iRow - hasGroupName.ToInt()];
                Logger.Debug("iRow={0}, bus={{ {1},{2},{3} }}".Fmt(iRow, bus.m_Name, bus.m_Station, bus.m_TimeToArrive));
                // Column 0
                var tbBusName = new TextBlock
                {
                    Text = bus.m_Name,
                    Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 15, 15, 100)),
                    FontSize = fontSize,
                    TextAlignment = TextAlignment.Left,
                };
                Grid.SetRow(tbBusName, iRow);
                Grid.SetColumn(tbBusName, 0);
                grid.Children.Add(tbBusName);

                // Column 1
                if (isWide)
                {
                    var tbStation = new TextBlock
                    {
                        Text = bus.m_Station,
                        Foreground = new SolidColorBrush(Colors.White),
                        FontSize = fontSize,
                        TextAlignment = TextAlignment.Right,
                        FontWeight = FontWeights.Bold,
                    };
                    Grid.SetRow(tbStation, iRow);
                    Grid.SetColumn(tbStation, 1);
                    grid.Children.Add(tbStation);
                }

                // Column 1 or 2
                var tbTime = new TextBlock
                {
                    Text = bus.m_TimeToArrive,
                    Foreground = new SolidColorBrush(Colors.Red),
                    FontSize = fontSize,
                    TextAlignment = TextAlignment.Right,
                };
                Grid.SetRow(tbTime, iRow);
                Grid.SetColumn(tbTime, 1 + isWide.ToInt());
                grid.Children.Add(tbTime);
            }
            
            grid.UpdateLayout();
            grid.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            grid.Arrange(new Rect(new Point(0, 0), new Size(width, height)));
            Logger.Debug("grid = {{ ActualWidth={0},ActualHeight={1} }}".Fmt(grid.ActualWidth, grid.ActualHeight));

            try
            {
                string jpgPath = TileJpgPath(groupName, isWide);
                Logger.Debug("jpgPath=" + jpgPath);
                using (var stream = IsolatedStorageFile.GetUserStoreForApplication()
                    .OpenFile(jpgPath, FileMode.Create))
                {
                    WriteableBitmap bitmap = new WriteableBitmap(grid, null);
                    bitmap.Render(grid, null);
                    bitmap.Invalidate();
                    bitmap.SaveJpeg(stream, width, height, 0, 100);
                }
                return new Uri("isostore:/" + jpgPath, UriKind.Absolute);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.DumpStr());
                return null;
            }
        }

        public static void UpdateTile(string groupName)
        {
            Logger.Debug("groupName=" + groupName);
            ShellTile tile = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString() == TileUri(groupName));
            if (tile == null)
                return;

            tile.Update(
                new FlipTileData
                {
                    Title = DateTime.Now.ToString("HH:mm:ss"),
                    BackgroundImage = GenerateTileJpg(groupName, false),
                    WideBackgroundImage = GenerateTileJpg(groupName, true),
                });
        }
    }
}

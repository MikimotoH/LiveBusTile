using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media;
using ScheduledTaskAgent1;
using Log = ScheduledTaskAgent1.Logger;

namespace LiveBusTile
{
    public partial class ListBusList : PhoneApplicationPage
    {
        public ListBusList()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            DataContext = new ExampleData();

        }



        private void BusItem_Delete_Click(object sender, RoutedEventArgs e)
        {
        }
        private void BusItem_Details_Click(object sender, RoutedEventArgs e)
        {
        }
               
        private void BusItem_Refresh_Click(object sender, RoutedEventArgs e)
        {
        }
        
        private void AppBar_Pin_Click(object sender, EventArgs e)
        {
            Log.Debug("");

        }
        private void AppBar_Refresh_Click(object sender, EventArgs e)
        {
            Log.Debug("");

        }
        private void AppBar_AddBus_Click(object sender, EventArgs e)
        {
            Log.Debug("");

        }
        private void AppBar_Settings_Click(object sender, EventArgs e)
        {
            Log.Debug("");

        }
        private void AppBar_About_Click(object sender, EventArgs e)
        {
            Log.Debug("");

        }

        private void ListNameMenuItem_Rename_Click(object sender, RoutedEventArgs e)
        {
            Log.Debug("");

        }

        private void lbBuses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.Debug("e.AddedItems.Count={0}, e.RemovedItems.Count={1}".Fmt(e.AddedItems.Count,e.RemovedItems.Count));

            /*
            //Place border round currently selected image
            ListBox lb = sender as ListBox;
            ListBoxItem lbi=null;
            if(e.AddedItems.Count>0)
                lbi = lb.ItemContainerGenerator.ContainerFromItem(e.AddedItems[0]) as ListBoxItem;
            else if(e.RemovedItems.Count>0)
                lbi = lb.ItemContainerGenerator.ContainerFromItem(e.RemovedItems[0]) as ListBoxItem;

            if (lbi.BorderThickness.Left == 0)
            {
                lbi.BorderThickness = new Thickness(4, 4, 4, 4);
                lbi.BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x30, 0x30));
            }
            else
            {
                lbi.BorderThickness = new Thickness(0, 0, 0, 0);
                lbi.BorderBrush = new SolidColorBrush(Color.FromArgb(0x00, 0xFF, 0x30, 0x30));
            }
            */
        }

        private void lbBuses_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Log.Debug("e.OriginalSource=" + e.OriginalSource.GetType());

            ListBox lb = sender as ListBox;
            if (lb.SelectedItem == null)
                return;
            ListBoxItem lbi = lb.ItemContainerGenerator.ContainerFromItem(lb.SelectedItem) as ListBoxItem;
            Log.Debug("lbi.Content={0}".Fmt((lbi.Content as BusInfo)));
            if (lbi.BorderThickness.Left == 0)
            {
                lbi.BorderThickness = new Thickness(4, 4, 4, 4);
                lbi.BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x30, 0x30));
            }
            else
            {
                lbi.BorderThickness = new Thickness(0, 0, 0, 0);
                lbi.BorderBrush = new SolidColorBrush(Color.FromArgb(0x00, 0xFF, 0x30, 0x30));
                lb.SelectedItem = null;
            }
        }

        private void grdBusRow_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Log.Debug("");
            Grid grid = sender as Grid;
            Log.Debug("grid.DataContext=" + grid.DataContext);
        }


    }
}
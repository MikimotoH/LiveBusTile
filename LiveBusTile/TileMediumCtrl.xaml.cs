﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using ScheduledTaskAgent1;
using System.Collections.ObjectModel;
using System.Collections;

namespace LiveBusTile
{
    public partial class TileMediumCtrl : UserControl
    {
        public TileMediumCtrl()
        {
            InitializeComponent();
        }

        public IEnumerable ItemsSource { get { return lbBuses.ItemsSource; } set { lbBuses.ItemsSource = value; } }
    }
}

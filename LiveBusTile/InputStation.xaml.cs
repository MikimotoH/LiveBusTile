using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace LiveBusTile
{
    public partial class InputStation : PhoneApplicationPage
    {
        public InputStation()
        {
            InitializeComponent();
            this.Loaded += InputStation_Loaded;
        }

        void InputStation_Loaded(object sender, RoutedEventArgs e)
        {
            //throw new NotImplementedException();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using LiveBusTile.Resources;

namespace LiveBusTile
{
    public partial class About : PhoneApplicationPage
    {
        public About()
        {
            InitializeComponent();
        }

        private void github_Click(object sender, RoutedEventArgs e)
        {
            var wbt = new WebBrowserTask { Uri = new Uri(http_github.Text, UriKind.Absolute) };
            wbt.Show();
        }

        private void hyperReportBug_Click(object sender, RoutedEventArgs e)
        {
            var wbt = new WebBrowserTask { Uri = new Uri(httpReportBug.Text, UriKind.Absolute) };
            wbt.Show();
        }

        private void Email_Click(object sender, RoutedEventArgs e)
        {
            EmailComposeTask emailComposeTask = new EmailComposeTask();

            emailComposeTask.Subject = AppResources.ApplicationTitle + "-問題回報：";
            emailComposeTask.Body = "症狀 ：\n\n重現方式：\n　　　步驟1：\n　　　步驟2：\n　　　步驟3：";
            emailComposeTask.To = EMail.Text;
            emailComposeTask.Show();
        }
    }
}
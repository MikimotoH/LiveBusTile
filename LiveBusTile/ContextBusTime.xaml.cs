using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Collections.ObjectModel;
using System.ComponentModel;
using ScheduledTaskAgent1;
using HtmlAgilityPack;
using LiveBusTile.Resources;
using System.IO.IsolatedStorage;

namespace LiveBusTile
{
    public partial class ContextBusTime : PhoneApplicationPage
    {
        public ContextBusTime()
        {
            InitializeComponent();
        }

        string m_BusName;
        BusDir m_Dir;
        string m_Station;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            m_BusName = NavigationContext.QueryString["BusName"];
            tbBusName.Text = m_BusName;
            m_Dir = (BusDir)Enum.Parse(typeof(BusDir), NavigationContext.QueryString.GetValue("Dir", "go"));
            m_Station = NavigationContext.QueryString.GetValue("Station", "");
            lbStationsBack.ItemsSource = null;
            lbStationsGo.ItemsSource = null;
            lbStationsBack.UpdateLayout();
            lbStationsGo.UpdateLayout();

            InitList();
        }

        const string m_BusInfoUrl = @"http://pda.5284.com.tw/MQS/businfo2.jsp?routename={0}";

        StationTimeVM GetStationTime(BusDir dir, string station)
        {
            if (dir == BusDir.go)
                return lbStationsGo.Items.Cast<StationTimeVM>().FirstOrDefault(x => x.Station == station);
            else
                return lbStationsBack.Items.Cast<StationTimeVM>().FirstOrDefault(x => x.Station == station);
        }

        async void InitList()
        {
            progbar.Visibility = Visibility.Visible;
            ApplicationBar.Buttons.DoForEach<ApplicationBarIconButton>(x => x.IsEnabled = false);
            ApplicationBar.IsMenuEnabled = false;

            HtmlDocument doc = new HtmlDocument();
            try
            {
                HttpClient client = new HttpClient();
                string strHtml = await client.GetStringAsync(new Uri(m_BusInfoUrl.Fmt(Uri.EscapeUriString(tbBusName.Text))));
                doc.LoadHtml(strHtml);
            }
            catch (Exception ex)
            {
                ProgBarEpilog();
                AppLogger.Error(ex.DumpStr());
                MessageBox.Show(AppResources.NetworkFault);
                return;
            }

            HtmlNodeCollection 
                goNodes = doc.DocumentNode.SelectNodes("/html/body/center/table/tr[5]/td/table/tr[2]/td[1]/table");
            //                                         "/html/body/center/table/tr[5]/td/table/tr/td/table/tr[1]/td[1]"
            if (goNodes.IsNullOrEmpty())
                goNodes = doc.DocumentNode.SelectNodes("/html/body/center/table/tr[5]/td/table/tr/td/table");
            if (!goNodes.IsNullOrEmpty())
            {
                ObservableCollection<StationTimeVM> goStatVM = new ObservableCollection<StationTimeVM>();
                for (int i = 1; i < goNodes[0].ChildNodes.Count; i += 2)
                    goStatVM.Add(new StationTimeVM(goNodes[0].ChildNodes[i].ChildNodes[0].InnerText,
                        goNodes[0].ChildNodes[i].ChildNodes[1].InnerText));
                lbStationsGo.ItemsSource = goStatVM;
                lbStationsGo.UpdateLayout();
            }

            HtmlNodeCollection backNodes = doc.DocumentNode.SelectNodes("/html/body/center/table/tr[5]/td/table/tr[2]/td[2]/table");
            if (!backNodes.IsNullOrEmpty())
            {
                ObservableCollection<StationTimeVM> backStatVM = new ObservableCollection<StationTimeVM>();
                for (int i = 1; i < backNodes[0].ChildNodes.Count; i += 2)
                    backStatVM.Add(new StationTimeVM(backNodes[0].ChildNodes[i].ChildNodes[0].InnerText,
                        backNodes[0].ChildNodes[i].ChildNodes[1].InnerText));
                lbStationsBack.ItemsSource = backStatVM;
                lbStationsBack.UpdateLayout();
            }

            if (m_Dir == BusDir.back)
            {
                pivot.SelectedItem = pivotItemBack;
                if (m_Station != "")
                {
                    var it = lbStationsBack.Items.Cast<StationTimeVM>().FirstOrDefault(x => x.Station == m_Station);
                    if (it != null)
                        lbStationsBack.ScrollIntoView(it);
                }
            }
            else
            {
                pivot.SelectedItem = pivotItemGo;
                if (m_Station != "")
                {
                    var it = lbStationsGo.Items.Cast<StationTimeVM>().FirstOrDefault(x => x.Station == m_Station);
                    if (it != null)
                        lbStationsGo.ScrollIntoView(it);
                }
            }

            if (lbStationsBack.Items.IsNullOrEmpty())
                pivot.Items.Remove(pivotItemBack);
            else
                pivotItemBack.Header = "往：" + (lbStationsBack.Items.LastElement() as StationTimeVM).Station;
            

            if (lbStationsGo.Items.IsNullOrEmpty())
                pivot.Items.Remove(pivotItemGo);
            else
                pivotItemGo.Header = "往：" + (lbStationsGo.Items.LastElement() as StationTimeVM).Station;

            UpdateDatabase();
            tbLastUpdatedTime.Text = Database.LastUpdatedTime.ToString(TileUtil.CurSysTimeFormat);
            ProgBarEpilog();
        }

        private void UpdateDatabase()
        {
            Database.FavBuses.Where(b => b.m_Name == m_BusName).DoForEach(
                b => b.m_TimeToArrive = GetStationTime(b.m_Dir, b.m_Station).TimeToArrive
                );
            Database.SaveFavBusGroups();
        }

        
        void ProgBarEpilog()
        {
            ApplicationBar.Buttons.DoForEach<ApplicationBarIconButton>(x => x.IsEnabled = true);
            ApplicationBar.IsMenuEnabled = true;
            progbar.Visibility = Visibility.Collapsed;
        }

        private async void AppBar_Refresh_Click(object sender, EventArgs e)
        {
            progbar.Visibility = Visibility.Visible;
            ApplicationBar.Buttons.DoForEach<ApplicationBarIconButton>(x => x.IsEnabled = false);
            ApplicationBar.IsMenuEnabled = false;

            
            HtmlDocument doc = new HtmlDocument();
            try
            {
                HttpClient client = new HttpClient();
                string strHtml = await client.GetStringAsync(new Uri(m_BusInfoUrl.Fmt(Uri.EscapeUriString(tbBusName.Text))));
                doc.LoadHtml(strHtml);
            }
            catch (Exception ex)
            {
                ProgBarEpilog();
                AppLogger.Error(ex.DumpStr());
                MessageBox.Show(AppResources.NetworkFault);
                return;
            }

            HtmlNodeCollection goNodes = doc.DocumentNode.SelectNodes("/html/body/center/table/tr[5]/td/table/tr[2]/td[1]/table");
            if (goNodes.Count > 0)
            {
                for (int i = 1; i < goNodes[0].ChildNodes.Count; i += 2)
                {
                    string st = goNodes[0].ChildNodes[i].ChildNodes[0].InnerText;
                    string tm = goNodes[0].ChildNodes[i].ChildNodes[1].InnerText;
                    lbStationsGo.Items.Cast<StationTimeVM>().FirstOrDefault(x => x.Station == st).TimeToArrive = tm;
                }
            }

            HtmlNodeCollection backNodes = doc.DocumentNode.SelectNodes("/html/body/center/table/tr[5]/td/table/tr[2]/td[2]/table");
            if (backNodes.Count > 0)
            {
                for (int i = 1; i < backNodes[0].ChildNodes.Count; i += 2)
                {
                    string st = backNodes[0].ChildNodes[i].ChildNodes[0].InnerText;
                    string tm = backNodes[0].ChildNodes[i].ChildNodes[1].InnerText;
                    lbStationsBack.Items.Cast<StationTimeVM>().FirstOrDefault(x => x.Station == st).TimeToArrive = tm;
                }
            }
            UpdateDatabase();
            tbLastUpdatedTime.Text = Database.LastUpdatedTime.ToString(TileUtil.CurSysTimeFormat);
            ProgBarEpilog();
        }

    }

    public class StationTime
    {
        public string m_Station;
        public string m_TimeToArrive;

        public override string ToString()
        {
            return m_Station + " " + m_TimeToArrive;
        }
    }

    public class StationTimeVM : INotifyPropertyChanged
    {
        StationTime m_base = new StationTime();
        public StationTime Base { get { return m_base; } }

        public StationTimeVM() { }
        public StationTimeVM(string st, string tm) { this.Station = st; this.TimeToArrive = tm; }
        public StationTimeVM(StationTime st) { m_base = st; }

        public string Station { 
            get { return m_base.m_Station; } 
            set { if (m_base.m_Station != value) { m_base.m_Station = value; NotifyPropertyChanged("Station"); } } 
        }
        public string TimeToArrive { 
            get { return m_base.m_TimeToArrive; } 
            set { if (m_base.m_TimeToArrive != value) { m_base.m_TimeToArrive = value; NotifyPropertyChanged("TimeToArrive"); } } 
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            if (null != PropertyChanged)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString() { return m_base.ToString(); }
    }

    public class ExampleContextBusTimeGo : List<StationTimeVM>
    {
        public ExampleContextBusTimeGo()
        {
            Add(new StationTimeVM("碧湖社區", "6分"));
            Add(new StationTimeVM("雙和醫院", "6分"));
            Add(new StationTimeVM("力行里", "6分"));
            Add(new StationTimeVM("圓通路口", "6分"));
            Add(new StationTimeVM("板南路口", "6分"));
            Add(new StationTimeVM("游氏宗祠", "6分"));
            Add(new StationTimeVM("地政事務所", "6分"));
            Add(new StationTimeVM("南山路口", "6分"));
            Add(new StationTimeVM("捷運景安站", "6分"));
            Add(new StationTimeVM("景新街口", "6分"));
            Add(new StationTimeVM("安和路口", "6分"));
            Add(new StationTimeVM("華泰新城","未發車"));
            Add(new StationTimeVM("捷運永安市場站","進站中"));
            Add(new StationTimeVM("永安市場","將到站"));
            Add(new StationTimeVM("八二三紀念公園","4分"));
            Add(new StationTimeVM("得和路一","4分"));
            Add(new StationTimeVM("得和路","5分"));
            Add(new StationTimeVM("秀朗國小","6分"));
            Add(new StationTimeVM("民生路","7分"));
            Add(new StationTimeVM("自立路","8分"));
            Add(new StationTimeVM("秀山國小","9分"));
            Add(new StationTimeVM("秀山站","10分"));
        }
    }

    public class ExampleContextBusTimeBack : List<StationTimeVM>
    {
        public ExampleContextBusTimeBack()
        {
            Add(new StationTimeVM("自立路","12分"));
            Add(new StationTimeVM("民生路","13分"));
            Add(new StationTimeVM("秀朗國小","15分"));
            Add(new StationTimeVM("得和路","16分"));
            Add(new StationTimeVM("得和路一","17分"));
            Add(new StationTimeVM("八二三紀念公園","18分"));
            Add(new StationTimeVM("永安市場","進站中"));
            Add(new StationTimeVM("捷運永安市場站","將到站"));
            Add(new StationTimeVM("華泰新城","4分"));
            Add(new StationTimeVM("安和路口","5分"));
            Add(new StationTimeVM("景新街口","7分"));
            Add(new StationTimeVM("捷運景安站","7分"));
            Add(new StationTimeVM("南山路口","10分"));
            Add(new StationTimeVM("地政事務所","12分"));
            Add(new StationTimeVM("游氏宗祠","進站中"));
            Add(new StationTimeVM("板南路口","將到站"));
            Add(new StationTimeVM("力行里","5分"));
            Add(new StationTimeVM("雙和醫院","5分"));
            Add(new StationTimeVM("碧湖社區","14分"));
        }
    }

}
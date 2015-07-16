using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace BusStationCrawler
{
    partial class Program
    {
        static void test_Orange2XiuShangSchool2()
        {
            Regex.IsMatch(@"<td\ .+class=\""ttestop\"".*?>(.*)<", "<td class=\"ttestop\" aaa=bbb>", RegexOptions.Multiline);
        }
    }

}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace LiveBusTile
{
    class IntegerCompare : IComparer<int>
    {
        public int Compare(int x, int y)
        {
            return x - y;
        }
    }
    
    class StrNumComparer : IComparer<string>
    {
        public int Compare(string strL, string strR)
        {
            var digits = "0123456789".ToArray();
            int idxL = strL.IndexOfAny(digits);
            int idxR = strR.IndexOfAny(digits);
            if (idxL == -1 || idxR == -1 || idxL != idxR)
                return StringComparer.InvariantCulture.Compare(strL, strR);
            int numL = atoi(strL.Substring(idxL));
            int numR = atoi(strR.Substring(idxR));
            return numL - numR;
        }
        static int atoi(string s)
        {
            int n = 0;
            foreach (char c in s)
            {
                if (!Char.IsDigit(c))
                    break;
                n = n*10 + (int)(c - '0');
            }
            return n;
        }
    }

    public class KeyedBusVM : ObservableCollection<string>
    {
        public string Key { get; set; }
        public KeyedBusVM(){}
        public KeyedBusVM(IGrouping<string,string> group)
            : base(group.OrderBy(x => x, new StrNumComparer()))
        {            
            this.Key = group.Key;
        }

        public KeyedBusVM(string key, IEnumerable<string> buses)
            : base(Enumerable.OrderBy<string, string>(buses, x => x))
        {
            this.Key = key;
        }
    }
}

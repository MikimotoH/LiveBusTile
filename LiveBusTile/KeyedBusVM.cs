using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace LiveBusTile
{
    public class KeyedBusVM : ObservableCollection<string>
    {
        public string Key { get; set; }
        public KeyedBusVM(){}
        public KeyedBusVM(IGrouping<string,string> group)
            : base(Enumerable.OrderBy<string,string>(group, x=>x))
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

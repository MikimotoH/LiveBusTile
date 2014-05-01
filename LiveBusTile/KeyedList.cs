using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveBusTile
{
    public class KeyedList<TKey, TItem> : List<TItem>
    {
        public TKey Key {protected set;  get;}

        public KeyedList(TKey key, IEnumerable<TItem> items)
            :base(items)
        {
            this.Key = key;
        }
        public KeyedList(IGrouping<TKey, TItem> grouping)
            :base(grouping)
        {
            this.Key = grouping.Key;
        }
    }
}

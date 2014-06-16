﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace ScheduledTaskAgent1
{
    public static class ExtensionMethods
    {
        public static string DumpStr(this NavigationEventArgs e)
        {
            return String.Format("{{ Uri={0}, NavigationMode={1}, IsNavigationInitiator={2}, e.Content={3} }}",
                e.Uri, e.NavigationMode, e.IsNavigationInitiator, e.Content);
        }

        public static TValue GetValue<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue)
        {
            if (!dict.ContainsKey(key))
                return defaultValue;
            return dict[key];
        }

        public static string DumpStr<TKey, TValue>(this IDictionary<TKey, TValue> dict)
        {
            return "{" + String.Join(",", dict.Select(kv => kv.Key + "=" + kv.Value)) + "}";
        }

        public static String Fmt(this String fmt, params object[] args)
        {
            return String.Format(fmt, args);
        }

        public static String Joyn(this String separator, IEnumerable<object> values)
        {
            return String.Join(separator, values);
        }
    }
}

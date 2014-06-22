using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace ScheduledTaskAgent1
{
    public static class ExtensionMethods
    {
        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> arr)
        {
            return new ObservableCollection<T>(arr);
        }
        public static bool IsNone(this String s)
        {
            return String.IsNullOrEmpty(s);
        }
        public static string DumpStr(this NavigationEventArgs e)
        {
            return String.Format("{{ Uri={0}, NavigationMode={1}, IsNavigationInitiator={2}, e.Content={3} }}",
                e.Uri, e.NavigationMode, e.IsNavigationInitiator, e.Content);
        }
        public static string DumpStr(this Exception ex)
        {
            return String.Format("{{Msg=\"{0}\",StackTrace=\"{1}\"}}", ex.Message, ex.StackTrace);
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

        public static string DumpArray<T>(this IEnumerable<T> arr) 
        {
            return "[" + arr.Count() + "]{" + String.Join(", ", arr.Select(x => x.ToString())) + "}"; 
        }

        public static String Fmt(this String fmt, params object[] args)
        {
            return String.Format(fmt, args);
        }
        public static T LastElement<T>(this T[] arr)
        {
            return arr[arr.Length - 1];
        }

        public static String Joyn(this String separator, IEnumerable<object> values)
        {
            return String.Join(separator, values);
        }
    }
}

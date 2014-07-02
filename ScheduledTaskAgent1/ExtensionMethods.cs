﻿using Microsoft.Phone.Scheduler;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduledTaskAgent1
{
    public static class ExtensionMethods
    {

        public static void DoForEach<T>( this IEnumerable<T> ls, Action<T> act)
        {
            foreach (T x in ls)
            {
                act(x);
            }
        }

        public static void DoForEach<T>(this IList ls, Action<T> act)
        {
            foreach (T x in ls)
            {
                act(x);
            }
        }

        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> arr)
        {
            return new ObservableCollection<T>(arr);
        }
        public static bool IsNone(this String s)
        {
            return String.IsNullOrEmpty(s);
        }

        public static string DumpStr(this Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{{Type={0}, Msg=\"{1}\"\n StackTrace=\n  {2}}}", ex.GetType(), ex.Message, ex.StackTrace);

            int innerLoop = 1;
            Exception jx = ex;
            while ((jx = jx.InnerException) != null)
            {
                sb.AppendFormat("[{0}] InnerException: {{Type={1}, Msg=\"{2}\"\n StackTrace=\n  {3}}}", innerLoop, jx.GetType(), jx.Message, jx.StackTrace);
                innerLoop++;
            }

            return sb.ToString();
        }

        public static TValue GetValue<TValue>(this IDictionary<string, object> dict, string key, TValue defValue)
        {
            if (!dict.ContainsKey(key))
                return defValue;
            return (TValue)dict[key];
        }

        public static string GetValue(this IDictionary<string,string> dict, string keyName, string defValue)
        {
            if (!dict.ContainsKey(keyName))
                return defValue;
            return dict[keyName];
        }

        public static string DumpStr(this ScheduledTask task)
        {
            return String.Format(
                "{{Name={0}, LastScheduledTime={1}, task.ExpirationTime={2}, task.LastExitReason={3}, task.IsScheduled={4}, GetType()={5} }}",
                task.Name, task.LastScheduledTime.ToString(Logger.timeFmt), task.ExpirationTime.ToString(Logger.timeFmt), task.LastExitReason, 
                task.IsScheduled, task.GetType());
        }

        public static string DumpStr<TKey, TValue>(this IDictionary<TKey, TValue> dict)
        {
            return "{" + String.Join(",", dict.Select(kv => kv.Key + "=" + kv.Value)) + "}";
        }

        public static string DumpArray<T>(this IEnumerable<T> arr) 
        {
            return "[" + arr.Count() + "]{" + String.Join(", ", arr.Select(x => x.ToString())) + "}"; 
        }

        public static string DumpArray<T>(this IEnumerable<T> arr, Func<T, string> func)
        {
            return "[" + arr.Count() + "]{" + String.Join(", ", arr.Select(x=>func(x))) + "}";
        }

        public static String Fmt(this String fmt, params object[] args)
        {
            return String.Format(fmt, args);
        }
        public static T LastElement<T>(this T[] arr)
        {
            return arr[arr.Length - 1];
        }

        public static T FirstElement<T>(this IEnumerable<T> en)
        {
            return en.ElementAt(0);
        }
        public static T LastElement<T>(this IEnumerable<T> en)
        {
            return en.ElementAt( en.Count() - 1);
        }
        public static int ToInt(this bool b)
        {
            return b ? 1 : 0;
        }
        public static bool ToBool(this int i)
        {
            return i != 0;
        }

        public static String Joyn(this String separator, IEnumerable<object> values)
        {
            return String.Join(separator, values);
        }

        public static String Joyn(this String separator, params object[] values)
        {
            return String.Join(separator, values);
        }
    }
}

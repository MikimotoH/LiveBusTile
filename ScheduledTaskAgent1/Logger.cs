using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.CompilerServices;

namespace ScheduledTaskAgent1
{
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Warn = 2,
        Error = 3,
    };

    public static class Logger
    {
        //static StreamWriter m_stm;
        //const string logdir = @"Shared\ShellContent";
        //const string logpath = logdir + @"\log.txt";

        //public static void Create(bool overwrite = true,
        //    [CallerFilePath] string path = "",
        //    [CallerMemberName] string func = "",
        //    [CallerLineNumber] int line = 0            
        //    )
        //{
            //System.Diagnostics.Debug.WriteLine("{0}<Debug>{1}:{2}:{3} [{4}] Logger.Create(overwrite={5}) enter m_stm={6}",
            //    DateTime.Now.ToString(timeFmt), Path.GetFileName(path), func, line, System.Threading.Thread.CurrentThread.ManagedThreadId
            //    , overwrite, m_stm);

            //try
            //{
            //    Directory.CreateDirectory(logdir);
            //}
            //catch (Exception e)
            //{
            //    System.Diagnostics.Debug.WriteLine("CreateDirectory(\"{0}\") failed, e={1}", logdir, e.Message);
            //    return;
            //}
            // m_stm = new StreamWriter(
            //     IsolatedStorageFile.GetUserStoreForApplication().OpenFile(logpath,
            //     overwrite ? FileMode.Create : FileMode.Append,
            //     FileAccess.Write, FileShare.ReadWrite));
            //Console.SetOut(m_stm);

            //System.Diagnostics.Debug.WriteLine("{0}<Debug>{1}:{2}:{3} [{4}] Logger.Create(overwrite={5}) exit m_stm={6}",
            //    DateTime.Now.ToString(timeFmt), Path.GetFileName(path), func, line, System.Threading.Thread.CurrentThread.ManagedThreadId
            //    , overwrite, m_stm);
        //}

        //public static void Flush
        //    (
        //    [CallerFilePath] string path = "",
        //    [CallerMemberName] string func = "",
        //    [CallerLineNumber] int line = 0
        //    )
        //{
        //    System.Diagnostics.Debug.WriteLine("{0}<Debug>{1}:{2}:{3} [{4}] Logger.Flush() enter m_stm={5}",
        //        DateTime.Now.ToString(timeFmt), Path.GetFileName(path), func, line, System.Threading.Thread.CurrentThread.ManagedThreadId
        //        , m_stm);
        //    m_stm.Flush();
        //}

        

        static void Log(LogLevel logLevel, string func, string path, int line, string msg)
        {
            string msg1 = String.Format("{0}<{1}>{2}:{3}:{4} [{5}] {6}", DateTime.Now.ToString("yyMMdd_HH:mm:ss.fff"),
                logLevel.ToString(), Path.GetFileName(path), func, line, System.Threading.Thread.CurrentThread.ManagedThreadId, 
                msg);
            System.Diagnostics.Debug.WriteLine(msg1);
            //if (m_stm != null)
            //    m_stm.WriteLine(msg1);
            //else
            //{
            //    System.Diagnostics.Debug.WriteLine("{0}<Error>{1}:{2}:{3} [{4}] m_stm == NULL!",
            //        DateTime.Now.ToString(timeFmt), Path.GetFileName(path), func, line, System.Threading.Thread.CurrentThread.ManagedThreadId);
            //    Debugger.Break();
            //}
        }

        //public delegate void LogFunc(string msg, string func, string path, int line);

        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public static void Error(string msg, 
            [CallerMemberName] string func="",
            [CallerFilePath] string path="",
            [CallerLineNumber] int line=0)
        {
            Log(LogLevel.Error, func, path, line, msg);
        }

        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public static void Debug(string msg,
            [CallerMemberName] string func = "",
            [CallerFilePath] string path = "",
            [CallerLineNumber] int line = 0)
        {
            Log(LogLevel.Debug, func, path, line, msg);
        }

        public static void LogRawMsg(string msg)
        {
            System.Diagnostics.Debug.WriteLine(msg);
            //if (m_stm != null)
            //m_stm.WriteLine(msg);
        }


        //public static void Close(
        //    [CallerFilePath] string path = "",
        //    [CallerMemberName] string func = "",
        //    [CallerLineNumber] int line = 0
        //    )
        //{
        //    System.Diagnostics.Debug.WriteLine("{0}<Debug>{1}:{2}:{3} [{4}] Logger.Close() enter",
        //        DateTime.Now.ToString(timeFmt), Path.GetFileName(path), func, line, System.Threading.Thread.CurrentThread.ManagedThreadId);
        //    m_stm.Close();
        //    m_stm = null;
        //    System.Diagnostics.Debug.WriteLine("{0}<Debug>{1}:{2}:{3} [{4}] Logger.Close() exit",
        //        DateTime.Now.ToString(timeFmt), Path.GetFileName(path), func, line, System.Threading.Thread.CurrentThread.ManagedThreadId);
        //}
    }

    public static class ForEachExtensions
    {
        public static void ForEachIndex<T>(this IEnumerable<T> enumerable, Action<T, int> handler)
        {
            int idx = 0;
            foreach (T item in enumerable)
                handler(item, idx++);
        }

        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            foreach (T item in enumeration)
                action(item);
        }
    }

}


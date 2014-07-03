using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.CompilerServices;
using System.Text;

namespace ScheduledTaskAgent1
{
    public enum LogLevel
    {
        Debug = 1,
        Warn = 2,
        Error = 3,
        Msg = 4,
    };

    public static class Logger
    {
        static Object m_lock = new Object();

        const string logFileName = "AgentLog.txt";
        public const string timeFmt = "yyMMdd_HH:mm:ss.fff";
        const string logdir = @"Shared\ShellContent";

        static void Log(LogLevel logLevel, string func, string path, int line, string msg)
        {
            string time = DateTime.Now.ToString("yyMMdd_HH:mm:ss.fff");
            string msg1 = "{0}<{1}>{2}:{3}:{4} [{5}] {6}".Fmt(time,
                logLevel.ToString(), Path.GetFileName(path), func, line, System.Threading.Thread.CurrentThread.ManagedThreadId,
                msg);
            System.Diagnostics.Debug.WriteLine(msg1);

            //Directory.CreateDirectory(logdir);

            lock (m_lock)
            {
                try
                {
                    using (IsolatedStorageFileStream stm = IsolatedStorageFile.GetUserStoreForApplication().OpenFile(logdir + "\\" + logFileName,
                            FileMode.Append, FileAccess.Write, FileShare.Read))
                    using (StreamWriter sw = new StreamWriter(stm))
                    {

                        sw.WriteLine(msg1);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Logger.Log(): OpenFile() failed,  {0}<{1}>{2}:{3}:{4} [{5}] msg=\"{6}\" ex={7}",
                        time, logLevel, Path.GetFileName(path), func, line, System.Threading.Thread.CurrentThread.ManagedThreadId,
                        msg, ex.DumpStr());
                }
            }
        }

        public static void Error(string msg, 
            [CallerMemberName] string func="",
            [CallerFilePath] string path="",
            [CallerLineNumber] int line=0)
        {
            Log(LogLevel.Error, func, path, line, msg);
        }

        public static void Msg(string msg,
            [CallerMemberName] string func = "",
            [CallerFilePath] string path = "",
            [CallerLineNumber] int line = 0)
        {
            Log(LogLevel.Msg, func, path, line, msg);
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

        //public static void LogRawMsg(string msg)
        //{
        //    System.Diagnostics.Debug.WriteLine(msg);
        //}
        //public static void Close(
        //    [CallerFilePath] string path = "",
        //    [CallerMemberName] string func = "",
        //    [CallerLineNumber] int line = 0
        //    )
        //{
        //    System.Diagnostics.Debug.WriteLine("{0}<Debug>{1}:{2}:{3} [{4}] Logger.Close() enter",
        //        DateTime.Now.ToString(timeFmt), Path.GetFileName(path), func, line, System.Threading.Thread.CurrentThread.ManagedThreadId);

        //    lock (m_lock)
        //    {
        //        if (stm != null)
        //        {
        //            stm.Close();
        //            stm = null;
        //        }
        //    }
        //}

    }
}


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.CompilerServices;
using ScheduledTaskAgent1;
using System.Windows.Navigation;
using System.Linq;

namespace LiveBusTile
{
    public enum LogLevel
    {
        Debug = 1,
        Warn = 2,
        Error = 3,
        Msg = 4,
    };

    public static class AppLogger
    {
        static Object m_lock = new Object();

        const string logFileName = "Log.txt";
        public const string timeFmt = "yyMMdd_HH:mm:ss.fff";
        const string logdir = @"Shared\ShellContent";

        static void Log(LogLevel logLevel, string func, string path, int line, string msg)
        {
            string time = DateTime.Now.ToString("yyMMdd_HH:mm:ss.fff");
            string msg1 = "{0}<{1}>{2}:{3}:{4} [{5}] {6}".Fmt(time,
                logLevel.ToString(), Path.GetFileName(path), func, line, System.Threading.Thread.CurrentThread.ManagedThreadId,
                msg);
            System.Diagnostics.Debug.WriteLine(msg1);

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
    }

    public static class Util
    {
        public static void DeleteFileSafely(string filePath)
        {
            try
            {
                AppLogger.Debug("DeleteFile(\"{0}\")".Fmt(filePath));
                IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(filePath);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Failed to DeleteFile(\"{0}\")\n ex={1}".Fmt(filePath, ex.DumpStr()));
            }
        }

        public static string DumpStr(this NavigationEventArgs e)
        {
            return String.Format("{{ Uri={0}, NavigationMode={1}, IsNavigationInitiator={2}, e.Content={3} }}",
                e.Uri, e.NavigationMode, e.IsNavigationInitiator, e.Content);
        }

    }

}


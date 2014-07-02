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
        Debug = 1,
        Warn = 2,
        Error = 3,
        Msg = 4,
    };

    public static class Logger
    {
        static Object m_lock = new Object();        
        static StreamWriter m_stm;

        const string logFileName = "AgentLog.txt";
        public const string timeFmt = "yyMMdd_HH:mm:ss.fff";
        const string logdir = @"Shared\ShellContent";

        static void Log(LogLevel logLevel, string func, string path, int line, string msg)
        {
            string msg1 = "{0}<{1}>{2}:{3}:{4} [{5}] {6}".Fmt(DateTime.Now.ToString("yyMMdd_HH:mm:ss.fff"),
                logLevel.ToString(), Path.GetFileName(path), func, line, System.Threading.Thread.CurrentThread.ManagedThreadId,
                msg);
            System.Diagnostics.Debug.WriteLine(msg1);

            lock (m_lock)
            {
                if (m_stm == null)
                {
                    try
                    {
                        Directory.CreateDirectory(logdir);
                        m_stm = new StreamWriter(
                            IsolatedStorageFile.GetUserStoreForApplication().OpenFile(logdir + "\\" + logFileName,
                            FileMode.Append,
                            FileAccess.Write, FileShare.Read));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Logger.Log(): OpenFile() failed, ex=" + ex.DumpStr());
                    }
                }

                if (m_stm != null)
                {
                    try
                    {
                        m_stm.WriteLine(msg1);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Logger.Log(): m_stm.WriteLine(msg1) failed, ex=" + ex.DumpStr());
                    }
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

        public static void LogRawMsg(string msg)
        {
            System.Diagnostics.Debug.WriteLine(msg);
            lock (m_lock)
            {
                if (m_stm != null)
                {
                    try
                    {
                        m_stm.WriteLine(msg);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Logger.Log(): m_stm.WriteLine(msg) failed, ex=" + ex.DumpStr());
                    }
                }
            }
        }

        public static void Flush
            (
            [CallerFilePath] string path = "",
            [CallerMemberName] string func = "",
            [CallerLineNumber] int line = 0
            )
        {
            System.Diagnostics.Debug.WriteLine("{0}<Debug>{1}:{2}:{3} [{4}] Logger.Flush() enter",
                DateTime.Now.ToString(timeFmt), Path.GetFileName(path), func, line, System.Threading.Thread.CurrentThread.ManagedThreadId);

            lock (m_lock)
            {
                if (m_stm != null)
                {
                    try
                    {
                        m_stm.Flush();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Logger.Flush(): m_stm.Flush() failed");
                        System.Diagnostics.Debug.WriteLine("ex=" + ex.DumpStr());
                    }
                }
            }
        }

        public static void Close(
            [CallerFilePath] string path = "",
            [CallerMemberName] string func = "",
            [CallerLineNumber] int line = 0
            )
        {
            System.Diagnostics.Debug.WriteLine("{0}<Debug>{1}:{2}:{3} [{4}] Logger.Close() enter",
                DateTime.Now.ToString(timeFmt), Path.GetFileName(path), func, line, System.Threading.Thread.CurrentThread.ManagedThreadId);

            lock (m_lock)
            {
                if (m_stm != null)
                {
                    m_stm.Close();
                    m_stm = null;
                }
            }
        }

    }
}


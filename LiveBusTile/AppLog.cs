﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.CompilerServices;
using ScheduledTaskAgent1;
using System.Windows.Navigation;

namespace LiveBusTile
{
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Warn = 2,
        Error = 3,
        Msg = 4,
    };

    public class AppLog : IDisposable
    {
        StreamWriter m_stm;
        const string timeFmt = "yyMMdd_HH:mm:ss.fff";

        public AppLog()
        {
            System.Diagnostics.Debug.WriteLine("Logger.Logger() ctor");
        }
        const string logdir = @"Shared\ShellContent";
        string logFileName;

        public void Create(FileMode fileMode, string logFileName,
            [CallerFilePath] string path = "",
            [CallerMemberName] string func = "",
            [CallerLineNumber] int line = 0            
            )
        {
            this.logFileName = logFileName;
            System.Diagnostics.Debug.WriteLine("{0}<Debug>{1}:{2}:{3} [{4}] Logger.Create(overwrite={5}) enter m_stm={6}",
                DateTime.Now.ToString(timeFmt), Path.GetFileName(path), func, line, System.Threading.Thread.CurrentThread.ManagedThreadId
                , fileMode, m_stm);

            try
            {
                Directory.CreateDirectory(logdir);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("CreateDirectory(\"{0}\") failed, e={1}", logdir, e.Message);
                return;
            }
            m_stm = new StreamWriter(
                IsolatedStorageFile.GetUserStoreForApplication().OpenFile(logdir+"\\"+logFileName,
                fileMode,
                FileAccess.Write, FileShare.Read));
            //Console.SetOut(m_stm);

            System.Diagnostics.Debug.WriteLine("{0}<Debug>{1}:{2}:{3} [{4}] Logger.Create(overwrite={5}) exit m_stm={6}",
                DateTime.Now.ToString(timeFmt), Path.GetFileName(path), func, line, System.Threading.Thread.CurrentThread.ManagedThreadId
                , fileMode, m_stm);
        }

        public void Flush
            (
            [CallerFilePath] string path = "",
            [CallerMemberName] string func = "",
            [CallerLineNumber] int line = 0
            )
        {
            if (m_stm != null)
            {
                System.Diagnostics.Debug.WriteLine("{0}<Debug>{1}:{2}:{3} [{4}] Logger.Flush() enter m_stm={5}",
                    DateTime.Now.ToString(timeFmt), Path.GetFileName(path), func, line, System.Threading.Thread.CurrentThread.ManagedThreadId
                    , m_stm);
                m_stm.Close();
                m_stm = new StreamWriter(IsolatedStorageFile.GetUserStoreForApplication().OpenFile(
                    logdir+"\\"+logFileName,
                    FileMode.Append, FileAccess.Write, FileShare.Read));
            }
        }


        void Log(LogLevel logLevel, string func, string path, int line, string msg)
        {
            string msg1 = String.Format("{0}<{1}>{2}:{3}:{4} [{5}] {6}", DateTime.Now.ToString("yyMMdd_HH:mm:ss.fff"),
                logLevel.ToString(), Path.GetFileName(path), func, line, System.Threading.Thread.CurrentThread.ManagedThreadId, 
                msg);
            System.Diagnostics.Debug.WriteLine(msg1);
            Console.Out.WriteLine(msg1);
            if (m_stm != null)
                m_stm.WriteLine(msg1);
            else
            {
                //System.Diagnostics.Debug.WriteLine("{0}<Error>{1}:{2}:{3} [{4}] m_stm == NULL!",
                //    DateTime.Now.ToString(timeFmt), Path.GetFileName(path), func, line, System.Threading.Thread.CurrentThread.ManagedThreadId);
                //Debugger.Break();
            }
        }

        //public delegate void LogFunc(string msg, string func, string path, int line);

        public void Error(string msg, 
            [CallerMemberName] string func="",
            [CallerFilePath] string path="",
            [CallerLineNumber] int line=0)
        {
            Log(LogLevel.Error, func, path, line, msg);
        }

        public void Msg(string msg,
            [CallerMemberName] string func = "",
            [CallerFilePath] string path = "",
            [CallerLineNumber] int line = 0)
        {
            Log(LogLevel.Msg, func, path, line, msg);
        }

        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public void Debug(string msg,
            [CallerMemberName] string func = "",
            [CallerFilePath] string path = "",
            [CallerLineNumber] int line = 0)
        {
            Log(LogLevel.Debug, func, path, line, msg);
        }

        public void LogRawMsg(string msg)
        {
            System.Diagnostics.Debug.WriteLine(msg);
            if (m_stm != null)
                m_stm.WriteLine(msg);
        }


        public void Close(
            [CallerFilePath] string path = "",
            [CallerMemberName] string func = "",
            [CallerLineNumber] int line = 0
            )
        {
            if(m_stm !=null)
            {
                System.Diagnostics.Debug.WriteLine("{0}<Debug>{1}:{2}:{3} [{4}] Logger.Close() enter",
                    DateTime.Now.ToString(timeFmt), Path.GetFileName(path), func, line, System.Threading.Thread.CurrentThread.ManagedThreadId);
                m_stm.Close();
                m_stm = null;
                System.Diagnostics.Debug.WriteLine("{0}<Debug>{1}:{2}:{3} [{4}] Logger.Close() exit",
                    DateTime.Now.ToString(timeFmt), Path.GetFileName(path), func, line, System.Threading.Thread.CurrentThread.ManagedThreadId);
            }
        }

        public void Dispose()
        {
            Close();
        }
    }

    public static class Util
    {
        public static void DeleteFileSafely(string filePath)
        {
            try
            {
                App.m_AppLog.Debug("DeleteFile(\"{0}\")".Fmt(filePath));
                IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(filePath);
            }
            catch (Exception ex)
            {
                App.m_AppLog.Error("Failed to DeleteFile(\"{0}\")\n ex={1}".Fmt(filePath, ex.DumpStr()));
            }
        }

        public static string DumpStr(this NavigationEventArgs e)
        {
            return String.Format("{{ Uri={0}, NavigationMode={1}, IsNavigationInitiator={2}, e.Content={3} }}",
                e.Uri, e.NavigationMode, e.IsNavigationInitiator, e.Content);
        }

    }

}


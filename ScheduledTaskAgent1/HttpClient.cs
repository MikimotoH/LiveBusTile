/****************************** Module Header ******************************\
* Module Name:    HttpClient.cs
* Project:        CSWP8AwaitWebClient
* Copyright (c) Microsoft Corporation
*
* This demo shows how to make an await WebClient
* (similar to HttpClient in Windows 8).
* 
* This source is subject to the Microsoft Public License.
* See http://www.microsoft.com/en-us/openness/licenses.aspx#MPL.
* All other rights reserved.
* 
* THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
* EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED 
* WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
\*****************************************************************************/

using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;


namespace ScheduledTaskAgent1
{
    public class HttpClient : WebClient
    {
        public HttpClient()
            : base()
        {
        }

        /// <summary>
        /// Get the string by URI string.
        /// </summary>
        /// <param name="strUri">The Uri the request is sent to.</param>
        /// <param name="timeOut">time Out in milliseconds</param>
        /// <returns>string</returns>
        public async Task<string> GetStringAsync(string strUri, int timeOut = 0)
        {
            CancellationTokenSource cancelTok = new CancellationTokenSource();
            if (timeOut != 0)
                cancelTok.CancelAfter(timeOut);
            
            Uri uri = new Uri(strUri);
            Task<string> task = Task.Run( () => this.GetStringAsync(uri), cancelTok.Token);
            string result = await task;
            return result;
        }

        public string GetStringBlocked(string strUri)
        {
            Uri uri = new Uri(strUri);
            base.DownloadStringCompleted += HttpClient_DownloadStringCompleted;
            base.DownloadStringAsync(uri);
            Debug.WriteLine("{0} this._downloadCompletedEvent.Wait() start", DateTime.Now.ToString("HH:mm:ss.fff"));
            this._downloadCompletedEvent.Wait(-1);
            Debug.WriteLine("{0} this._downloadCompletedEvent.Wait() end", DateTime.Now.ToString("HH:mm:ss.fff"));
            return this._ResultString;
        }

        ManualResetEventSlim _downloadCompletedEvent = new ManualResetEventSlim(false);
        String _ResultString;
        void HttpClient_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            this._ResultString = e.Result;
            _downloadCompletedEvent.Set();
        }

        /// <summary>
        /// Get the string by URI.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <returns>string</returns>
        public Task<string> GetStringAsync(Uri requestUri)
        {
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();

            try
            {
                this.DownloadStringCompleted += (s, e) =>
                {
                    if (e.Error == null)
                    {
                        tcs.TrySetResult(e.Result);
                    }
                    else
                    {
                        tcs.TrySetException(e.Error);
                    }
                };

                this.DownloadStringAsync(requestUri);

            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            if (tcs.Task.Exception != null)
            {
                throw tcs.Task.Exception;             
            }

            return tcs.Task;
        }
    }
}

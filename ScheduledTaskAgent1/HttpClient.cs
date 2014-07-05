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
            if (Headers == null)
                Headers = new WebHeaderCollection();
            Headers["Cache-Control"] = "max-age=0"; 
            Headers["Pragma"] = "no-cache";
        }


        public string GetStringSync(Uri requestUri)
        {
            Task<string> task = GetStringAsync(requestUri);
            task.Wait();
            return task.Result;
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

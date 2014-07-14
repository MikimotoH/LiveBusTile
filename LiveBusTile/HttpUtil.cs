using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LiveBusTile
{
    public static class HttpUtil
    {
        public static Task<string> DownloadStringAsync(string url)
        {
            HttpWebRequest m_req;
            m_req = HttpWebRequest.Create(new Uri(url)) as HttpWebRequest;
            if (m_req.Headers == null)
                m_req.Headers = new WebHeaderCollection();
            m_req.Headers[HttpRequestHeader.IfModifiedSince] = DateTime.UtcNow.ToString("R");
            m_req.Headers["Cache-Control"] = "no-cache";
            m_req.Headers["Pragma"] = "no-cache";


            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();

            try
            {
                m_req.BeginGetResponse(new AsyncCallback((callbackResult) =>
                {
                    HttpWebRequest myRequest = (HttpWebRequest)callbackResult.AsyncState;

                    try
                    {
                        HttpWebResponse myResponse = (HttpWebResponse)myRequest.EndGetResponse(callbackResult);
                        using (StreamReader httpwebStreamReader = new StreamReader(myResponse.GetResponseStream()))
                        {
                            string results = httpwebStreamReader.ReadToEnd();
                            tcs.TrySetResult(results);
                        }
                        myResponse.Close();
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }

                }), m_req);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            if (tcs.Task.Exception != null)
                throw tcs.Task.Exception;

            return tcs.Task;

        }

    }
}

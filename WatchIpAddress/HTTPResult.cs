using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WatchIpAddress
{
    public enum HTTPRequestStatus
    {
        OK = 0x00,
        Timeout = 0x01,
        Exception = 0xff,
        Error = 0xff
    }

    public class HTTPResult
    {
        public HTTPRequestStatus Status;
        public object Value;
        public string ExceptionMessage;
    }

    public class HTTPRequestParameters
    {
        public string Url;
        public string Method = "GET";
        public string Accept = "";
        public string ContentType = "application/json";
        public string[] Headers = null;
        public string Body = null;
        public int Timeout = 20000;
    }

    public sealed partial class HttpRequest
    {
        private static volatile HttpRequest instance;
        private static object syncRoot = new Object();

        private HttpRequest() { }

        public static HttpRequest Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new HttpRequest();
                    }
                }
                return instance;
            }
        }
    }

    public sealed partial class HttpRequest
    {
        public HTTPResult Query(HTTPRequestParameters parameters)
        {
            HttpWebRequest httpWebRequest;

            try
            {
                httpWebRequest = (HttpWebRequest)WebRequest.Create(parameters.Url);
                httpWebRequest.Method = parameters.Method;
                httpWebRequest.ContentType = parameters.ContentType;
                httpWebRequest.Accept = parameters.Accept;
                if (parameters.Headers != null)
                    for (int i = 0; i < parameters.Headers.Length; i++)
                        httpWebRequest.Headers.Add(parameters.Headers[i]);

                httpWebRequest.Timeout = parameters.Timeout;
                httpWebRequest.Proxy = WebRequest.GetSystemWebProxy();
                if (!string.IsNullOrWhiteSpace(parameters.Body))
                {
                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        streamWriter.Write(parameters.Body);
                        streamWriter.Flush();
                        streamWriter.Close();
                    }
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    return new HTTPResult() { Status = HTTPRequestStatus.OK, Value = streamReader.ReadToEnd().Trim() };
                }
            }
            catch (Exception ex)
            {
                try
                {
                    if (((WebException)ex).Status == WebExceptionStatus.Timeout)
                        return new HTTPResult() { Status = HTTPRequestStatus.Timeout, Value = null };
                }
                catch (Exception)
                { }
                return new HTTPResult() { Status = HTTPRequestStatus.Error, Value = null };
            }
        }
    }

}

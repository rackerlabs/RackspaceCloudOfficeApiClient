using System;
using System.Dynamic;
using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace Rackspace.CloudOffice
{
    public class ApiException : Exception
    {
        public dynamic Response { get; private set; }

        public ApiException(WebException ex) : base(GetErrorMessage(ex), ex)
        {
            var responseStream = ex.Response?.GetResponseStream();
            if (responseStream == null) return;
            Response = new StreamReader(responseStream).ReadToEnd();
            try
            {
                Response = JsonConvert.DeserializeObject<ExpandoObject>(Response);
            }
            catch
            {
            }
        }

        static string GetErrorMessage(WebException ex)
        {
            var r = ex.Response as HttpWebResponse;
            return r == null
                ? ex.Message
                : $"{r.StatusCode:d} - {r.Headers["x-error-message"]}";
        }
    }
}

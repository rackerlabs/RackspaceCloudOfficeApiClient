using System;
using System.Dynamic;
using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace Rackspace.CloudOffice
{
    public class ApiException : Exception
    {
        public HttpStatusCode? HttpCode { get; private set; }
        public dynamic Response { get; private set; }
        public Uri RequestUri { get; private set; }

        public ApiException(WebException ex) : base(GetErrorMessage(ex), ex)
        {
            Response = ParseResponse(ex.Response);

            var webResponse = ex.Response as HttpWebResponse;
            if (webResponse != null)
                HttpCode = webResponse.StatusCode;
        }

        internal ApiException(HttpWebRequest request, WebException ex) : this(ex)
        {
            RequestUri = request.RequestUri;
        }

        static object ParseResponse(WebResponse response)
        {
            if (response == null)
                return null;

            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                var raw = reader.ReadToEnd();
                try
                {
                    return JsonConvert.DeserializeObject<ExpandoObject>(raw);
                }
                catch
                {
                    return raw;
                }
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

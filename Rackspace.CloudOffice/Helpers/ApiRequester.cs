using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Rackspace.CloudOffice.Helpers
{
    internal class ApiRequester : IApiRequester
    {
        public Task<WebResponse> Send(ApiRequest request)
        {
            var webRequest = BuildHttpWebRequest(request);
            if (request.ShouldSendBody)
                SendRequestBody(webRequest, request.Body, request.ContentType);

            return ReadResponse(webRequest);
        }

        static HttpWebRequest BuildHttpWebRequest(ApiRequest request)
        {
            var result = (HttpWebRequest)WebRequest.Create(request.Url);
            result.Method = request.Method;
            result.Accept = ApiClient.ContentType.Json;
            result.UserAgent = "https://github.com/rackerlabs/RackspaceCloudOfficeApiClient";
            foreach (var customHeader in request.Headers)
                result.Headers.Add(customHeader.Key, customHeader.Value);

            SignRequest(result, request.UserKey, request.SecretKey);

            return result;
        }

        static void SendRequestBody(HttpWebRequest request, object data, string contentType)
        {
            request.ContentType = contentType;

            using (var stream = request.GetRequestStream())
            using (var writer = new StreamWriter(request.GetRequestStream(), Encoding.ASCII))
            {
                writer.Write(BodyEncoder.Encode(data, contentType));
            }
        }

        static async Task<WebResponse> ReadResponse(HttpWebRequest request)
        {
            try
            {
                return await request.GetResponseAsync().ConfigureAwait(false);
            }
            catch (WebException ex)
            {
                throw new ApiException(request, ex);
            }
        }

        static void SignRequest(HttpWebRequest request, string userKey, string secretKey)
        {
            var dateTime = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            Trace.WriteLine(string.Format("{0:HH:mm:ss.fff}: {1} {2}",
                dateTime, request.Method, request.RequestUri.AbsoluteUri));

            request.Headers["X-Api-Signature"] = string.Format("{0}:{1}:{2}",
                userKey,
                dateTime,
                ComputeSha1(userKey + request.UserAgent + dateTime + secretKey));
        }

        static string ComputeSha1(string data)
        {
            var sha1 = System.Security.Cryptography.SHA1.Create();
            var hashed = sha1.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hashed);
        }
    }
}

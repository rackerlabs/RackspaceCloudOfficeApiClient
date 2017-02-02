using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rackspace.CloudOffice.Helpers;

namespace Rackspace.CloudOffice
{
    public class ApiClient : IApiClient
    {
        public const string DefaultBaseUrl = "https://api.emailsrvr.com";

        public string BaseUrl { get; private set; }
        public string UserKey { get; private set; }
        public IDictionary<string, string> CustomHeaders { get; } = new Dictionary<string, string>();

        readonly string _secretKey;
        readonly Throttler _throttler = new Throttler
        {
            ThreshholdCount = 30,
            WindowSize = TimeSpan.FromSeconds(1),
        };

        public ApiClient(string userKey, string secretKey, string baseUrl=DefaultBaseUrl)
        {
            UserKey = userKey;
            _secretKey = secretKey;
            BaseUrl = baseUrl;
        }

        public ApiClient(string configFilePath=null)
        {
            var config = new ConfigParser(configFilePath);
            UserKey = config.UserKey;
            _secretKey = config.SecretKey;
            BaseUrl = config.BaseUrl;
        }

        public async Task<dynamic> Get(string path)
        {
            return await Get<ExpandoObject>(path).ConfigureAwait(false);
        }

        public async Task<T> Get<T>(string path)
        {
            var request = await CreateJsonRequest("GET", path).ConfigureAwait(false);
            return await ReadResponse<T>(request).ConfigureAwait(false);
        }

        public async Task<IEnumerable<dynamic>> GetAll(string path, string pagedProperty, int pageSize = 50)
        {
            return await GetAll<ExpandoObject>(path, pagedProperty, pageSize).ConfigureAwait(false);
        }

        public async Task<IEnumerable<dynamic>> GetAll(string path, PagingPropertyNames propertyNames, int pageSize = 50)
        {
            return await GetAll<ExpandoObject>(path, propertyNames, pageSize).ConfigureAwait(false);
        }

        public async Task<IEnumerable<T>> GetAll<T>(string path, string pagedProperty, int pageSize = 50)
        {
            var propertyNames = PagingPropertyNames.Default;
            propertyNames.ItemsName = pagedProperty;
            return await GetAll<T>(path, propertyNames, pageSize).ConfigureAwait(false);
        }

        public async Task<IEnumerable<T>> GetAll<T>(string path, PagingPropertyNames propertyNames, int pageSize = 50)
        {
            var result = new List<T>();

            var offset = 0;
            IDictionary<string, object> page;
            do
            {
                page = await Get<IDictionary<string, object>>(JoinPathWithQueryString(path, new Dictionary<string, string>
                {
                    { propertyNames.OffsetName, offset.ToString() },
                    { propertyNames.PageSizeName, pageSize.ToString() },
                })).ConfigureAwait(false);

                var items = ConvertToEnumerable<T>(page.GetCaseInensitive(propertyNames.ItemsName));
                result.AddRange(items);

                offset += pageSize;
            } while (offset < Convert.ToInt32(page.GetCaseInensitive(propertyNames.TotalName)));

            return result;
        }

        public async Task<dynamic> Post(string path, object data, string contentType=ContentType.UrlEncoded)
        {
            return await Post<ExpandoObject>(path, data, contentType).ConfigureAwait(false);
        }

        public async Task<T> Post<T>(string path, object data, string contentType=ContentType.UrlEncoded)
        {
            var request = await CreateJsonRequest("POST", path).ConfigureAwait(false);
            SendRequestBody(request, data, contentType);
            return await ReadResponse<T>(request).ConfigureAwait(false);
        }

        public async Task<dynamic> Put(string path, object data, string contentType=ContentType.UrlEncoded)
        {
            return await Put<ExpandoObject>(path, data, contentType).ConfigureAwait(false);
        }

        public async Task<T> Put<T>(string path, object data, string contentType=ContentType.UrlEncoded)
        {
            var request = await CreateJsonRequest("PUT", path).ConfigureAwait(false);
            SendRequestBody(request, data, contentType);
            return await ReadResponse<T>(request).ConfigureAwait(false);
        }

        public async Task<dynamic> Patch(string path, object data, string contentType=ContentType.UrlEncoded)
        {
            return await Patch<ExpandoObject>(path, data, contentType).ConfigureAwait(false);
        }

        public async Task<T> Patch<T>(string path, object data, string contentType=ContentType.UrlEncoded)
        {
            var request = await CreateJsonRequest("PATCH", path).ConfigureAwait(false);
            SendRequestBody(request, data, contentType);
            return await ReadResponse<T>(request).ConfigureAwait(false);
        }

        public async Task Delete(string path)
        {
            var request = await CreateJsonRequest("DELETE", path).ConfigureAwait(false);
            using (var response = await GetResponseBody(request).ConfigureAwait(false))
            {
            }
        }

        async Task<HttpWebRequest> CreateJsonRequest(string method, string path)
        {
            await _throttler.Throttle().ConfigureAwait(false);

            var request = BuildRequest(method, path, ContentType.Json);
            Trace.WriteLine(string.Format("{0:HH:mm:ss.fff}: {1} {2}",
                DateTime.Now, request.Method, request.RequestUri.AbsoluteUri));
            return request;
        }

        HttpWebRequest BuildRequest(string method, string path, string acceptType)
        {
            var request = (HttpWebRequest)WebRequest.Create(BaseUrl + path);
            request.Method = method;
            request.Accept = acceptType;
            request.UserAgent = "https://github.com/rackerlabs/RackspaceCloudOfficeApiClient";
            foreach (var customHeader in CustomHeaders)
            {
                request.Headers.Add(customHeader.Key, customHeader.Value);
            }

            SignRequest(request);

            return request;
        }

        void SignRequest(HttpWebRequest request)
        {
            var dateTime = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            request.Headers["X-Api-Signature"] = string.Format("{0}:{1}:{2}",
                UserKey,
                dateTime,
                ComputeSha1(UserKey + request.UserAgent + dateTime + _secretKey));
        }

        static string ComputeSha1(string data)
        {
            var sha1 = System.Security.Cryptography.SHA1.Create();
            var hashed = sha1.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hashed);
        }

        static void SendRequestBody(HttpWebRequest request, object data, string contentType)
        {
            request.ContentType = contentType;

            using (var writer = new StreamWriter(request.GetRequestStream(), Encoding.ASCII))
                writer.Write(BodyEncoder.Encode(data, contentType));
        }

        static async Task<T> ReadResponse<T>(HttpWebRequest request)
        {
            using (var r = await GetResponseBody(request))
                return ParseJsonStream<T>(r.GetResponseStream());
        }

        static async Task<WebResponse> GetResponseBody(HttpWebRequest request)
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

        static T ParseJsonStream<T>(Stream s)
        {
            return JsonConvert.DeserializeObject<T>(
                new StreamReader(s).ReadToEnd());
        }

        static string JoinPathWithQueryString(string path, IEnumerable<KeyValuePair<string, string>> queryStringParams)
        {
            var queryStringParts = queryStringParams.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}");
            var joiner = path.Contains("?") ? "&" : "?";
            return path + joiner + string.Join("&", queryStringParts);
        }

        static IEnumerable<T> ConvertToEnumerable<T>(object collection)
        {
            foreach (var item in (IEnumerable<object>)collection)
                yield return item is T
                    ? (T)item
                    : JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(item));
        }

        public static class ContentType
        {
            public const string UrlEncoded = "application/x-www-form-urlencoded";
            public const string Json = "application/json";
        }
    }
}

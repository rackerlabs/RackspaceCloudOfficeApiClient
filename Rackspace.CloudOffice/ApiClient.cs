using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;

namespace Rackspace.CloudOffice
{
    public class ApiClient : IApiClient
    {
        public const string DefaultBaseUrl = "https://api.emailsrvr.com";

        public string BaseUrl { get; private set; }
        public string UserKey { get; private set; }
        public IDictionary<string,string> CustomHeaders { get; private set; } 

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
            CustomHeaders = new Dictionary<string, string>();
        }

        public ApiClient(string configFilePath=null)
        {
            if (string.IsNullOrEmpty(configFilePath))
                configFilePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RsCloudOfficeApi.config");

            var config = new XmlDocument();
            config.Load(configFilePath);

            UserKey = ReadNode(config, "/config/userKey");
            _secretKey = ReadNode(config, "/config/secretKey");
            BaseUrl = ReadNode(config, "/config/baseUrl", DefaultBaseUrl);
            CustomHeaders = new Dictionary<string, string>();
        }

        public async Task<dynamic> Get(string path)
        {
            return await Get<ExpandoObject>(path);
        }

        public async Task<T> Get<T>(string path)
        {
            using (var r = await GetResponse(await CreateJsonRequest("GET", path)))
                return ParseJsonStream<T>(r.GetResponseStream());
        }

        public async Task<IEnumerable<dynamic>> GetAll(string path, string pagedProperty, int pageSize = 50)
        {
            return await GetAll<ExpandoObject>(path, pagedProperty, pageSize);
        }

        public async Task<IEnumerable<T>> GetAll<T>(string path, string pagedProperty, int pageSize = 50)
        {
            var result = new List<T>();

            var offset = 0;
            dynamic page;
            do
            {
                var queryString = string.Format("offset={0}&size={1}", offset, pageSize);
                page = await Get(JoinPathWithQueryString(path, queryString));

                result.AddRange(GetEnumerableProperty<T>(page, pagedProperty));

                offset += pageSize;
            } while (offset < page.total);

            return result;
        }

        public async Task<dynamic> Post(string path, object data, string contentType=ContentType.UrlEncoded)
        {
            return await Post<ExpandoObject>(path, data, contentType);
        }

        public async Task<T> Post<T>(string path, object data, string contentType=ContentType.UrlEncoded)
        {
            var request = await CreateJsonRequest("POST", path);
            SendRequestBody(request, data, contentType);

            using (var r = await GetResponse(request))
                return ParseJsonStream<T>(r.GetResponseStream());
        }

        public async Task<dynamic> Put(string path, object data, string contentType=ContentType.UrlEncoded)
        {
            return await Put<ExpandoObject>(path, data, contentType);
        }

        public async Task<T> Put<T>(string path, object data, string contentType=ContentType.UrlEncoded)
        {
            var request = await CreateJsonRequest("PUT", path);
            SendRequestBody(request, data, contentType);

            using (var r = await GetResponse(request))
                return ParseJsonStream<T>(r.GetResponseStream());
        }

        public async Task Delete(string path)
        {
            var r = await GetResponse(await CreateJsonRequest("DELETE", path));
            r.Dispose();
        }

        async Task<HttpWebRequest> CreateJsonRequest(string method, string path)
        {
            await _throttler.Throttle();

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

        static async Task<WebResponse> GetResponse(HttpWebRequest request)
        {
            try
            {
                return await request.GetResponseAsync().ConfigureAwait(false);
            }
            catch (WebException ex)
            {
                throw new ApiException(ex);
            }
        }

        static T ParseJsonStream<T>(Stream s)
        {
            return JsonConvert.DeserializeObject<T>(
                new StreamReader(s).ReadToEnd());
        }

        static string JoinPathWithQueryString(string path, string queryString)
        {
            var joiner = path.Contains("?") ? "&" : "?";
            return path + joiner + queryString;
        }

        static IEnumerable<T> GetEnumerableProperty<T>(ExpandoObject obj, string property)
        {
            var asDict = (IDictionary<string, object>)obj;
            var items = (IEnumerable<object>)asDict[property];
            foreach (var item in items)
                yield return item is T
                    ? (T)item
                    : JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(item));
        }

        static string ReadNode(XmlDocument doc, string xpath, string defaultValue = null)
        {
            var node = doc.SelectSingleNode(xpath);
            if (node != null)
                return node.InnerText;
            else if (defaultValue != null)
                return defaultValue;
            else
                throw new InvalidOperationException("Could not find config value at: " + xpath);
        }

        public static class ContentType
        {
            public const string UrlEncoded = "application/x-www-form-urlencoded";
            public const string Json = "application/json";
        }
    }
}

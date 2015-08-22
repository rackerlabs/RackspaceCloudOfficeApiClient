using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;

namespace Rackspace.CloudOffice
{
    public class ApiClient
    {
        private const string DefaultBaseUrl = "https://api.emailsrvr.com";

        private string _baseUrl;
        private string _userKey;
        private string _secretKey;

        public ApiClient(string userKey, string secretKey, string baseUrl=DefaultBaseUrl)
        {
            _userKey = userKey;
            _secretKey = secretKey;
            _baseUrl = baseUrl;
        }

        public ApiClient(string configFilePath = null)
        {
            if (string.IsNullOrEmpty(configFilePath))
                configFilePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RsCloudOfficeApi.config");

            var config = new XmlDocument();
            config.Load(configFilePath);

            _userKey = ReadNode(config, "/config/userKey");
            _secretKey = ReadNode(config, "/config/secretKey");
            _baseUrl = ReadNode(config, "/config/baseUrl", DefaultBaseUrl);
        }

        public async Task<dynamic> Get(string path)
        {
            var response = await GetResponse(CreateJsonRequest("GET", path));
            return ParseJsonStream(response.GetResponseStream());
        }

        public async Task<IEnumerable<dynamic>> GetAll(string path, string pagedProperty, int pageSize=50)
        {
            var result = new List<dynamic>();

            var offset = 0;
            dynamic page;
            do
            {
                var queryString = string.Format("offset={0}&size={1}", offset, pageSize);
                page = await Get(JoinPathWithQueryString(path, queryString));

                result.AddRange(GetProperty<IEnumerable<dynamic>>(page, pagedProperty));

                offset += pageSize;
            } while (offset < page.total);

            return result;
        }

        public async Task<dynamic> Post(string path, object data, string contentType=ContentType.UrlEncoded)
        {
            var request = CreateJsonRequest("POST", path);
            SendRequestBody(request, data, contentType);

            var response = await GetResponse(request);
            return ParseJsonStream(response.GetResponseStream());
        }

        public async Task<dynamic> Put(string path, object data, string contentType=ContentType.UrlEncoded)
        {
            var request = CreateJsonRequest("PUT", path);
            SendRequestBody(request, data, contentType);

            var response = await GetResponse(request);
            return ParseJsonStream(response.GetResponseStream());
        }

        public async void Delete(string path)
        {
            await GetResponse(CreateJsonRequest("DELETE", path));
        }

        private HttpWebRequest CreateJsonRequest(string method, string path)
        {
            var request = (HttpWebRequest)HttpWebRequest.Create(_baseUrl + path);
            request.Method = method;
            request.Accept = ContentType.Json;
            request.UserAgent = "https://gist.github.com/mkropat/07714ac4ecf97e785a4e";

            SignRequest(request);

            return request;
        }

        private void SignRequest(HttpWebRequest request)
        {
            var dateTime = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            request.Headers["X-Api-Signature"] = string.Format("{0}:{1}:{2}",
                _userKey,
                dateTime,
                ComputeSha1(_userKey + request.UserAgent + dateTime + _secretKey));
        }

        private static string ComputeSha1(string data)
        {
            var sha1 = System.Security.Cryptography.SHA1.Create();
            var hashed = sha1.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hashed);
        }

        private static async Task<WebResponse> GetResponse(HttpWebRequest request)
        {
            return await request.GetResponseAsync().ConfigureAwait(false);
        }

        private static string ReadNode(XmlDocument doc, string xpath, string defaultValue=null)
        {
            var node = doc.SelectSingleNode(xpath);
            if (node != null)
                return node.InnerText;
            else if (defaultValue != null)
                return defaultValue;
            else
                throw new InvalidOperationException("Could not find config value at: " + xpath);
        }

        private static void SendRequestBody(HttpWebRequest request, object data, string contentType)
        {
            request.ContentType = contentType;

            using (var writer = new StreamWriter(request.GetRequestStream(), Encoding.ASCII))
            {
                writer.Write(EncodeBody(data, contentType));
            }
        }

        private static string JoinPathWithQueryString(string path, string queryString)
        {
            var joiner = path.Contains("?") ? "&" : "?";
            return path + joiner + queryString;
        }

        private static T GetProperty<T>(ExpandoObject obj, string property)
        {
            var asDict = (IDictionary<string, object>)obj;
            return (T)asDict[property];
        }

        private static ExpandoObject ParseJsonStream(Stream s)
        {
            return JsonConvert.DeserializeObject<ExpandoObject>(
                new StreamReader(s).ReadToEnd());
        }

        private static string EncodeBody(object data, string contentType)
        {
            switch (contentType)
            {
                case ContentType.UrlEncoded: return FormUrlEncode(GetObjectAsDictionary(data));
                case ContentType.Json:       return JsonConvert.SerializeObject(data);
                default: throw new ArgumentException("Unsupported contentType: " + contentType);
            }
        }

        private static IDictionary<string, string> GetObjectAsDictionary(object obj)
        {
            var dict = new Dictionary<string, string>();

            var properties = obj.GetType()
                .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                .OfType<PropertyInfo>();
            foreach (var prop in properties)
                dict[prop.Name] = Convert.ToString(prop.GetValue(obj));

            return dict;
        }

        private static string FormUrlEncode(IDictionary<string, string> data)
        {
            var pairs = data.Select(pair => string.Format("{0}={1}",
                WebUtility.UrlEncode(pair.Key),
                WebUtility.UrlEncode(pair.Value)));
            return string.Join("&", pairs);
        }

        public static class ContentType
        {
            public const string UrlEncoded = "application/x-www-form-urlencoded";
            public const string Json = "application/json";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
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
        readonly IApiRequester _requester = new ThrottledHandlingApiRequesterWrapper(new ApiRequester());
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
            using (var response = await Send("GET", path).ConfigureAwait(false))
            {
                return await ParseResponse<T>(response).ConfigureAwait(false);
            }
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
            using (var response = await Send("POST", path, data, contentType).ConfigureAwait(false))
            {
                return await ParseResponse<T>(response).ConfigureAwait(false);
            }
        }

        public async Task<dynamic> Put(string path, object data, string contentType=ContentType.UrlEncoded)
        {
            return await Put<ExpandoObject>(path, data, contentType).ConfigureAwait(false);
        }

        public async Task<T> Put<T>(string path, object data, string contentType=ContentType.UrlEncoded)
        {
            using (var response = await Send("PUT", path, data, contentType).ConfigureAwait(false))
            {
                return await ParseResponse<T>(response).ConfigureAwait(false);
            }
        }

        public async Task<dynamic> Patch(string path, object data, string contentType=ContentType.UrlEncoded)
        {
            return await Patch<ExpandoObject>(path, data, contentType).ConfigureAwait(false);
        }

        public async Task<T> Patch<T>(string path, object data, string contentType=ContentType.UrlEncoded)
        {
            using (var response = await Send("PATCH", path, data, contentType).ConfigureAwait(false))
            {
                return await ParseResponse<T>(response).ConfigureAwait(false);
            }
        }

        public async Task Delete(string path)
        {
            using (var response = await Send("DELETE", path).ConfigureAwait(false))
            {
            }
        }

        async Task<WebResponse> Send(string method, string path, object body=null, string contentType=null)
        {
            await _throttler.Throttle().ConfigureAwait(false);

            var request = new ApiRequest
            {
                Body = body,
                ContentType = contentType,
                Headers = CustomHeaders,
                Method = method,
                SecretKey = _secretKey,
                Url = BaseUrl + path,
                UserKey = UserKey,
            };
            return await _requester.Send(request).ConfigureAwait(false);
        }

        static Task<T> ParseResponse<T>(WebResponse response)
        {
            using (var s = response.GetResponseStream())
            {
                return ParseJsonStream<T>(s);
            }
        }

        static async Task<T> ParseJsonStream<T>(Stream s)
        {
            using (var reader = new StreamReader(s))
            {
                var body = await reader.ReadToEndAsync().ConfigureAwait(false);
                return JsonConvert.DeserializeObject<T>(body);
            }
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

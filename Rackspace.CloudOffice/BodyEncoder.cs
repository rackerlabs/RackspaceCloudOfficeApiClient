using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using Newtonsoft.Json;

namespace Rackspace.CloudOffice
{
    internal static class BodyEncoder
    {
        public static string Encode(object data, string contentType)
        {
            switch (contentType)
            {
                case ApiClient.ContentType.UrlEncoded: return FormUrlEncode(GetObjectAsDictionary(data));
                case ApiClient.ContentType.Json:       return JsonConvert.SerializeObject(data);
                default: throw new ArgumentException("Unsupported contentType: " + contentType);
            }
        }

        static string FormUrlEncode(IDictionary<string, string> data)
        {
            var pairs = data.Select(pair => string.Format("{0}={1}",
                WebUtility.UrlEncode(pair.Key),
                WebUtility.UrlEncode(pair.Value)));
            return string.Join("&", pairs);
        }

        static IDictionary<string, string> GetObjectAsDictionary(object obj)
        {
            var dict = new Dictionary<string, string>();

            var properties = obj.GetType()
                .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                .OfType<PropertyInfo>();
            foreach (var prop in properties)
                dict[prop.Name] = Convert.ToString(prop.GetValue(obj));

            return dict;
        }
    }
}

using System;
using System.IO;
using System.Xml;

namespace Rackspace.CloudOffice.Helpers
{
    internal class ConfigParser
    {
        public string UserKey => ReadNode("/config/userKey");
        public string SecretKey => ReadNode("/config/secretKey");
        public string BaseUrl => ReadNode("/config/baseUrl", ApiClient.DefaultBaseUrl);

        readonly Lazy<XmlDocument> _doc;

        public ConfigParser(string configFilePath = null)
        {
            if (string.IsNullOrEmpty(configFilePath))
                configFilePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RsCloudOfficeApi.config");

            _doc = new Lazy<XmlDocument>(() =>
            {
                var doc = new XmlDocument();
                doc.Load(configFilePath);
                return doc;
            });
        }

        string ReadNode(string xpath, string defaultValue = null)
        {
            var node = _doc.Value.SelectSingleNode(xpath);
            if (node != null)
                return node.InnerText;
            else if (defaultValue != null)
                return defaultValue;
            else
                throw new InvalidOperationException("Could not find config value at: " + xpath);
        }
    }
}

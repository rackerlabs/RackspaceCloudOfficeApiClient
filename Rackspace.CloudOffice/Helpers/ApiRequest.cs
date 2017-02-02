using System.Collections.Generic;

namespace Rackspace.CloudOffice.Helpers
{
    internal class ApiRequest
    {
        public string Method { get; set; }
        public string Url { get; set; }
        public IDictionary<string, string> Headers { get; set; }
        public string UserKey { get; set; }
        public string SecretKey { get; set; }
        public bool ShouldSendBody { get; set; }
        public object Body { get; set; }
        public string ContentType { get; set; }
    }
}

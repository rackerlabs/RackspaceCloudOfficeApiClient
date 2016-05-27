using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rackspace.CloudOffice
{
    public interface IApiClient
    {
        string BaseUrl { get; }
        string UserKey { get; }

        IDictionary<string, string> CustomHeaders { get; }

        Task<dynamic> Get(string path);
        Task<T> Get<T>(string path);
        Task<IEnumerable<dynamic>> GetAll(string path, string pagedProperty, int pageSize = 50);
        Task<IEnumerable<dynamic>> GetAll(string path, PagingPropertyNames propertyNames, int pageSize = 50);
        Task<IEnumerable<T>> GetAll<T>(string path, string pagedProperty, int pageSize = 50);
        Task<IEnumerable<T>> GetAll<T>(string path, PagingPropertyNames propertyNames, int pageSize = 50);

        Task<dynamic> Post(string path, object data, string contentType = ApiClient.ContentType.UrlEncoded);
        Task<T> Post<T>(string path, object data, string contentType = ApiClient.ContentType.UrlEncoded);
        Task<dynamic> Put(string path, object data, string contentType = ApiClient.ContentType.UrlEncoded);
        Task<T> Put<T>(string path, object data, string contentType = ApiClient.ContentType.UrlEncoded);
        Task<dynamic> Patch(string path, object data, string contentType = ApiClient.ContentType.UrlEncoded);
        Task<T> Patch<T>(string path, object data, string contentType = ApiClient.ContentType.UrlEncoded);
        Task Delete(string path);
    }
}

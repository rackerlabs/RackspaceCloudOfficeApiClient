using System.Net;
using System.Threading.Tasks;

namespace Rackspace.CloudOffice.Helpers
{
    internal interface IApiRequester
    {
        Task<WebResponse> Send(ApiRequest request);
    }
}

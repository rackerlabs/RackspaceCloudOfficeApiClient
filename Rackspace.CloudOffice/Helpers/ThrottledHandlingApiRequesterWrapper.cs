using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace Rackspace.CloudOffice.Helpers
{
    internal class ThrottledHandlingApiRequesterWrapper : IApiRequester
    {
        readonly IApiRequester _requester;

        public ThrottledHandlingApiRequesterWrapper(IApiRequester wrappedRequester)
        {
            _requester = wrappedRequester;
        }

        public async Task<WebResponse> Send(ApiRequest request)
        {
            var delay = TimeSpan.FromSeconds(1);

            while (true)
            {
                try
                {
                    return await _requester.Send(request);
                }
                catch (ApiException ex)
                {
                    if (ex.HttpCode != HttpStatusCode.Forbidden)
                        throw;

                    var isThrottledMessage = "Exceeded request limits".Equals(ex.Response?.unauthorizedFault.message, StringComparison.InvariantCulture);
                    if (!isThrottledMessage)
                        throw;
                }

                Trace.WriteLine($"Throttled. Delaying for {delay}.");
                await Task.Delay(delay);
                delay = new TimeSpan(delay.Ticks * 2);
            }
        }
    }
}

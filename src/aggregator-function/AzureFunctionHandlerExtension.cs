using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace aggregator
{
    public static class AzureFunctionHandlerExtension
    {
        /// <summary>
        /// This method exists for backward compatibility reasons.
        /// </summary>
        /// <param name="this"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> Run(this AzureFunctionHandler @this, HttpRequestMessage req)
        {
            return await @this.RunAsync(req, CancellationToken.None);
        }
    }
}

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


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
        public static async Task<HttpResponseMessage> Run(this AzureFunctionHandler @this, HttpRequestMessage request)
        {
            //var eventData = req
            //return await @this.RunAsync(req, CancellationToken.None);
            return new HttpResponseMessage();
        }
    }
}

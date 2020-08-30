using System.Net.Http;
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
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public static async Task<HttpResponseMessage> Run(this AzureFunctionHandler @this, HttpRequestMessage request)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            //var eventData = req
            //return await @this.RunAsync(req, CancellationToken.None);
            return new HttpResponseMessage();
        }
    }
}

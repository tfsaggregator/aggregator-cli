using System.Net.Http;
using System.Threading.Tasks;


namespace aggregator
{
    public static class AzureFunctionHandlerExtension
    {
        /// <summary>
        /// This method exists for backward compatibility reasons.
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable IDE0060 // Remove unused parameter
        public static async Task<HttpResponseMessage> Run(this AzureFunctionHandler @this, HttpRequestMessage request)
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {

#pragma warning disable S125 // Sections of code should not be commented out
            //var eventData = req
            //return await @this.RunAsync(req, CancellationToken.None);
#pragma warning restore S125 // Sections of code should not be commented out
            return new HttpResponseMessage();
        }
    }
}

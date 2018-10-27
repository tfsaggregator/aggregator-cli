#r "../bin/aggregator-function.dll"
#r "../bin/aggregator-shared.dll"

using aggregator;

public static async Task<object> Run(HttpRequestMessage req, ILogger logger, ExecutionContext context)
{
    var handler = new AzureFunctionHandler(logger, context);
    return await handler.Run(req);
}

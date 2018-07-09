#r "../bin/aggregator-function.dll"

using aggregator;

public static async Task<object> Run(HttpRequestMessage req, TraceWriter log, ExecutionContext context)
{
    var handler = new AzureFunctionHandler(log, context);
    return await handler.Run(req);
}

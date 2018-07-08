#r "../bin/aggregator-function.dll"

using aggregator;

public static async Task<object> Run(HttpRequestMessage req, TraceWriter log, ExecutionContext context)
{
    var processor = new RequestProcessor(log, context);
    return await processor.Run(req);
}

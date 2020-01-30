#r "../bin/aggregator-function.dll"
#r "../bin/aggregator-shared.dll"
#r "../bin/Microsoft.VisualStudio.Services.ServiceHooks.WebApi.dll"

using System.Threading;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;
using aggregator;

public static async Task<IActionResult> Run(HttpRequest req, ILogger logger, ExecutionContext executionContext, CancellationToken cancellationToken)
{
    var handler = new AzureFunctionHandler(logger, executionContext, req.HttpContext);
    var eventData = await req.GetWebHookEvent(logger);
    var result = await handler.RunAsync(eventData, cancellationToken);
    return result;
}

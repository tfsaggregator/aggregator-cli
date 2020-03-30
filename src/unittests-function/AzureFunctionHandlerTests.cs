using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using aggregator;
using aggregator.Engine;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.ServiceHooks.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

using Newtonsoft.Json;

using NSubstitute;
using unittests_function.TestData;
using Xunit;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace unittests_function
{
    public class AzureFunctionHandlerTests
    {
        private readonly TestAzureFunctionHandler azureFunctionHandler;

        public AzureFunctionHandlerTests()
        {
            ILogger logger;
            ExecutionContext context;
            HttpContext httpContext;

            logger = Substitute.For<ILogger>();
            context = Substitute.For<ExecutionContext>();
            context.InvocationId = Guid.Empty;
            context.FunctionName = "TestRule";
            context.FunctionDirectory = "";
            context.FunctionAppDirectory = "";

            var services = new ServiceCollection()
                .AddMvc()
                .AddWebApiConventions()
                .Services
                .BuildServiceProvider();

            httpContext = new DefaultHttpContext()
                          {
                              RequestServices = services,
                          };

            httpContext.Request.Protocol = "http";
            httpContext.Request.Host = new HostString("localhost");
            httpContext.Request.Method = HttpMethod.Post.ToString();

            azureFunctionHandler = new TestAzureFunctionHandler(logger, context, httpContext);
        }

        [Fact]
        public async void HandleTestEvent_ReturnAggregatorInformation_Succeeds()
        {
            IActionResult actionResult = await azureFunctionHandler.RunAsync(ExampleEvents.TestEvent, CancellationToken.None);

            var objectResult = actionResult as ObjectResult;
            Assert.True(IsSuccessStatusCode(objectResult.StatusCode));
            Assert.True(azureFunctionHandler.Response.Headers.TryGetValue("X-Aggregator-Version", out var versions));
            Assert.Single(versions);


            Assert.True(azureFunctionHandler.Response.Headers.TryGetValue("X-Aggregator-Rule", out var rules));
            Assert.Equal("TestRule", rules.Single());

            var content = JsonConvert.SerializeObject(objectResult.Value);
            Assert.StartsWith("{\"message\":\"Hello from Aggregator v", content);
            Assert.EndsWith("executing rule 'TestRule'\"}", content);
        }

        private static bool IsSuccessStatusCode(int? statusCode)
        {
            return statusCode.HasValue &&
                   (statusCode >= 200) && (statusCode <= 299);
        }

        public static IEnumerable<object[]> Data =>
            new List<object[]>
            {
                new object[] { ExampleEvents.WorkItemUpdateEventResourceVersion10 },
                new object[] { ExampleEvents.WorkItemUpdateEventResourceVersion31Preview3 },
                new object[] { ExampleEvents.WorkItemUpdateEventResourceVersion51Preview3 },
            };

        [Theory]
        [MemberData(nameof(Data))]
        public void HandleEventsWithDifferentResourceVersion_CheckIdentityConversion_Succeeds(WebHookEvent eventData)
        {
            var eventContext = azureFunctionHandler.InvokeCreateContextFromEvent(eventData);

            var workItem = eventContext.WorkItemPayload.WorkItem;

            Assert.IsType<IdentityRef>(workItem.Fields[CoreFieldRefNames.AssignedTo]);
            Assert.IsType<IdentityRef>(workItem.Fields[CoreFieldRefNames.ChangedBy]);
            Assert.IsType<IdentityRef>(workItem.Fields[CoreFieldRefNames.CreatedBy]);
        }
    }


    internal class TestAzureFunctionHandler : AzureFunctionHandler
    {
        /// <inheritdoc />
        public TestAzureFunctionHandler(ILogger logger, ExecutionContext executionContext, HttpContext httpContext) : base(logger, executionContext, httpContext) { }


        internal WorkItemEventContext InvokeCreateContextFromEvent(WebHookEvent eventData)
        {
            return CreateContextFromEvent(eventData);
        }

        internal void InvokeMigrateIdentityInformation(string resourceVersion, WorkItem workItem)
        {
            MigrateIdentityInformation(resourceVersion, workItem);
        }
    }
}

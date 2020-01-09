using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using aggregator;
using aggregator.Engine;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.ServiceHooks.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

using NSubstitute;
using unittests_function.TestData;
using Xunit;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace unittests_function
{
    public class AzureFunctionHandlerTests
    {
        private readonly ILogger logger;
        private readonly ExecutionContext context;
        private readonly HttpRequestMessage request;

        public AzureFunctionHandlerTests()
        {
            logger = Substitute.For<ILogger>();
            context = Substitute.For<ExecutionContext>();
            context.InvocationId = Guid.Empty;
            context.FunctionName = "TestRule";
            context.FunctionDirectory = "";
            context.FunctionAppDirectory = "";

            request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/");
            var services = new ServiceCollection()
                .AddMvc()
                .AddWebApiConventions()
                .Services
                .BuildServiceProvider();

            request.Properties.Add(nameof(HttpContext), new DefaultHttpContext
            {
                RequestServices = services
            });
        }

        [Fact]
        public async void HandleTestEvent_ReturnAggregatorInformation_Succeeds()
        {
            request.Content = new StringContent(ExampleEvents.TestEventAsString, Encoding.UTF8, "application/json");

            var handler = new AzureFunctionHandler(logger, context);
            var response = await handler.RunAsync(request, CancellationToken.None);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Headers.TryGetValues("X-Aggregator-Version", out var versions));
            Assert.Single(versions);

            Assert.True(response.Headers.TryGetValues("X-Aggregator-Rule", out var rules));
            Assert.Equal("TestRule", rules.Single());

            var content = await response.Content.ReadAsStringAsync();
            Assert.StartsWith("{\"message\":\"Hello from Aggregator v", content);
            Assert.EndsWith("executing rule 'TestRule'\"}", content);
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
            var handler = new TestAzureFunctionHandler(logger, context);

            var eventContext = handler.InvokeCreateContextFromEvent(eventData);

            var workItem = eventContext.WorkItemPayload.WorkItem;

            Assert.IsType<IdentityRef>(workItem.Fields[CoreFieldRefNames.AssignedTo]);
            Assert.IsType<IdentityRef>(workItem.Fields[CoreFieldRefNames.ChangedBy]);
            Assert.IsType<IdentityRef>(workItem.Fields[CoreFieldRefNames.CreatedBy]);
        }
    }


    class TestAzureFunctionHandler : AzureFunctionHandler
    {
        /// <inheritdoc />
        public TestAzureFunctionHandler(ILogger logger, ExecutionContext context) : base(logger, context) { }


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

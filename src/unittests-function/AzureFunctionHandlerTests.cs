using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using aggregator;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using unittests_ruleng.TestData;
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
            request.Content = new StringContent(ExampleTestData.TestEventAsString, Encoding.UTF8, "application/json");

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
    }
}

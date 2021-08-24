using System.Collections.Generic;
using aggregator;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;
using Xunit;


namespace unittests_function
{
    public class RequestHelperTests
    {
        [Fact]
        public void MigrateIdentityInformationUpdatesIdentityFields()
        {
            var logger = NSubstitute.Substitute.For<ILogger>();
            WorkItem workItem = new WorkItem
            {
                Fields = new Dictionary<string, object>
                {
                  { "System.WorkItemType", "Bug" },
                  { "FixedBy", "Jane Doe <jdoe@example.com>" },
                },
            };

            new RequestHelper(logger).MigrateIdentityInformation("1.0", workItem);

            var identityValue = workItem.Fields["FixedBy"];
            Assert.IsType<IdentityRef>(identityValue);
            Assert.Equal("Jane Doe", ((IdentityRef)identityValue).DisplayName);
            Assert.Equal("jdoe@example.com", ((IdentityRef)identityValue).UniqueName);
        }

        [Fact]
        public void MigrateIdentityInformationDoesNotUpdateStrings()
        {
            var logger = NSubstitute.Substitute.For<ILogger>();
            WorkItem workItem = new WorkItem
            {
                Fields = new Dictionary<string, object>
                {
                  { "System.WorkItemType", "Bug" },
                  { "VerifyBy", "GA" },
                  { "SmileTo", ">_<" },
                },
            };

            new RequestHelper(logger).MigrateIdentityInformation("1.0", workItem);

            var stringValue = workItem.Fields["VerifyBy"];
            Assert.IsType<string>(stringValue);
            Assert.Equal("GA", stringValue);

            stringValue = workItem.Fields["SmileTo"];
            Assert.IsType<string>(stringValue);
            Assert.Equal(">_<", stringValue);
        }
    }
}

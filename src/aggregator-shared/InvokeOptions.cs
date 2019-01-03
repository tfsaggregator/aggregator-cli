using System;
using System.Collections.Generic;
using System.Text;

namespace aggregator
{
    public class InvokeOptions
    {
        public static string AppendToUrl(string ruleUrl, bool dryRun, SaveMode saveMode)
        {
            return ruleUrl + $"?dryRun={dryRun}&saveMode={saveMode}";
        }

        public static AggregatorConfiguration ExtendFromUrl(AggregatorConfiguration configuration, Uri requestUri)
        {
            var parameters = System.Web.HttpUtility.ParseQueryString(requestUri.Query);

            bool dryRun = bool.TryParse(parameters["dryRun"], out dryRun) ? dryRun : false;
            configuration.DryRun = dryRun;

            SaveMode saveMode;
            if (Enum.TryParse(parameters["saveMode"], out saveMode))
            {
                configuration.SaveMode = saveMode;
            }

            return configuration;
        }
    }
}

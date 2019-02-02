using System;

namespace aggregator
{
    public static class InvokeOptions
    {
        public static string AppendToUrl(string ruleUrl, bool dryRun, SaveMode saveMode)
        {
            return ruleUrl + FormattableString.Invariant($"?dryRun={dryRun}&saveMode={saveMode}");
        }

        public static AggregatorConfiguration ExtendFromUrl(AggregatorConfiguration configuration, Uri requestUri)
        {
            var parameters = System.Web.HttpUtility.ParseQueryString(requestUri.Query);

            bool dryRun = bool.TryParse(parameters["dryRun"], out dryRun) && dryRun;
            configuration.DryRun = dryRun;

            if (Enum.TryParse(parameters["saveMode"], out SaveMode saveMode))
            {
                configuration.SaveMode = saveMode;
            }

            return configuration;
        }
    }
}

using System;
using System.Collections.Specialized;
using System.Globalization;

using Microsoft.WindowsAzure.Storage.Core;


namespace aggregator
{
    public static class InvokeOptions
    {
        public static Uri AddToUrl(this Uri ruleUrl, bool dryRun = false, SaveMode saveMode = SaveMode.Default, bool impersonate = false)
        {
            var queryBuilder = new UriQueryBuilder();
            queryBuilder.Add("dryRun", dryRun.ToString(CultureInfo.InvariantCulture));
            queryBuilder.Add("saveMode", saveMode.ToString());

            if (impersonate)
            {
                queryBuilder.Add("execute", "impersonated");
            }

            return queryBuilder.AddToUri(ruleUrl);
        }

        public static AggregatorConfiguration UpdateFromUrl(this AggregatorConfiguration configuration, Uri requestUri)
        {
            var parameters = System.Web.HttpUtility.ParseQueryString(requestUri.Query);

            configuration.DryRun = IsDryRunEnabled(parameters);

            if (Enum.TryParse(parameters["saveMode"], out SaveMode saveMode))
            {
                configuration.SaveMode = saveMode;
            }

            configuration.Impersonate = IsImpersonatationEnabled(parameters);

            return configuration;
        }

        public static bool IsImpersonatationEnabled(this Uri ruleUrl)
        {
            var parameters = System.Web.HttpUtility.ParseQueryString(ruleUrl.Query);

            return IsImpersonatationEnabled(parameters);
        }

        private static bool IsImpersonatationEnabled(NameValueCollection parameters)
        {
            return string.Equals(parameters["execute"], "impersonated", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDryRunEnabled(NameValueCollection parameters)
        {
            bool dryRun = bool.TryParse(parameters["dryRun"], out dryRun) && dryRun;
            return dryRun;
        }
    }
}

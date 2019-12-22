using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;

using Microsoft.WindowsAzure.Storage.Core;


namespace aggregator
{
    public static class InvokeOptions
    {
        /// <summary>
        /// extend Url with configuration information
        /// </summary>
        /// <param name="ruleUrl"></param>
        /// <param name="dryRun"></param>
        /// <param name="saveMode"></param>
        /// <param name="impersonate"></param>
        /// <returns></returns>
        public static Uri AddToUrl(this Uri ruleUrl, bool dryRun = false, SaveMode saveMode = SaveMode.Default, bool impersonate = false)
        {
            var queryBuilder = new UriQueryBuilder();

            queryBuilder.AddIfNotDefault("dryRun", dryRun)
                        .AddIfNotDefault("saveMode", saveMode);

            return queryBuilder.AddToUri(ruleUrl);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="ruleName"></param>
        /// <param name="requestUri"></param>
        /// <returns></returns>
        public static IAggregatorConfiguration UpdateFromUrl(this IAggregatorConfiguration configuration, string ruleName, Uri requestUri)
        {
            var parameters = System.Web.HttpUtility.ParseQueryString(requestUri.Query);

            configuration.DryRun = IsDryRunEnabled(parameters);
            configuration.SaveMode = GetSaveMode(parameters);

            return configuration;
        }

        private static bool IsDryRunEnabled(NameValueCollection parameters)
        {
            bool dryRun = bool.TryParse(parameters["dryRun"], out dryRun) && dryRun;
            return dryRun;
        }

        private static SaveMode GetSaveMode(NameValueCollection parameters)
        {
            return Enum.TryParse(parameters["saveMode"], out SaveMode saveMode) ? saveMode : SaveMode.Default;
        }

        private static UriQueryBuilder AddIfNotDefault<T>(this UriQueryBuilder queryBuilder, string name, T value, T defaultValue = default, string valueString = null)
        {
            if (!EqualityComparer<T>.Default.Equals(defaultValue, value))
            {
                queryBuilder.Add(name, valueString ?? Convert.ToString(value, CultureInfo.InvariantCulture));
            }

            return queryBuilder;
        }
    }
}

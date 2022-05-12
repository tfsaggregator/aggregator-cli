namespace aggregator
{
    public static class MagicConstants
    {
        public const string LoggerCategoryName = "Aggregator";
        public const string ApiKeyAuthenticationHeaderName = "X-Auth-ApiKey";
        public const string AzureFunctionKeyHeaderName = "x-functions-key";
        public const string InvalidApiKey = "INVALID";
#pragma warning disable S1075 // URIs should not be hardcoded
        public const string MissingUrl = "https://this.should.never.come.up.example.com/";
#pragma warning restore S1075 // URIs should not be hardcoded
#pragma warning disable CA1707 // Identifiers should not contain underscores
        public const string EnvironmentVariable_SharedSecret = "Aggregator_SharedSecret";
#pragma warning restore CA1707 // Identifiers should not contain underscores
    }
}

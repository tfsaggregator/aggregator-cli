namespace aggregator.cli
{
    static class ContextExtensions
    {
        public static void ResourceGroupDeprecationCheck(this CommandContext ctx, string rg)
        {
            if (string.IsNullOrWhiteSpace(rg))
            {
                ctx.Logger.WriteWarning($"Deprecation notice: the resourceGroup option will be mandatory in a future version of Aggregator.");
            }
        }
    }
}

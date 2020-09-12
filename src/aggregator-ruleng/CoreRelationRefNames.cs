namespace aggregator.Engine
{
    internal static class CoreRelationRefNames
    {
        public const string Parent = "System.LinkTypes.Hierarchy-Reverse";
        public const string Children = "System.LinkTypes.Hierarchy-Forward";
        public const string Related = "System.LinkTypes.Related";
        public const string Hyperlink = "Hyperlink";
        // TODO this is not implemented but should be
        public const string AttachedFile = "AttachedFile";
    }
}

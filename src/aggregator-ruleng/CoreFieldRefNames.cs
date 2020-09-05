#pragma warning disable S125 // Sections of code should not be commented out
/*
 *  TODO replace this class with references to 
using Microsoft.TeamFoundation.WorkItemTracking.Common;
using Microsoft.TeamFoundation.WorkItemTracking.Common.Constants;
*/
#pragma warning restore S125 // Sections of code should not be commented out

namespace aggregator.Engine
{
    internal static class CoreFieldRefNames
    {

        public const string TeamProject = "System.TeamProject";
        public const string WorkItemType = "System.WorkItemType";
        public const string Id = "System.Id";
        public const string State = "System.State";

        public const string AreaId = "System.AreaId";
        public const string AreaPath = "System.AreaPath";
        public const string AssignedTo = "System.AssignedTo";
        public const string AttachedFileCount = "System.AttachedFileCount";
        public const string AuthorizedAs = "System.AuthorizedAs";
        public const string ChangedBy = "System.ChangedBy";
        public const string ChangedDate = "System.ChangedDate";
        public const string CreatedBy = "System.CreatedBy";
        public const string CreatedDate = "System.CreatedDate";
        public const string Description = "System.Description";
        public const string ExternalLinkCount = "System.ExternalLinkCount";
        public const string History = "System.History";
        public const string HyperLinkCount = "System.HyperLinkCount";
        public const string IterationId = "System.IterationId";
        public const string IterationPath = "System.IterationPath";
        public const string Reason = "System.Reason";
        public const string RelatedLinkCount = "System.RelatedLinkCount";
        public const string RevisedBy = "System.RevisedBy";
        public const string RevisedDate = "System.RevisedDate";
        public const string AuthorizedDate = "System.AuthorizedDate";
        public const string Tags = "System.Tags";
        public const string Title = "System.Title";
        public const string Watermark = "System.Watermark";
        public const string IsDeleted = "System.IsDeleted";
    }
}

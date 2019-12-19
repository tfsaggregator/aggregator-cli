using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace aggregator.Engine
{
    public class WorkItemRelationWrapper
    {
        private readonly WorkItemRelation _relation;

        private WorkItemRelationWrapper(string relationUrl)
        {
            if (!string.IsNullOrWhiteSpace(relationUrl))
            {
                var relationUri = new Uri(relationUrl);
                var id = int.Parse(relationUri.Segments.Last());
                LinkedId = new PermanentWorkItemId(id);
            }
        }

        internal WorkItemRelationWrapper(WorkItemRelation relation) : this(relation.Url)
        {
            _relation = relation;
        }


        internal WorkItemRelationWrapper(string type, string url, string comment) : this(url)
        {
            _relation = new WorkItemRelation()
            {
                Rel = type,
                Url = url,
                Attributes = new Dictionary<string,object> { { "comment", comment } }
            };
        }

        public string Title => _relation.Title;

        public string Rel => _relation.Rel;

        public string Url => _relation.Url;

        public WorkItemId LinkedId { get; }

        public IDictionary<string, object> Attributes => _relation.Attributes;
    }
}

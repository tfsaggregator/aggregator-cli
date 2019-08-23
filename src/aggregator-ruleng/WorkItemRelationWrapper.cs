using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace aggregator.Engine
{
    public class WorkItemRelationWrapper
    {
        private readonly WorkItemRelation _relation;

        internal WorkItemRelationWrapper(WorkItemRelation relation)
        {
            _relation = relation;
        }

        internal WorkItemRelationWrapper(string type, string url, string comment)
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

        public IDictionary<string, object> Attributes => _relation.Attributes;
    }
}

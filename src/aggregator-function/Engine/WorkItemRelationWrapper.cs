using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace aggregator.Engine
{
    public class WorkItemRelationWrapper
    {
        private WorkItemRelation _relation;
        private readonly WorkItemWrapper _item;

        internal WorkItemRelationWrapper(WorkItemWrapper item, WorkItemRelation relation)
        {
            _item = item;
            _relation = relation;
        }

        internal WorkItemRelationWrapper(WorkItemWrapper item, string type, string url, string comment)
        {
            _item = item;
            _relation = new WorkItemRelation()
            {
                Rel = type,
                Url = url,
                Attributes = { { "comment", comment } }
            };
        }

        public string Title
        {
            get
            {
                return _relation.Title;
            }
        }

        public string Rel
        {
            get
            {
                return _relation.Rel;
            }
        }

        public string Url
        {
            get
            {
                return _relation.Url;
            }
        }

        public IDictionary<string, object> Attributes
        {
            get { return _relation.Attributes; }
        }
    }
}

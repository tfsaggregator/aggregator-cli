using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace aggregator
{
    public class VstsEvents
    {
        // TODO this table should be visible in the help
        static string[] validValues = new string[] {
            "workitem.created",
            "workitem.deleted",
            "workitem.restored",
            "workitem.updated",
            "workitem.commented"
        };

        public static bool IsValidEvent(string @event)
        {
            return validValues.Contains(@event);
        }

        public static string PublisherId => "tfs";
    }
}

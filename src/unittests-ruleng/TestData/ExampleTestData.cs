using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

using Newtonsoft.Json;


namespace unittests_ruleng.TestData
{
    class ExampleTestData
    {
        public static ExampleTestData Instance => new ExampleTestData();

        public WorkItem DeltedWorkItem => GetFromResource("DeltedWorkItem.json");
        public WorkItem WorkItem => GetFromResource("WorkItem.22.json");


        private static string GetEmbeddedResourceContent(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            var fullName = assembly.GetManifestResourceNames()
                                   .Single(str => str.EndsWith(resourceName));

            string fileContent;
            using (Stream stream = assembly.GetManifestResourceStream(fullName))
            {
                using (StreamReader source = new StreamReader(stream))
                {
                    fileContent = source.ReadToEnd();
                }
            }

            return fileContent;
        }

        private static WorkItem GetFromResource(string resourceName)
        {
            var json = GetEmbeddedResourceContent(resourceName);
            return JsonConvert.DeserializeObject<WorkItem>(json);
        }
    }
}

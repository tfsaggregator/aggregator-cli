using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.TeamFoundation.Work.WebApi.Contracts;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

using Newtonsoft.Json;


namespace unittests_ruleng.TestData
{
    class ExampleTestData
    {
        public static ExampleTestData Instance => new ExampleTestData();

        public WorkItem DeltedWorkItem => GetFromResource<WorkItem>("DeletedWorkItem.json");
        public WorkItem WorkItem => GetFromResource<WorkItem>("WorkItem.22.json");
        public WorkItemUpdate WorkItemUpdateFields => GetFromResource<WorkItemUpdate>("WorkItem.22.UpdateFields.json");
        public WorkItemUpdate WorkItemUpdateLinks => GetFromResource<WorkItemUpdate>("WorkItem.22.UpdateLinks.json");


        public WorkItem BacklogFeature => GetFromResource<WorkItem>("Backlog.Feature1.json");
        public WorkItem BacklogUserStoryNew => GetFromResource<WorkItem>("Backlog.UserStory2_New.json");
        public WorkItem BacklogUserStoryActive => GetFromResource<WorkItem>("Backlog.UserStory2_Active.json");


        public ProcessConfiguration ProcessConfigDefaultAgile => GetFromResource<ProcessConfiguration>("WorkClient.ProcessConfiguration.Agile.json");
        public ProcessConfiguration ProcessConfigDefaultScrum => GetFromResource<ProcessConfiguration>("WorkClient.ProcessConfiguration.Scrum.json");
        public WorkItemStateColor[] WorkItemStateColorDefault =>  GetFromResource<WorkItemStateColor[]>("WitClient.WorkItemStateColor.EpicFeatureUserStory.json");


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

        private static T GetFromResource<T>(string resourceName)
        {
            var json = GetEmbeddedResourceContent(resourceName);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}

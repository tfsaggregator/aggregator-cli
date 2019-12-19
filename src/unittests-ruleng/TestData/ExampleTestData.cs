using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.TeamFoundation.Work.WebApi.Contracts;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

using Newtonsoft.Json;


namespace unittests_ruleng.TestData
{
    class Helper
    {
        private static string GetEmbeddedResourceContent(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            var fullName = assembly.GetManifestResourceNames()
                                   .SingleOrDefault(str => str.EndsWith(resourceName)) ?? throw new FileNotFoundException("EmbeddedResource Not Found - Wrong name, or forget to set resource file Build Action to 'Embedded resource'");

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

        internal static T GetFromResource<T>(string resourceName)
        {
            var json = GetEmbeddedResourceContent(resourceName);
            return JsonConvert.DeserializeObject<T>(json);
        }

        internal static string[] GetFromResource(string resourceName)
        {
            var content = GetEmbeddedResourceContent(resourceName);
            return content.Split(Environment.NewLine);
        }
    }

    static class ExampleRuleCode
    {
        public static string[] ActivateParent => Helper.GetFromResource("advanced.activate-parent.rulecode");
        public static string[] ResolveParent => Helper.GetFromResource("advanced.resolve-parent.rulecode");
    }

    static class ExampleTestData
    {
        public static WorkItem DeltedWorkItem => Helper.GetFromResource<WorkItem>("DeletedWorkItem.json");
        public static WorkItem WorkItem => Helper.GetFromResource<WorkItem>("WorkItem.22.json");
        public static WorkItemUpdate WorkItemUpdateFields => Helper.GetFromResource<WorkItemUpdate>("WorkItem.22.UpdateFields.json");
        public static WorkItemUpdate WorkItemUpdateLinks => Helper.GetFromResource<WorkItemUpdate>("WorkItem.22.UpdateLinks.json");


        public static WorkItem BacklogFeatureOneChild => Helper.GetFromResource<WorkItem>("Backlog.Feature1.OneChild.json");
        public static WorkItem BacklogFeatureTwoChildren => Helper.GetFromResource<WorkItem>("Backlog.Feature1.TwoChildren.json");
        public static WorkItem BacklogUserStoryNew => Helper.GetFromResource<WorkItem>("Backlog.UserStory2_New.json");
        public static WorkItem BacklogUserStoryActive => Helper.GetFromResource<WorkItem>("Backlog.UserStory2_Active.json");
        public static WorkItem BacklogUserStoryClosed => Helper.GetFromResource<WorkItem>("Backlog.UserStory2_Closed.json");


        public static ProcessConfiguration ProcessConfigDefaultAgile => Helper.GetFromResource<ProcessConfiguration>("WorkClient.ProcessConfiguration.Agile.json");
        public static ProcessConfiguration ProcessConfigDefaultScrum => Helper.GetFromResource<ProcessConfiguration>("WorkClient.ProcessConfiguration.Scrum.json");
        public static WorkItemStateColor[] WorkItemStateColorDefault => Helper.GetFromResource<WorkItemStateColor[]>("WitClient.WorkItemStateColor.EpicFeatureUserStory.json");
    }
}

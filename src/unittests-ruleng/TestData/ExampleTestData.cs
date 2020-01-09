using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.TeamFoundation.Work.WebApi.Contracts;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.ServiceHooks.WebApi;

using Newtonsoft.Json;


namespace unittests_ruleng.TestData
{
    internal static class Helper
    {
        internal static string GetEmbeddedResourceContent(string resourceName)
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

    internal static class ExampleRuleCode
    {
        public static string[] ActivateParent => Helper.GetFromResource("advanced.activate-parent.rulecode");
        public static string[] ResolveParent => Helper.GetFromResource("advanced.resolve-parent.rulecode");
    }

    internal static class ExampleTestData
    {
        public static WorkItem DeltedWorkItem => Helper.GetFromResource<WorkItem>("DeletedWorkItem.json");
        public static WorkItem WorkItem => Helper.GetFromResource<WorkItem>("WorkItem.22.json");
        public static WorkItemUpdate WorkItemUpdateFields => Helper.GetFromResource<WorkItemUpdate>("WorkItem.22.UpdateFields.json");
        public static WorkItemUpdate WorkItemUpdateLinks => Helper.GetFromResource<WorkItemUpdate>("WorkItem.22.UpdateLinks.json");


        public static WorkItem WorkItemResourceVersion10 => Helper.GetFromResource<WorkItem>("WorkItem.30.ResourceVersion-1.0.json");
        public static WorkItem WorkItemResourceVersion31preview3 => Helper.GetFromResource<WorkItem>("WorkItem.30.ResourceVersion-3.1-preview.3.json");
        public static WorkItem WorkItemResourceVersion51preview3 => Helper.GetFromResource<WorkItem>("WorkItem.30.ResourceVersion-5.1-preview.3.json");


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

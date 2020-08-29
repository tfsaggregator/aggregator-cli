﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.Services.ServiceHooks.WebApi;

using Newtonsoft.Json;


namespace unittests_function.TestData
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

    public static class ExampleEvents
    {
        public static WebHookEvent WorkItemUpdateEventResourceVersion10 => Helper.GetFromResource<WebHookEvent>("ResourceVersion-1.0.json");
        public static WebHookEvent WorkItemUpdateEventResourceVersion31Preview3 => Helper.GetFromResource<WebHookEvent>("ResourceVersion-3.1-preview.3.json");
        public static WebHookEvent WorkItemUpdateEventResourceVersion51Preview3 => Helper.GetFromResource<WebHookEvent>("ResourceVersion-5.1-preview.3.json");


        public static WebHookEvent TestEvent => Helper.GetFromResource<WebHookEvent>("TestEvent.json");
        public static string TestEventAsString => Helper.GetEmbeddedResourceContent("TestEvent.json");
    }
}

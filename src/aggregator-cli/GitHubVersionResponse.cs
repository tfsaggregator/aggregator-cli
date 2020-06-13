using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.TestManagement.WebApi.Legacy;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace aggregator.cli
{
    class GitHubVersionResponse
    {
        public static string CacheFileName =>
            LocalAppData.GetPath("githubresponse.cache");


        private readonly TimeSpan _cacheExpiration = new TimeSpan(1, 0, 0, 0);

        public string Tag { get; set; }
        public string Name { get; set; }

        public DateTimeOffset? When { get; set; }

        public string Url { get; set; }

        public DateTime ResponseDate { get; set; }

        public static GitHubVersionResponse TryReadFromCache()
        {
            try
            {
                if (System.IO.File.Exists(CacheFileName))
                {
                    return JsonConvert.DeserializeObject<GitHubVersionResponse>(System.IO.File.ReadAllText(CacheFileName));
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
           
            
        }

        public bool CacheIsInDate()
        {
            return (DateTime.Now.Subtract(_cacheExpiration) <= ResponseDate);
        }

        public bool SaveCache()
        {
            try
            {
                System.IO.File.WriteAllText(CacheFileName, JsonConvert.SerializeObject(this));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            
        }

        public static bool ClearCache()
        {
            try
            {
                System.IO.File.Delete(CacheFileName);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

using Microsoft.CodeAnalysis.Scripting;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace aggregator
{
    internal class Cache
    {
        private static readonly Cache instance = new Cache();

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static Cache() {}

        private Cache() {}

        public static Cache Instance => instance;

        VssConnection vssConnection = null;
        Script<string> script = null;

        internal VssConnection GetVstsConnection(Func<Task<VssConnection>> ctor)
        {
            if (vssConnection == null)
            {
                vssConnection = ctor().Result;
            }
            return vssConnection;
        }

        internal Script<string> GetScript(Func<Script<string>> ctor)
        {
            if (script == null)
            {
                script = ctor();
            }
            return script;
        }
    }
}

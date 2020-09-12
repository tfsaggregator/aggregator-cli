using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace aggregator.cli
{
    internal static class Extensions
    {
        internal static async Task<string> GetEmbeddedResourceContent(this Assembly assembly, string resourceName)
        {
            var fullName = assembly.GetManifestResourceNames()
                                   .SingleOrDefault(str => str.EndsWith(resourceName))
                           ?? throw new FileNotFoundException($"Embedded Resource '{resourceName}' not found.");

            string content;
            using (var stream = assembly.GetManifestResourceStream(fullName))
            {
                using (var source = new StreamReader(stream))
                {
                    content = await source.ReadToEndAsync();
                }
            }

            return content;
        }

        internal static async Task AddFunctionDefaultFiles(this IDictionary<string, string> uploadFiles, Stream assemblyStream)
        {
            var context = new AssemblyLoadContext(null, isCollectible: true);

            using (var memoryStream = new MemoryStream())
            {
                assemblyStream.CopyTo(memoryStream);
                memoryStream.Position = 0;
                var assembly = context.LoadFromStream(memoryStream);

                await AddFunctionDefaultFiles(uploadFiles, assembly);
            }

            context.Unload();
        }

        internal static async Task AddFunctionDefaultFiles(this IDictionary<string, string> uploadFiles, Assembly assembly)
        {
            await AddFileFromResource(uploadFiles, assembly, "function.json");
            await AddFileFromResource(uploadFiles, assembly, "run.csx");

            async Task AddFileFromResource(IDictionary<string, string> _uploadFiles, Assembly _assembly, string filename)
            {
                var content = await _assembly.GetEmbeddedResourceContent(filename);
                _uploadFiles.Add(filename, content);
            }
        }
    }
}

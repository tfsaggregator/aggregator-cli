using System;
using System.Globalization;
using Semver;

namespace aggregator.cli
{
    internal static class ManifestParser
    {
        private const string VersionPrefix = "version=";

        public static ManifestInfo Parse(string content)
        {
            if (content == null)
            {
                throw new System.ArgumentNullException(nameof(content));
            }

            var lines = content.Split(new [] { "\n", "\r\n" }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                var isVersionLine = line.StartsWith(VersionPrefix, false, CultureInfo.InvariantCulture);
                if (isVersionLine)
                {
                    var version = SemVersion.Parse(line.Substring(VersionPrefix.Length));
                    return new ManifestInfo(version);
                }
            }

            throw new InvalidOperationException("Manifest does not contain a version number");
        }
    }

    internal class ManifestInfo
    {
        public ManifestInfo(SemVersion version)
        {
            Version = version;
        }

        public SemVersion Version { get; }
    }
}
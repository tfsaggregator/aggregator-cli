using System;
using System.Collections.Generic;
using System.Linq;

namespace aggregator.cli
{
    static class TelemetryFormatter
    {
        internal static string Format(TelemetryDisplayMode mode, object value)
        {
            try
            {
                switch (mode)
                {
                    case TelemetryDisplayMode.AsIs:
                        if (value == null)
                            return "";
                        switch (value)
                        {
                            case IEnumerable<string> listValue:
                                return string.Join(";", listValue.ToArray());
                            default:
                                return value.ToString();
                        }
                    case TelemetryDisplayMode.Presence:
                        return value != null ? "set" : "unset";
                    case TelemetryDisplayMode.MaskOthersUrl:
                        return StripUrl(value);
                    default:
                        return "UNSUPPORTED";
                }
            }
            catch (Exception e)
            {
                return $"TelemetryFormatter exception {e.Message}";
            }
        }

        private static string StripUrl(object value)
        {
            if (value == null)
                return "";

            if (!Uri.TryCreate(value.ToString(), UriKind.Absolute, out Uri uri))
                return "";

            if (uri.Host == "github.com"
                && uri.Segments.Length > 1
                && uri.Segments[1] == "tfsaggregator/")
            {
                // our stuff goes clear
                return uri.AbsoluteUri;
            }

            // mask anything else
            return $"{uri.Scheme}://***/{uri.Segments.Last()}";
        }
    }
}

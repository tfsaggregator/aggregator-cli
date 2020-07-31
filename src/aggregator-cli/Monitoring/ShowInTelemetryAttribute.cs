using System;
using System.Collections.Generic;
using System.Text;

namespace aggregator.cli
{
    enum TelemetryDisplayMode
    {
        AsIs,
        Presence,
        MaskOthersUrl
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    class ShowInTelemetryAttribute : Attribute
    {
        public ShowInTelemetryAttribute(TelemetryDisplayMode mode = TelemetryDisplayMode.AsIs)
        {
            this.Mode = mode;
        }

        public TelemetryDisplayMode Mode { get; private set; }
    }
}

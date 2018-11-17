using System;
using System.Collections.Generic;
using System.Text;

namespace aggregator.Engine
{
    public class BatchRequest
    {
        public string method { get; set; }
        public Dictionary<string, string> headers { get; set; }
        public object[] body { get; set; }
        public string uri { get; set; }
    }
}

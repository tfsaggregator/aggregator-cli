using System;
using System.Collections.Generic;
using System.Text;

namespace aggregator
{
    public static class EnvironmentVariables
    {
        public static bool GetAsBool(string varName, bool valueIfMissing = false)
        {
            string str = Environment.GetEnvironmentVariable(varName);
            if (str == null)
                return valueIfMissing;

            bool isTrue = false;
            switch (str.ToLowerInvariant())
            {
                case "true":
                case "yes":
                case "1":
                    isTrue = true;
                    break;
                case "false":
                case "no":
                case "0":
                    isTrue = false;
                    break;
                default:
                    throw new ArgumentException("Environment variable was not truthy nor falsy", varName);
            }
            return isTrue;
        }
    }
}

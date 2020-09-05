using System;
using System.Linq;
using System.Security.Cryptography;

namespace aggregator
{
    public static class SharedSecret
    {
        private const string HexSalt = "203C436D249C7304";
        private static readonly byte[] salt1 = Enumerable.Range(0, HexSalt.Length / 2).Select(x => Convert.ToByte(HexSalt.Substring(x * 2, 2), 16)).ToArray();

        public static string DeriveFromPassword(string userManagedPassword)
        {
            const int myIterations = 1000;
            using var k1 = new Rfc2898DeriveBytes(userManagedPassword, salt1, myIterations);
            byte[] edata1 = k1.GetBytes(16);
            string hexdata = BitConverter.ToString(edata1).Replace("-", "");
            k1.Reset();
            return hexdata;
        }
    }
}

using System;
using System.Linq;
using System.Security.Cryptography;

namespace aggregator
{
    static public class SharedSecret
    {
        private const string hexSalt = "203C436D249C7304";
        private static byte[] salt1 = Enumerable.Range(0, hexSalt.Length / 2).Select(x => Convert.ToByte(hexSalt.Substring(x * 2, 2), 16)).ToArray();

        public static string DeriveFromPassword(string userManagedPassword)
        {
            const int myIterations = 1000;
            using (var k1 = new Rfc2898DeriveBytes(userManagedPassword, salt1, myIterations))
            {
                byte[] edata1 = k1.GetBytes(16);
                string hexdata = BitConverter.ToString(edata1).Replace("-", "");
                k1.Reset();
                return hexdata;
            }
        }
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace aggregator.cli
{
    class LogonDataStore
    {
        public LogonDataStore(string tag) => this.Tag = tag;

        public string Tag { get; private set; }
        public byte[] Magic => Encoding.ASCII.GetBytes(Tag);
        protected string LogonDataPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "aggregator-cli",
                    Tag + ".dat");
            }
        }

        public string Save<T>(T data)
        {
            string logonDataString = JsonConvert.SerializeObject(data);
            var logonData = UnicodeEncoding.UTF8.GetBytes(logonDataString);

            var entropy = new byte[16];
            new RNGCryptoServiceProvider().GetBytes(entropy);
            var encryptedData = ProtectedData.Protect(logonData, entropy, DataProtectionScope.CurrentUser);

            string logonADataPath = LogonDataPath;
            // make sure exists
            Directory.CreateDirectory(Path.GetDirectoryName(logonADataPath));

            using (var stream = new FileStream(logonADataPath, FileMode.OpenOrCreate))
            {
                stream.Write(Magic, 0, Magic.Length);
                stream.Write(entropy, 0, entropy.Length);
                stream.Write(encryptedData, 0, encryptedData.Length);
            }

            return logonADataPath;
        }

        public T Load<T>()
            where T : class
        {
            if (!File.Exists(LogonDataPath))
                return null;

            var entropy = new byte[16];
            byte[] outBuffer;
            using (var stream = new FileStream(LogonDataPath, FileMode.Open))
            {
                var magicBuffer = new byte[Magic.Length];
                stream.Read(magicBuffer, 0, magicBuffer.Length);
                if (magicBuffer == Magic)
                    throw new InvalidDataException("Invalid credential file");
                var inBuffer = new byte[stream.Length];
                stream.Read(entropy, 0, entropy.Length);
                stream.Read(inBuffer, 0, inBuffer.Length);
                outBuffer = ProtectedData.Unprotect(inBuffer, entropy, DataProtectionScope.CurrentUser);
            }
            var logonAzureDataString = UnicodeEncoding.UTF8.GetString(outBuffer);
            var result = JsonConvert.DeserializeObject<T>(logonAzureDataString);
            return result;
        }
    }
}

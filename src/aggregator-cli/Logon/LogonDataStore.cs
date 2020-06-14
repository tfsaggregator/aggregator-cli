using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace aggregator.cli
{
    public enum LogonResult
    {
        Succeeded = 0,
        NoLogonData = 1,
        LogonExpired = 2,
    }

    class LogonDataStore
    {
        // TODO is this the right number?
        public static int MaxHoursForCachedCredential = 2;

        public LogonDataStore(string tag)
        {
            this.Tag = tag;

            // add data protection services
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDataProtection();
            var services = serviceCollection.BuildServiceProvider();

            // create an instance of MyClass using the service provider
            protector = ActivatorUtilities.CreateInstance<MyProtector>(services);
        }

        private MyProtector protector;

        public string Tag { get; private set; }
        public char[] Magic => Tag.ToCharArray();
        protected string LogonDataPath => 
            LocalAppData.GetPath(Tag + ".dat");

        public string Save<T>(T data)
            where T : LogonDataBase
        {
            data.Timestamp = DateTime.UtcNow;

            string logonDataString = JsonConvert.SerializeObject(data);

            var encryptedData = protector.Encrypt(logonDataString);

            string logonADataPath = LogonDataPath;
            // make sure exists
            Directory.CreateDirectory(Path.GetDirectoryName(logonADataPath));

            using (var stream = File.CreateText(logonADataPath))
            {
                stream.Write(Magic);
                stream.Write(encryptedData);
            }

            return logonADataPath;
        }

        public (T connection, LogonResult reason) Load<T>()
            where T : LogonDataBase
        {
            if (!File.Exists(LogonDataPath))
                return (null, LogonResult.NoLogonData);

            string logonAzureDataString;
            using (var stream = File.OpenText(LogonDataPath))
            {
                var magicBuffer = new char[Magic.Length];
                stream.Read(magicBuffer, 0, magicBuffer.Length);
                if (magicBuffer == Magic)
                    throw new InvalidDataException("Invalid credential file");
                var encryptedData = stream.ReadToEnd();
                logonAzureDataString = protector.Decrypt(encryptedData);
            }

            var result = JsonConvert.DeserializeObject<T>(logonAzureDataString);
            var elapsed = DateTime.UtcNow - result.Timestamp;
            if (elapsed.TotalHours > MaxHoursForCachedCredential)
            {
                File.Delete(LogonDataPath);
                return (null, LogonResult.LogonExpired);
            }
            else
            {
                return (result, LogonResult.Succeeded);
            }

        }
    }
}

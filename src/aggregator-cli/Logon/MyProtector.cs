using Microsoft.AspNetCore.DataProtection;

namespace aggregator.cli
{
    class MyProtector
    {
        readonly IDataProtector _protector;

        // the 'provider' parameter is provided by DI
        public MyProtector(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector("Aggregator.CLI.Logon.Protector.v1");
        }

        public string Encrypt(string input) => _protector.Protect(input);

        public string Decrypt(string encrypted) => _protector.Unprotect(encrypted);
    }
}

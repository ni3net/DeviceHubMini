using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DeviceHubMini.Security
{
    public static class SecureApiKeyManager
    {
       
        /// <summary>
        /// Loads the API key securely (DPAPI). If not found, bootstraps from env var and stores encrypted.
        /// </summary>
        public static string LoadOrCreateApiKey(string KeyFilePath,string bootstrapKey)
        {
            if (File.Exists(KeyFilePath))
            {
                var encrypted = File.ReadAllBytes(KeyFilePath);
                var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.LocalMachine);
                return Encoding.UTF8.GetString(decrypted);
            }

            // Use bootstrap key if provided
            if (string.IsNullOrWhiteSpace(bootstrapKey))
                throw new InvalidOperationException("API key missing. Pass it during installation or first startup.");

            SaveApiKey(KeyFilePath,bootstrapKey);
            return bootstrapKey;
        }


        /// <summary>
        /// Encrypts and stores the API key using DPAPI.
        /// </summary>
        public static void SaveApiKey(string KeyFilePath, string apiKey)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(KeyFilePath)!);

            var data = Encoding.UTF8.GetBytes(apiKey);
            var encrypted = ProtectedData.Protect(data, null, DataProtectionScope.LocalMachine);
            File.WriteAllBytes(KeyFilePath, encrypted);
        }

        public static string GetApiKeysFromArugments(string[] args)
        {
            string? bootstrapApiKey = string.Empty;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("--apikey", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    bootstrapApiKey = args[i + 1];
                    break;
                }
            }
            return bootstrapApiKey;
        }
    }
}

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
        public static string LoadOrCreateApiKey(string keyFilePath, string bootstrapKey)
        {
            // 1. If key already stored → load it
            if (File.Exists(keyFilePath))
            {
                var encrypted = File.ReadAllBytes(keyFilePath);

                if (OperatingSystem.IsWindows())
                {
                    var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.LocalMachine);
                    return Encoding.UTF8.GetString(decrypted);
                }
                else
                {
                    // Non-Windows: return as plain text (or AES decrypt)
                    return Encoding.UTF8.GetString(encrypted);
                }
            }

            // 2. First-time initialization
            if (string.IsNullOrWhiteSpace(bootstrapKey))
                throw new InvalidOperationException(
                    "API key missing. Provide key during installation or first startup."
                );

            // 3. Save key securely (Windows) or plain (non-Windows)
            SaveApiKey(keyFilePath, bootstrapKey);

            return bootstrapKey;
        }



        /// <summary>
        /// Encrypts and stores the API key using DPAPI.
        /// </summary>
        public static void SaveApiKey(string keyFilePath, string apiKey)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(keyFilePath)!);
            var data = Encoding.UTF8.GetBytes(apiKey);

            byte[] toWrite;

            if (OperatingSystem.IsWindows())
            {
                toWrite = ProtectedData.Protect(data, null, DataProtectionScope.LocalMachine);
            }
            else
            {
                // On Linux / Docker store raw text or implement AES
                toWrite = data;
            }

            File.WriteAllBytes(keyFilePath, toWrite);
        }


        public static string GetApiKeysFromArugments(string[] args)
        {
            if (args != null && args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
                return args[0].Trim();

            return string.Empty;
        }
    }
}

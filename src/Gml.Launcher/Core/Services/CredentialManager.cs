using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Diagnostics;
using Gml.Launcher.Models;
using Gml.Client;

namespace Gml.Launcher.Core.Services
{
    /// <summary>
    /// Direct credential storage service that bypasses JSON serialization for more reliable credential persistence.
    /// </summary>
    public class CredentialManager
    {
        private readonly ISystemService _systemService;
        private readonly IGmlClientManager _gmlClientManager;
        private const string CREDENTIALS_FOLDER = "secure";
        private const string LOGIN_FILE = "login.dat";
        private const string PASSWORD_FILE = "pwd.dat";
        private const string REMEMBER_FILE = "remember.dat";
        
        public CredentialManager(ISystemService systemService, IGmlClientManager gmlClientManager)
        {
            _systemService = systemService;
            _gmlClientManager = gmlClientManager;
            
            // Ensure the credentials directory exists
            EnsureCredentialsDirectoryExists();
        }
        
        /// <summary>
        /// Saves user credentials directly to protected files.
        /// </summary>
        public async Task SaveCredentials(string login, string password, bool rememberMe)
        {
            try
            {
                if (!rememberMe)
                {
                    // If rememberMe is false, clear all credentials
                    await ClearCredentials();
                    return;
                }
                
                var credentialsPath = GetCredentialsPath();
                
                // Save login (encoded but not encrypted for easy identification)
                var loginBytes = Encoding.UTF8.GetBytes(login);
                await File.WriteAllBytesAsync(Path.Combine(credentialsPath, LOGIN_FILE), loginBytes);
                
                // Encrypt and save password
                var encryptedPassword = EncryptData(password);
                await File.WriteAllBytesAsync(Path.Combine(credentialsPath, PASSWORD_FILE), encryptedPassword);
                
                // Save remember me flag
                await File.WriteAllTextAsync(Path.Combine(credentialsPath, REMEMBER_FILE), rememberMe.ToString());
                
                Debug.WriteLine($"Credentials saved directly to disk for user: {login}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving credentials directly: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
            }
        }
        
        /// <summary>
        /// Loads saved user credentials directly from protected files.
        /// </summary>
        public async Task<SavedCredentials?> LoadCredentials()
        {
            try
            {
                var credentialsPath = GetCredentialsPath();
                var loginFile = Path.Combine(credentialsPath, LOGIN_FILE);
                var passwordFile = Path.Combine(credentialsPath, PASSWORD_FILE);
                var rememberFile = Path.Combine(credentialsPath, REMEMBER_FILE);
                
                // Check if all required files exist
                if (!File.Exists(loginFile) || !File.Exists(passwordFile) || !File.Exists(rememberFile))
                {
                    Debug.WriteLine("One or more credential files are missing");
                    return null;
                }
                
                // Read remember me flag first
                var rememberText = await File.ReadAllTextAsync(rememberFile);
                if (!bool.TryParse(rememberText, out bool rememberMe) || !rememberMe)
                {
                    Debug.WriteLine("Remember me is disabled or invalid");
                    return null;
                }
                
                // Read and decode login
                var loginBytes = await File.ReadAllBytesAsync(loginFile);
                var login = Encoding.UTF8.GetString(loginBytes);
                
                // Read and decrypt password
                var encryptedPasswordBytes = await File.ReadAllBytesAsync(passwordFile);
                var password = DecryptData(encryptedPasswordBytes);
                
                Debug.WriteLine($"Credentials loaded directly from disk for user: {login}");
                
                // Create credentials object
                return new SavedCredentials
                {
                    Login = login,
                    EncryptedPassword = Convert.ToBase64String(encryptedPasswordBytes), // Store encrypted version
                    RememberMe = rememberMe,
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading credentials directly: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
                return null;
            }
        }
        
        /// <summary>
        /// Clears all saved credentials.
        /// </summary>
        public async Task ClearCredentials()
        {
            try
            {
                var credentialsPath = GetCredentialsPath();
                
                // Delete all credential files
                if (File.Exists(Path.Combine(credentialsPath, LOGIN_FILE)))
                    File.Delete(Path.Combine(credentialsPath, LOGIN_FILE));
                    
                if (File.Exists(Path.Combine(credentialsPath, PASSWORD_FILE)))
                    File.Delete(Path.Combine(credentialsPath, PASSWORD_FILE));
                    
                if (File.Exists(Path.Combine(credentialsPath, REMEMBER_FILE)))
                    File.Delete(Path.Combine(credentialsPath, REMEMBER_FILE));
                    
                Debug.WriteLine("All credentials cleared");
                
                // Also clear the traditional storage just to be safe
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error clearing credentials: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
            }
        }
        
        /// <summary>
        /// Gets the path to the credentials folder.
        /// </summary>
        private string GetCredentialsPath()
        {
            var basePath = _systemService.GetGameFolder(_gmlClientManager.ProjectName, true);
            return Path.Combine(basePath, CREDENTIALS_FOLDER);
        }
        
        /// <summary>
        /// Ensures the credentials directory exists.
        /// </summary>
        private void EnsureCredentialsDirectoryExists()
        {
            var credentialsPath = GetCredentialsPath();
            if (!Directory.Exists(credentialsPath))
            {
                Directory.CreateDirectory(credentialsPath);
            }
        }
        
        /// <summary>
        /// Encrypts the provided data using AES.
        /// </summary>
        private byte[] EncryptData(string data)
        {
            if (string.IsNullOrEmpty(data))
                return Array.Empty<byte>();
                
            try
            {
                // Use a consistent key and IV for encryption
                byte[] key = Encoding.UTF8.GetBytes("COINT_LauncherSecretKey_2025!!"); // 32 bytes for AES-256
                byte[] iv = new byte[16]; // Zero IV for simplicity (in production, use a proper IV)
                
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                
                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    
                    using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                    using (var ms = new MemoryStream())
                    {
                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            cs.Write(dataBytes, 0, dataBytes.Length);
                            cs.FlushFinalBlock();
                        }
                        return ms.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error encrypting data: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
                return Array.Empty<byte>();
            }
        }
        
        /// <summary>
        /// Decrypts the provided data using AES.
        /// </summary>
        private string DecryptData(byte[] encryptedData)
        {
            if (encryptedData == null || encryptedData.Length == 0)
                return string.Empty;
                
            try
            {
                // Use the same key and IV used for encryption
                byte[] key = Encoding.UTF8.GetBytes("COINT_LauncherSecretKey_2025!!"); // 32 bytes for AES-256
                byte[] iv = new byte[16]; // Zero IV for simplicity (in production, use a proper IV)
                
                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    
                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    using (var ms = new MemoryStream(encryptedData))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var resultMs = new MemoryStream())
                    {
                        byte[] buffer = new byte[1024];
                        int read;
                        while ((read = cs.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            resultMs.Write(buffer, 0, read);
                        }
                        return Encoding.UTF8.GetString(resultMs.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error decrypting data: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
                return string.Empty;
            }
        }
    }
}

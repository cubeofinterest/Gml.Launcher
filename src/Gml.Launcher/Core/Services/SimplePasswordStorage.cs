using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Diagnostics;

namespace Gml.Launcher.Core.Services
{
    /// <summary>
    /// Ultra-simple password storage with minimal dependencies
    /// </summary>
    public static class SimplePasswordStorage
    {
        private const string SIMPLE_FOLDER = "cred";
        private const string SIMPLE_LOGIN_FILE = "user.bin";
        private const string SIMPLE_PWD_FILE = "pwd.bin";
        private const string SIMPLE_REMEMBER_FILE = "remember.bin";
        
        // Fixed encryption key - in a real application, this would be more secure
        private static readonly byte[] KEY = Encoding.UTF8.GetBytes("COINT_Launcher_Simple_Password_Storage!!");
        private static readonly byte[] IV = new byte[16]; // All zeros for simplicity
        
        /// <summary>
        /// Ensures the storage directory exists
        /// </summary>
        private static string EnsureDirectoryExists()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "LumenMD", SIMPLE_FOLDER);
            
            if (!Directory.Exists(dir))
            {
                try
                {
                    Directory.CreateDirectory(dir);
                    Debug.WriteLine($"Created simple password directory at {dir}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to create password directory: {ex.Message}");
                }
            }
            
            return dir;
        }
        
        /// <summary>
        /// Simple function to save the username
        /// </summary>
        public static void SaveUsername(string username)
        {
            if (string.IsNullOrEmpty(username)) return;
            
            try
            {
                var dir = EnsureDirectoryExists();
                var file = Path.Combine(dir, SIMPLE_LOGIN_FILE);
                
                File.WriteAllText(file, username);
                Debug.WriteLine($"Saved username: {username}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving username: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Simple function to load the username
        /// </summary>
        public static string LoadUsername()
        {
            try
            {
                var dir = EnsureDirectoryExists();
                var file = Path.Combine(dir, SIMPLE_LOGIN_FILE);
                
                if (File.Exists(file))
                {
                    var username = File.ReadAllText(file);
                    Debug.WriteLine($"Loaded username: {username}");
                    return username;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading username: {ex.Message}");
            }
            
            return string.Empty;
        }
        
        /// <summary>
        /// Simple function to save the password (with basic encryption)
        /// </summary>
        public static void SavePassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return;
            
            try
            {
                var dir = EnsureDirectoryExists();
                var file = Path.Combine(dir, SIMPLE_PWD_FILE);
                
                // Basic encryption
                var encrypted = EncryptString(password);
                
                File.WriteAllBytes(file, encrypted);
                Debug.WriteLine("Password saved successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving password: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Simple function to load the password (with basic decryption)
        /// </summary>
        public static string LoadPassword()
        {
            try
            {
                var dir = EnsureDirectoryExists();
                var file = Path.Combine(dir, SIMPLE_PWD_FILE);
                
                if (File.Exists(file))
                {
                    var encryptedBytes = File.ReadAllBytes(file);
                    var password = DecryptBytes(encryptedBytes);
                    Debug.WriteLine($"Loaded password, length: {password.Length}");
                    return password;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading password: {ex.Message}");
            }
            
            return string.Empty;
        }
        
        /// <summary>
        /// Save the "remember me" flag
        /// </summary>
        public static void SaveRememberMe(bool remember)
        {
            try
            {
                var dir = EnsureDirectoryExists();
                var file = Path.Combine(dir, SIMPLE_REMEMBER_FILE);
                
                File.WriteAllText(file, remember.ToString());
                Debug.WriteLine($"Saved remember flag: {remember}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving remember flag: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Load the "remember me" flag
        /// </summary>
        public static bool LoadRememberMe()
        {
            try
            {
                var dir = EnsureDirectoryExists();
                var file = Path.Combine(dir, SIMPLE_REMEMBER_FILE);
                
                if (File.Exists(file))
                {
                    var text = File.ReadAllText(file);
                    if (bool.TryParse(text, out bool result))
                    {
                        Debug.WriteLine($"Loaded remember flag: {result}");
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading remember flag: {ex.Message}");
            }
            
            return false;
        }
        
        /// <summary>
        /// Clear all saved credentials
        /// </summary>
        public static void ClearAll()
        {
            try
            {
                var dir = EnsureDirectoryExists();
                
                var loginFile = Path.Combine(dir, SIMPLE_LOGIN_FILE);
                var pwdFile = Path.Combine(dir, SIMPLE_PWD_FILE);
                var rememberFile = Path.Combine(dir, SIMPLE_REMEMBER_FILE);
                
                if (File.Exists(loginFile)) File.Delete(loginFile);
                if (File.Exists(pwdFile)) File.Delete(pwdFile);
                if (File.Exists(rememberFile)) File.Delete(rememberFile);
                
                Debug.WriteLine("All simple credentials cleared");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error clearing credentials: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Basic string encryption
        /// </summary>
        private static byte[] EncryptString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return Array.Empty<byte>();
                
            using (Aes aes = Aes.Create())
            {
                aes.Key = KEY;
                aes.IV = IV;
                
                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }
                    
                    return ms.ToArray();
                }
            }
        }
        
        /// <summary>
        /// Basic bytes decryption to string
        /// </summary>
        private static string DecryptBytes(byte[] cipherText)
        {
            if (cipherText == null || cipherText.Length == 0)
                return string.Empty;
                
            using (Aes aes = Aes.Create())
            {
                aes.Key = KEY;
                aes.IV = IV;
                
                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream(cipherText))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }
    }
}
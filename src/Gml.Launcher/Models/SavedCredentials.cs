using System;
using System.Security.Cryptography;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;

namespace Gml.Launcher.Models;

/// <summary>
/// Модель для хранения сохраненных учетных данных пользователя
/// </summary>
// Class for storing saved user credentials
public class SavedCredentials
{
    public string Login { get; set; } = string.Empty;
    
    // We'll add JsonIgnore to the raw password field to prevent accidental serialization
    [JsonIgnore]
    private string _rawPassword = string.Empty;

    // This property will be serialized
    public string EncryptedPassword { get; set; } = string.Empty;
    
    // Store serialized password flag
    public bool HasPassword { get; set; }
    
    // Create backup property to ensure data is retained
    [JsonIgnore]
    public string DecryptedPassword => GetDecryptedPassword();
    
    public bool RememberMe { get; set; }
    
    // Метод для создания экземпляра с зашифрованным паролем
    public static SavedCredentials Create(string login, string password, bool rememberMe)
    {
        Debug.WriteLine($"Creating SavedCredentials for user: {login}, RememberMe: {rememberMe}");
        var encryptedPass = EncryptPassword(password ?? string.Empty);
        Debug.WriteLine($"Password encrypted, length: {encryptedPass?.Length ?? 0}");
        
        return new SavedCredentials
        {
            Login = login ?? string.Empty,
            EncryptedPassword = encryptedPass ?? string.Empty,
            RememberMe = rememberMe,
            HasPassword = !string.IsNullOrEmpty(encryptedPass),
            _rawPassword = password ?? string.Empty // Store raw password temporarily for debugging
        };
    }
    
    // Метод для дешифрования пароля
    public string GetDecryptedPassword()
    {
        Debug.WriteLine($"Getting decrypted password for user {Login}, HasPassword: {HasPassword}");
        Debug.WriteLine($"EncryptedPassword length: {EncryptedPassword?.Length ?? 0}");
        
        // If we have the raw password, use it directly (helps with debugging)
        if (!string.IsNullOrEmpty(_rawPassword))
        {
            Debug.WriteLine("Using cached raw password");
            return _rawPassword;
        }
        
        if (string.IsNullOrEmpty(EncryptedPassword))
        {
            Debug.WriteLine("EncryptedPassword is empty, returning empty string");
            return string.Empty;
        }
        
        try
        {
            var decrypted = DecryptPassword(EncryptedPassword);
            Debug.WriteLine($"Password decrypted successfully, length: {decrypted.Length}");
            return decrypted;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error decrypting password: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            return string.Empty;
        }
    }
    
    // Простое шифрование пароля
    // Make this public so it can be accessed from outside
    public static string EncryptPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            Debug.WriteLine("Cannot encrypt empty password");
            return string.Empty;
        }
            
        try
        {
            Debug.WriteLine($"Encrypting password, length: {password.Length}");
            // Используем простое обратимое шифрование для демонстрации
            // В реальном приложении следует использовать более надежные методы
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] protectedBytes = Protect(passwordBytes);
            var result = Convert.ToBase64String(protectedBytes);
            Debug.WriteLine($"Password encrypted successfully, result length: {result.Length}");
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error encrypting password: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            return string.Empty;
        }
    }
    
    // Make this public so it can be accessed from outside
    public static string DecryptPassword(string encryptedPassword)
    {
        if (string.IsNullOrEmpty(encryptedPassword))
        {
            Debug.WriteLine("Cannot decrypt empty encrypted password");
            return string.Empty;
        }
        
        try
        {
            Debug.WriteLine($"Decrypting password, encrypted length: {encryptedPassword.Length}");
            byte[] protectedBytes = Convert.FromBase64String(encryptedPassword);
            byte[] passwordBytes = Unprotect(protectedBytes);
            var result = Encoding.UTF8.GetString(passwordBytes);
            Debug.WriteLine($"Password decrypted successfully, result length: {result.Length}");
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error decrypting password: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            return string.Empty;
        }
    }
    
    // Простая защита данных - в реальном приложении используйте более надежные методы
    private static byte[] Protect(byte[] data)
    {
        // Используем ключ для шифрования (в реальном приложении храните его безопасно)
        byte[] key = Encoding.UTF8.GetBytes("COINT_LauncherSecretKey_2025!!"); // 32 байта для AES-256
        byte[] iv = new byte[16]; // Инициализационный вектор
        
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            
            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            using (var ms = new System.IO.MemoryStream())
            {
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.FlushFinalBlock();
                }
                return ms.ToArray();
            }
        }
    }
    
    private static byte[] Unprotect(byte[] protectedData)
    {
        // Используем тот же ключ для дешифрования
        byte[] key = Encoding.UTF8.GetBytes("COINT_LauncherSecretKey_2025!!"); // 32 байта для AES-256
        byte[] iv = new byte[16]; // Инициализационный вектор
        
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            
            using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            using (var ms = new System.IO.MemoryStream(protectedData))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (var resultMs = new System.IO.MemoryStream())
            {
                byte[] buffer = new byte[1024];
                int read;
                while ((read = cs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    resultMs.Write(buffer, 0, read);
                }
                return resultMs.ToArray();
            }
        }
    }
}
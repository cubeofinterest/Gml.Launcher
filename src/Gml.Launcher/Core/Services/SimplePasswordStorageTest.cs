using System;
using System.Diagnostics;
using System.IO;

namespace Gml.Launcher.Core.Services
{
    /// <summary>
    /// Simple test class for SimplePasswordStorage
    /// This can be used to manually verify functionality
    /// </summary>
    public static class SimplePasswordStorageTest
    {
        public static void RunTest()
        {
            Debug.WriteLine("----------------------------------------------");
            Debug.WriteLine("STARTING SIMPLE PASSWORD STORAGE TEST");
            Debug.WriteLine("----------------------------------------------");
            
            // Test saving credentials
            const string testUsername = "testuser@example.com";
            const string testPassword = "Password123!";
            const bool testRemember = true;
            
            Debug.WriteLine($"Saving test credentials - Username: {testUsername}, Password length: {testPassword.Length}");
            
            // Clear existing data first
            SimplePasswordStorage.ClearAll();
            Debug.WriteLine("Cleared all existing credentials");
            
            // Save credentials
            SimplePasswordStorage.SaveUsername(testUsername);
            SimplePasswordStorage.SavePassword(testPassword);
            SimplePasswordStorage.SaveRememberMe(testRemember);
            Debug.WriteLine("Saved test credentials");
            
            // Read back and verify
            var savedUsername = SimplePasswordStorage.LoadUsername();
            var savedPassword = SimplePasswordStorage.LoadPassword();
            var savedRemember = SimplePasswordStorage.LoadRememberMe();
            
            Debug.WriteLine("----------------------------------------------");
            Debug.WriteLine("VERIFICATION RESULTS:");
            Debug.WriteLine($"Username matches: {savedUsername == testUsername} (Expected: {testUsername}, Got: {savedUsername})");
            Debug.WriteLine($"Password matches: {savedPassword == testPassword} (Expected length: {testPassword.Length}, Got length: {savedPassword.Length})");
            Debug.WriteLine($"Remember flag matches: {savedRemember == testRemember} (Expected: {testRemember}, Got: {savedRemember})");
            Debug.WriteLine("----------------------------------------------");
            
            // Verify binary content of files
            VerifyFileContents();
            
            // Test clearing credentials
            TestClearingCredentials();
            
            Debug.WriteLine("----------------------------------------------");
            Debug.WriteLine("TEST COMPLETED");
            Debug.WriteLine("----------------------------------------------");
        }
        
        private static void VerifyFileContents()
        {
            // Get storage location
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "LumenMD", "cred");
            Debug.WriteLine($"Storage location: {dir}");
            
            if (!Directory.Exists(dir))
            {
                Debug.WriteLine("ERROR: Storage directory does not exist!");
                return;
            }
            
            // List files in storage directory
            var files = Directory.GetFiles(dir);
            Debug.WriteLine($"Files in storage directory ({files.Length}):");
            
            bool foundUserFile = false;
            bool foundPwdFile = false;
            bool foundRememberFile = false;
            
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                Debug.WriteLine($"- {fileInfo.Name} ({fileInfo.Length} bytes)");
                
                // Check for expected files
                if (fileInfo.Name == "user.bin") foundUserFile = true;
                if (fileInfo.Name == "pwd.bin") foundPwdFile = true;
                if (fileInfo.Name == "remember.bin") foundRememberFile = true;
            }
            
            Debug.WriteLine($"Found user.bin: {foundUserFile}");
            Debug.WriteLine($"Found pwd.bin: {foundPwdFile}");
            Debug.WriteLine($"Found remember.bin: {foundRememberFile}");
            
            if (!foundUserFile || !foundPwdFile || !foundRememberFile)
            {
                Debug.WriteLine("ERROR: Some expected files are missing!");
            }
            else
            {
                Debug.WriteLine("All expected files were found.");
            }
        }
        
        private static void TestClearingCredentials()
        {
            Debug.WriteLine("\nTesting clearing credentials...");
            
            // Clear all credentials
            SimplePasswordStorage.ClearAll();
            Debug.WriteLine("Cleared all credentials");
            
            // Verify everything is cleared
            var username = SimplePasswordStorage.LoadUsername();
            var password = SimplePasswordStorage.LoadPassword();
            var remember = SimplePasswordStorage.LoadRememberMe();
            
            Debug.WriteLine($"After clearing - Username: '{username}', Password length: {password.Length}, Remember: {remember}");
            
            if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password) && !remember)
            {
                Debug.WriteLine("Clearing credentials test: PASSED ✅");
            }
            else
            {
                Debug.WriteLine("Clearing credentials test: FAILED ❌");
            }
        }
    }
}
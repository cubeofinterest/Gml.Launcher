using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Gml.Client;
using Gml.Launcher.Core.Exceptions;
using Gml.Launcher.Models;
using Splat;
using SQLite;

namespace Gml.Launcher.Core.Services;

public class LocalStorageService : IStorageService
{
    private const string DatabaseFileName = "data.db";
    private readonly SQLiteAsyncConnection _database;

    public LocalStorageService(ISystemService? systemService = null, IGmlClientManager? gmlClient = null)
    {
        var gmlClientManager = gmlClient
                               ?? Locator.Current.GetService<IGmlClientManager>()
                               ?? throw new ServiceNotFoundException(typeof(IGmlClientManager));

        var systemServiceDependency = systemService
                                      ?? Locator.Current.GetService<ISystemService>()
                                      ?? throw new ServiceNotFoundException(typeof(ISystemService));

        var databasePath = Path.Combine(systemServiceDependency.GetGameFolder(gmlClientManager.ProjectName, true),
            DatabaseFileName);

        _database = new SQLiteAsyncConnection(databasePath);

        InitializeTables();
    }

    public async Task SetAsync<T>(string key, T value, CancellationToken? token = default)
    {
        // Create special serialization options for credentials
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            IgnoreReadOnlyProperties = false,
            PropertyNameCaseInsensitive = true
        };

        // Special handling for SavedCredentials to ensure password is properly saved
        if (value is Models.SavedCredentials credentials)
        {
            Debug.WriteLine($"Saving credentials for {credentials.Login}, HasPassword: {credentials.HasPassword}, EncryptedPassword length: {credentials.EncryptedPassword?.Length ?? 0}");
        }

        var serializedValue = JsonSerializer.Serialize(value, options) ?? string.Empty;
        
        // Debug logging for saved credentials
        if (key == StorageConstants.SavedCredentials)
        {
            Debug.WriteLine($"Serialized credentials JSON length: {serializedValue?.Length ?? 0}");
        }
        
        var storageItem = new StorageItem
        {
            Key = key,
            TypeName = typeof(T).FullName ?? typeof(T).Name,
            Value = serializedValue
        };

        await _database.InsertOrReplaceAsync(storageItem);
        
        // Verify credentials were saved properly for debugging
        if (key == StorageConstants.SavedCredentials)
        {
            var verifyItem = await _database.Table<StorageItem>()
                .Where(si => si.Key == key)
                .FirstOrDefaultAsync();
                
            if (verifyItem != null)
            {
                Debug.WriteLine($"Verified serialized data in database, length: {verifyItem.Value?.Length ?? 0}");
            }
            else
            {
                Debug.WriteLine("WARNING: Failed to retrieve saved credentials from database for verification");
            }
        }
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var storageItem = await _database.Table<StorageItem>()
            .Where(si => si.Key == key)
            .FirstOrDefaultAsync();

        if (storageItem?.Value != null) 
        {
            // Special handling for credentials with debug logging
            if (key == StorageConstants.SavedCredentials)
            {
                Debug.WriteLine($"Retrieving saved credentials, JSON length: {storageItem.Value?.Length ?? 0}");
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                    IgnoreReadOnlyProperties = false,
                    PropertyNameCaseInsensitive = true
                };
                
                var result = JsonSerializer.Deserialize<T>(storageItem.Value, options);
                
                if (result is Models.SavedCredentials credentials)
                {
                    Debug.WriteLine($"Deserialized credentials: Login={credentials.Login}, HasPassword={credentials.HasPassword}, EncryptedPassword length={credentials.EncryptedPassword?.Length ?? 0}");
                }
                
                return result;
            }
            
            return JsonSerializer.Deserialize<T>(storageItem.Value ?? string.Empty);
        }

        return default;
    }

    public Task<int> SaveRecord<T>(T record)
    {
        return _database.InsertOrReplaceAsync(record);
    }

    public async Task<string> GetLogsAsync(int rowCount = 100)
    {
        var logs = await _database.Table<LogsItem>().Take(rowCount).ToListAsync();

        return string.Join("\n", logs.Select(c => c.Message));
    }

    private void InitializeTables()
    {
        _database.CreateTableAsync<StorageItem>().Wait();
        _database.CreateTableAsync<LogsItem>().Wait();
    }

    [Table("StorageItems")]
    private class StorageItem
    {
        [PrimaryKey] public string Key { get; init; } = null!;
        public string? TypeName { get; set; }
        public string Value { get; init; } = null!;
    }

    [Table("Logs")]
    private class LogsItem
    {
        [PrimaryKey] public string Date { get; set; } = null!;
        public string? Message { get; set; }
        public string StackTrace { get; set; } = null!;
    }
}

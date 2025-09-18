using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia;
using Gml.Client;
using Gml.Launcher.Assets;
using Gml.Launcher.Core.Services;
using Gml.Launcher.Models;
using Sentry;
using Splat;

namespace Gml.Launcher.Core.Extensions;

public static class ServiceLocator
{
    public static AppBuilder RegisterServices(this AppBuilder builder, string[] arguments)
    {
        var systemService = new SystemService();

        var installationDirectory =
            Path.Combine(systemService.GetApplicationFolder(), ResourceKeysDictionary.FolderName);

        RegisterLocalizationService();
        RegisterSystemService(systemService);
        
        // Регистрируем универсальный сервис логирования
        RegisterLoggingService();
        
        RegisterLogHelper(systemService);
        var manager = RegisterGmlManager(systemService, installationDirectory, arguments);
        var storageService = RegisterStorage();
        
        // Register the new credential manager
        RegisterCredentialManager(systemService, manager);

        CheckAndChangeInstallationFolder(storageService, manager);
        CheckAndChangeLanguage(storageService, systemService);
        
        // Register DevLoggingService for debug builds
#if DEBUG
        var devLoggingService = new DevLoggingService();
        Locator.CurrentMutable.RegisterConstant(devLoggingService, typeof(IDevLoggingService));
        
        // Configure HttpClient factory for network logging
        ConfigureGlobalHttpClientLogging(devLoggingService);
#endif
        
        Locator.CurrentMutable.RegisterConstant(new BackendChecker(), typeof(IBackendChecker));
        Locator.CurrentMutable.RegisterConstant(new SettingsService(
                GetRequiredService<ISystemService>(),
                GetRequiredService<IStorageService>()
            ),
            typeof(ISettingsService)
        );

        // Регистрируем сервис проверки сессии
        Locator.CurrentMutable.RegisterConstant(new SessionValidationService(
                GetRequiredService<IGmlClientManager>(),
                GetRequiredService<IStorageService>(),
                GetRequiredService<ISettingsService>()
            ),
            typeof(ISessionValidationService)
        );

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            // Логируем необработанные исключения
            var loggingService = Locator.Current.GetService<ILoggingService>();
            loggingService?.LogCritical("Unhandled exception", ((Exception)args.ExceptionObject).ToString());
            
            SentrySdk.CaptureException((Exception)args.ExceptionObject);
        };

        Debug.WriteLine($"[Gml][{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Configuring ended");

        return builder;
    }

    private static T GetRequiredService<T>()
    {
        return Locator.Current.GetService<T>() ?? throw new Exception();
    }

    private static void RegisterLoggingService()
    {
        var loggingService = new LoggingService();
        Locator.CurrentMutable.RegisterConstant(loggingService, typeof(ILoggingService));
        
        // Логируем успешную инициализацию
        loggingService.LogLauncherUpdate("Logging service registered", 
            $"Build mode: {(loggingService != null ? "ENABLED" : "DISABLED")}");
    }

    private static void RegisterLogHelper(SystemService systemService)
    {
        Locator.CurrentMutable.RegisterConstant(new LogHandler());
    }

    private static void CheckAndChangeLanguage(LocalStorageService storageService, SystemService systemService)
    {
        // Set Russian language regardless of saved settings
        Assets.Resources.Resources.Culture = new CultureInfo("ru-RU");
    }

    private static LocalStorageService RegisterStorage()
    {
        var storageService = new LocalStorageService();
        Locator.CurrentMutable.RegisterConstant(storageService, typeof(IStorageService));

        return storageService;
    }

    private static void CheckAndChangeInstallationFolder(LocalStorageService storageService, GmlClientManager manager)
    {
        var installationDirectory = storageService.GetAsync<string>(StorageConstants.InstallationDirectory).Result;

        if (!string.IsNullOrEmpty(installationDirectory)) manager.ChangeInstallationFolder(installationDirectory);
    }

    private static GmlClientManager RegisterGmlManager(SystemService systemService, string installationDirectory,
        string[] arguments)
    {
        var manager = new GmlClientManager(installationDirectory, ResourceKeysDictionary.Host,
            ResourceKeysDictionary.FolderName,
            systemService.GetOsType());
#if DEBUG
        manager.SkipUpdate = true;
#else
        manager.SkipUpdate = arguments.Contains("-skip-update");
#endif

        Locator.CurrentMutable.RegisterConstant(manager, typeof(IGmlClientManager));

        return manager;
    }

    private static void RegisterSystemService(SystemService systemService)
    {
        Locator.CurrentMutable.RegisterConstant(systemService, typeof(ISystemService));
    }
    
    private static void RegisterCredentialManager(SystemService systemService, GmlClientManager manager)
    {
        var credentialManager = new CredentialManager(systemService, manager);
        Locator.CurrentMutable.RegisterConstant(credentialManager, typeof(CredentialManager));
        Debug.WriteLine($"[Gml][{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Credential Manager registered");
    }

    private static void RegisterLocalizationService()
    {
        var service = new ResourceLocalizationService();
        Locator.CurrentMutable.RegisterConstant(service, typeof(ILocalizationService));
    }

#if DEBUG
    private static void ConfigureGlobalHttpClientLogging(DevLoggingService devLoggingService)
    {
        try
        {
            // Logging configuration message
            devLoggingService.LogLauncherUpdate("NetworkLogging", "Configured global HTTP client logging for development mode");
            
            // Setup initial messages to show it's working
            devLoggingService.LogNetworkRequest("SYSTEM", "[Startup Configuration]", 
                "User-Agent: COINT.Launcher-Client/1.0", 
                "Network logging initialized successfully");
            
            // Create a default client with the User-Agent header to show it works
            var client = new System.Net.Http.HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "COINT.Launcher-Client/1.0");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to configure HTTP client logging: {ex.Message}");
        }
    }
#endif
}

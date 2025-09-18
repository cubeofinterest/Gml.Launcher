using System;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using Avalonia;
using Avalonia.ReactiveUI;
using Gml.Client;
using Gml.Launcher.Assets;
using Gml.Launcher.Core.Extensions;
using Gml.Launcher.Core.Services;
using ReactiveUI;
using Sentry;
using Splat;

namespace Gml.Launcher;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        ILoggingService? loggingService = null;

        try
        {
            Debug.WriteLine($"[Gml][{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Application started");

#if DEBUG
            Console.WriteLine("===========================================");
            Console.WriteLine("  GML LAUNCHER - DEVELOPMENT VERSION");
            Console.WriteLine("===========================================");
            Console.WriteLine("DETAILED FILE LOGGING IS ENABLED!");
            Console.WriteLine("The following operations will be logged:");
            Console.WriteLine("- All file downloads from server");
            Console.WriteLine("- File validation and hash checking");
            Console.WriteLine("- Directory creation operations");
            Console.WriteLine("- Profile and mod actions");
            Console.WriteLine("- Minecraft client file operations");
            Console.WriteLine("- Network requests and responses");
            Console.WriteLine("===========================================");
            Console.WriteLine();
#else
            Console.WriteLine("===========================================");
            Console.WriteLine("  GML LAUNCHER - RELEASE VERSION");
            Console.WriteLine("===========================================");
            Console.WriteLine("ERROR & CRITICAL LOGGING IS ENABLED!");
            Console.WriteLine("The following will be logged:");
            Console.WriteLine("- Critical errors and exceptions");
            Console.WriteLine("- Failed file operations");
            Console.WriteLine("- Network errors");
            Console.WriteLine("- Profile and mod actions");
            Console.WriteLine("===========================================");
            Console.WriteLine();
#endif

            //InitializeSentry();
            RxApp.DefaultExceptionHandler = Observer.Create<Exception>(ex =>
            {
                // Логируем глобальные исключения
                loggingService?.LogException(ex, "Global exception handler");
                SentrySdk.CaptureException(ex);
            });

            var app = BuildAvaloniaApp(args);

            // Получаем сервис логирования после инициализации DI
            loggingService = Locator.Current.GetService<ILoggingService>();
            loggingService?.LogLauncherUpdate("Application startup", "Avalonia app built successfully");

            // Показываем путь к лог файлу
            var logPath = loggingService?.GetLogFilePath();
            if (!string.IsNullOrEmpty(logPath))
            {
                Console.WriteLine($"Log file: {logPath}");
                Console.WriteLine();
            }

            app.StartWithClassicDesktopLifetime(args);

            loggingService?.LogLauncherUpdate("Application shutdown", "Application finished normally");
        }
        catch (Exception exception)
        {
            loggingService?.LogCritical("Critical startup error", exception.ToString());

            SentrySdk.CaptureException(exception);
            Console.WriteLine($"CRITICAL ERROR: {exception}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }

    private static void GlobalExceptionHandler(Exception exception)
    {
        var loggingService = Locator.Current.GetService<ILoggingService>();
        loggingService?.LogException(exception, "Global exception");
        SentrySdk.CaptureException(exception);
    }

    public static AppBuilder BuildAvaloniaApp(string[] args)
    {
        Debug.WriteLine($"[Gml][{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Configuring launcher");

        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .RegisterServices(args)
            .LogToTrace()
            .UseReactiveUI();
    }

    private static void InitializeSentry()
    {
        Debug.WriteLine($"[Gml][{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Start sentry initialization");
        var sentryUrl = GmlClientManager.GetSentryLink(ResourceKeysDictionary.Host).Result;

        try
        {
            if (!string.IsNullOrEmpty(sentryUrl))
                SentrySdk.Init(options =>
                {
                    options.Dsn = sentryUrl;
#if DEBUG
                    options.Debug = true;
#endif
                    options.TracesSampleRate = 1.0;
                    options.DiagnosticLevel = SentryLevel.Debug;
                    options.IsGlobalModeEnabled = true;
                    options.SendDefaultPii = true;
                    options.MaxAttachmentSize = 10 * 1024 * 1024;
                });

            Debug.WriteLine($"[Gml][{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Sentry initialized");
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }
}

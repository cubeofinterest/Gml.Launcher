using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Gml.Launcher.Core.Services;
using Gml.Launcher.ViewModels;
using Gml.Launcher.Views;
using Gml.Launcher.Views.SplashScreen;

namespace Gml.Launcher;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
#if DEBUG
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var logDirectory = Path.Combine(appDataPath, "LumenMD", "DevLogs");
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var logFile = Path.Combine(logDirectory, $"startup_debug_{timestamp}.log");
        
        try
        {
            File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] OnFrameworkInitializationCompleted started\n");
            
            // Run SimplePasswordStorage test
            SimplePasswordStorageTest.RunTest();
            File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] SimplePasswordStorage test completed\n");
        }
        catch (Exception testEx) 
        {
            try {
                File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] SimplePasswordStorage test failed: {testEx.Message}\n");
            } catch { }   
        }
#endif
        
        try
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
#if DEBUG
                try
                {
                    File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Creating SplashScreenViewModel\n");
                }
                catch { }
#endif

                var splashViewModel = new SplashScreenViewModel();
                
#if DEBUG
                try
                {
                    File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Creating SplashScreen window\n");
                }
                catch { }
#endif
                
                var splashScreen = new SplashScreen
                {
                    DataContext = splashViewModel
                };

                desktop.MainWindow = splashScreen;
                
#if DEBUG
                try
                {
                    File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] About to call splashViewModel.InitializeAsync()\n");
                }
                catch { }
#endif
                
                await splashViewModel.InitializeAsync();

#if DEBUG
                try
                {
                    File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] splashViewModel.InitializeAsync() completed\n");
                }
                catch { }
#endif

                desktop.MainWindow = splashScreen.GetMainWindow();
                desktop.MainWindow.Show();
                splashScreen.Close();
                
#if DEBUG
                try
                {
                    File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Main window shown, splash closed\n");
                }
                catch { }
#endif
            }

            base.OnFrameworkInitializationCompleted();
            
#if DEBUG
            try
            {
                File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] OnFrameworkInitializationCompleted finished successfully\n");
            }
            catch { }
#endif
        }
        catch (Exception ex)
        {
#if DEBUG
            try
            {
                File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] OnFrameworkInitializationCompleted FAILED: {ex}\n");
                File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Stack trace: {ex.StackTrace}\n");
            }
            catch { }
#endif
            // Используем переменную ex
            Console.WriteLine($"Critical error in OnFrameworkInitializationCompleted: {ex.Message}");
            throw;
        }
    }
}

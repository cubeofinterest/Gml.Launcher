using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using GamerVII.Notification.Avalonia;
using Gml.Client;
using Gml.Client.Models;
using Gml.Launcher.Assets;
using Gml.Launcher.Assets.Resources;
using Gml.Launcher.Core.Services;
using Gml.Launcher.Models;
using Gml.Launcher.ViewModels.Base;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Sentry;

namespace Gml.Launcher.ViewModels.Pages;

public class SettingsPageViewModel : PageViewModelBase
{
    private const int HighRamThreshold = 16384;
    private readonly IGmlClientManager _gmlManager;
    private readonly ISettingsService _settingsService;
    private readonly MainWindowViewModel _mainViewModel;

    private double _ramValue;
    private int _windowHeight;
    private int _windowWidth;

    public SettingsPageViewModel(
        IScreen screen,
        ILocalizationService? localizationService,
        ISettingsService settingsService,
        IGmlClientManager gmlManager) : base(screen, localizationService)
    {
        MainViewModel = (MainWindowViewModel)screen;
        _mainViewModel = (MainWindowViewModel)screen;
        _settingsService = settingsService;
        _gmlManager = gmlManager;
        
        // Initialize client management commands
        UpdateClientCommand = ReactiveCommand.CreateFromTask(UpdateClient);
        ReinstallJavaCommand = ReactiveCommand.CreateFromTask(ReinstallJava);
        // Session validation command removed

        this.WhenAnyValue(
                x => x.RamValue,
                x => x.WindowWidth,
                x => x.WindowHeight,
                x => x.FullScreen,
                x => x.DynamicRamValue
            )
            .Where(ValidateParams)
            .Throttle(TimeSpan.FromMilliseconds(400), RxApp.TaskpoolScheduler)
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Subscribe(SaveSettings);

        this.WhenAnyValue(x => x.Settings)
            .WhereNotNull()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(UpdateSettings);

        // Language change subscription removed

        RxApp.TaskpoolScheduler.Schedule(LoadSettings);
    }

    private bool ValidateParams(
        (
            double ramValue,
            string width,
            string height,
            bool isFullScreen,
            bool isDynamicRam)
        update)
    {
        if (update.ramValue <= 0)
        {
            return false;
        }

        if (!int.TryParse(update.width, out var width) || width <= 0)
        {
            return false;
        }

        if (!int.TryParse(update.height, out var height) || height <= 0)
        {
            return false;
        }

        // Session validation check removed

        return true;
    }

    [Reactive] public bool DynamicRamValue { get; set; }
    [Reactive] public bool FullScreen { get; set; }
    [Reactive] public ulong MinRamValue { get; set; }
    [Reactive] public ulong MaxRamValue { get; set; }
    [Reactive] public ulong RamTickValue { get; set; }
    // Language selection removed - using only Russian
    [Reactive] public string? InstallationFolder { get; set; }
    // Language selection removed completely
    // Session validation interval removed
    // RamValueView is now manually implemented above
    [Reactive] private SettingsInfo? Settings { get; set; }

    public double RamValue
    {
        get => _ramValue;
        set
        {
            if (!(Math.Abs(value - _ramValue) > 0.0)) return;

            _ramValue = Round(value, 8);
            RamValueView = Convert.ToInt32(_ramValue).ToString(CultureInfo.InvariantCulture);
            this.RaisePropertyChanged();
            return;

            double Round(double value, double step)
            {
                var offset = value % step;
                return offset >= step / 2.0
                    ? value + (step - offset)
                    : value - offset;
            }
        }
    }
    
    // Property for the RAM value input field
    private string _ramValueView = string.Empty;
    private bool _isUpdatingRamValue = false; // Flag to prevent infinite loop
    
    public string RamValueView
    {
        get => _ramValueView;
        set
        {
            // Only update if different
            if (_ramValueView == value) return;
            
            // Check if the value is numeric
            if (int.TryParse(value, out int ramInt))
            {
                // Ensure the value is within the allowed range
                int minRamValue = (int)MinRamValue;
                int maxRamValue = (int)MaxRamValue;
                
                if (ramInt < minRamValue)
                    ramInt = minRamValue;
                else if (ramInt > maxRamValue)
                    ramInt = maxRamValue;
                
                // Round to the nearest tick value if needed
                if (RamTickValue > 1)
                {
                    int remainder = ramInt % (int)RamTickValue;
                    if (remainder > 0)
                    {
                        if (remainder >= (int)RamTickValue / 2)
                            ramInt = ramInt + ((int)RamTickValue - remainder); // Round up
                        else
                            ramInt = ramInt - remainder; // Round down
                    }
                }
                
                // Update the RamValue (which will update the slider) only if not already updating
                if (!_isUpdatingRamValue)
                {
                    _isUpdatingRamValue = true;
                    RamValue = ramInt;
                    _isUpdatingRamValue = false;
                }
                
                // Update the display value
                _ramValueView = ramInt.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                // If not numeric, just update the display value but don't change the slider
                _ramValueView = value;
            }
            
            this.RaisePropertyChanged();
        }
    }

    public string WindowWidth
    {
        get => _windowWidth.ToString();
        set
        {
            var isNumeric = int.TryParse(string.Concat(value.Where(char.IsDigit)), out var numericValue);
            if (isNumeric)
                this.RaiseAndSetIfChanged(ref _windowWidth, numericValue);
            else
                this.RaiseAndSetIfChanged(ref _windowWidth, 600);
        }
    }

    public string WindowHeight
    {
        get => _windowHeight.ToString();
        set
        {
            var isNumeric = int.TryParse(string.Concat(value.Where(char.IsDigit)), out var numericValue);
            if (isNumeric)
                this.RaiseAndSetIfChanged(ref _windowHeight, numericValue);
            else
                this.RaiseAndSetIfChanged(ref _windowWidth, 900);
        }
    }

    // Available languages removed - using only Russian
    public MainWindowViewModel MainViewModel { get; }
    
    public ICommand UpdateClientCommand { get; set; }
    public ICommand ReinstallJavaCommand { get; set; }
    // Test session expiration command removed

    // Language change method removed - using only Russian

    internal void ChangeFolder()
    {
        if (InstallationFolder != null)
        {
            // ИСПРАВЛЕНО: Добавляем логирование для отладки проблем с путями
            System.Diagnostics.Debug.WriteLine($"Changing installation folder to: '{InstallationFolder}'");
            _gmlManager.ChangeInstallationFolder(InstallationFolder);
        }

        _settingsService.UpdateInstallationDirectory(InstallationFolder);
    }

    private void UpdateSettings(SettingsInfo settings)
    {
        WindowWidth = settings.GameWidth.ToString();
        WindowHeight = settings.GameHeight.ToString();
        RamValue = settings.RamValue;
        DynamicRamValue = settings.IsDynamicRam;
        FullScreen = settings.FullScreen;
        
        // Language setting removed
        
        InstallationFolder = _gmlManager.InstallationDirectory;
        // Session validation interval setting removed

        MinRamValue = (ulong)(MaxRamValue > HighRamThreshold ? 1024 : 512);
        RamTickValue = (ulong)(MaxRamValue > HighRamThreshold ? 1024 : 512);
    }

    private async void LoadSettings()
    {
        try
        {
            // Language loading removed
            
            MaxRamValue = _settingsService.GetMaxRam();

            Settings = await _settingsService.GetSettings();
        }
        catch (Exception exception)
        {
            SentrySdk.CaptureException(exception);
        }
    }

    private async void SaveSettings(
        (double ramValue, string width, string height, bool isFullScreen, bool isDynamicRam)
            update)
    {
        try
        {
            var newSettings = new SettingsInfo(
                int.Parse(update.width),
                int.Parse(update.height),
                update.isFullScreen,
                update.isDynamicRam,
                update.ramValue);

            await _settingsService.UpdateSettingsAsync(newSettings);
        }
        catch (Exception exception)
        {
            SentrySdk.CaptureException(exception);
        }
    }
    
    /// <summary>
    /// Updates GTNH client by clearing all files except specified exclusions
    /// </summary>
    private async Task UpdateClient()
    {
        try
        {


            // Show progress notification (simplified)
            var progressMessage = _mainViewModel.Manager
                .CreateMessage()
                .Background("#0066CC")
                .HasHeader(LocalizationService.GetString(ResourceKeysDictionary.UpdateClientTitle) ?? "Обновление клиента")
                .HasMessage("Очистка файлов клиента GTNH...")
                .Queue();

            await Task.Run(async () =>
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var gtnhClientPath = Path.Combine(appDataPath, "LumenMD", "clients", "gtnh");
                
                if (Directory.Exists(gtnhClientPath))
                {
                    // Files to preserve
                    var preserveFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        "options.txt",
                        "knownkeys.txt", 
                        "servers.dat"
                    };
                    
                    // Folders to preserve
                    var preserveFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        "shaderpacks",
                        "resourcepacks",
                        "journeymap",
                        "visualprospecting"
                    };
                    
                    // Delete all files except preserved ones
                    var files = Directory.GetFiles(gtnhClientPath);
                    foreach (var file in files)
                    {
                        var fileName = Path.GetFileName(file);
                        if (!preserveFiles.Contains(fileName))
                        {
                            try
                            {
                                File.Delete(file);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Failed to delete file {file}: {ex.Message}");
                            }
                        }
                    }
                    
                    // Delete all folders except preserved ones
                    var directories = Directory.GetDirectories(gtnhClientPath);
                    foreach (var directory in directories)
                    {
                        var dirName = Path.GetFileName(directory);
                        if (!preserveFolders.Contains(dirName))
                        {
                            try
                            {
                                Directory.Delete(directory, true);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Failed to delete directory {directory}: {ex.Message}");
                            }
                        }
                    }
                }
                
                await Task.Delay(1000); // Brief delay to show progress
            });

            // Progress completed
            
            // Close progress notification
            if (progressMessage != null)
            {
                _mainViewModel.Manager.Dismiss(progressMessage);
            }
            
            // Show completion message
            _mainViewModel.Manager
                .CreateMessage()
                .Background("#008800")
                .HasHeader("Обновление завершено")
                .HasMessage("GTNH клиент очищен. Настройки, сервера и пользовательские данные сохранены.")
                .Dismiss().WithDelay(TimeSpan.FromSeconds(5))
                .Queue();
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            
            _mainViewModel.Manager
                .CreateMessage()
                .Background("#CC0000")
                .HasHeader("Ошибка обновления")
                .HasMessage("Произошла ошибка при обновлении клиента. Проверьте логи.")
                .Dismiss().WithDelay(TimeSpan.FromSeconds(5))
                .Queue();
        }
    }
    
    /// <summary>
    /// Reinstalls Java by deleting the runtime folder
    /// </summary>
    private async Task ReinstallJava()
    {
        try
        {


            // Show progress notification (simplified)
            var progressMessage = _mainViewModel.Manager
                .CreateMessage()
                .Background("#0066CC")
                .HasHeader(LocalizationService.GetString(ResourceKeysDictionary.ReinstallJavaTitle) ?? "Переустановка Java")
                .HasMessage("Удаление текущей установки Java...")
                .Queue();

            await Task.Run(async () =>
            {
                var runtimePath = Path.Combine(_gmlManager.InstallationDirectory, "runtime");
                if (Directory.Exists(runtimePath))
                {
                    Directory.Delete(runtimePath, true);
                }
                await Task.Delay(1000); // Brief delay to show progress
            });

            // Progress completed
            
            // Close progress notification
            if (progressMessage != null)
            {
                _mainViewModel.Manager.Dismiss(progressMessage);
            }
            
            // Show completion message
            _mainViewModel.Manager
                .CreateMessage()
                .Background("#008800")
                .HasHeader("Переустановка завершена")
                .HasMessage("Java успешно удалена. При следующем запуске игры будет загружена новая версия.")
                .Dismiss().WithDelay(TimeSpan.FromSeconds(5))
                .Queue();
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            
            _mainViewModel.Manager
                .CreateMessage()
                .Background("#CC0000")
                .HasHeader("Ошибка переустановки")
                .HasMessage("Произошла ошибка при переустановке Java. Проверьте логи.")
                .Dismiss().WithDelay(TimeSpan.FromSeconds(5))
                .Queue();
        }
    }
    

    
    // Language change method removed completely
    
    // Session validation test method removed
}

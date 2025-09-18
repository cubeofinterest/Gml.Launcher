using System;
using System.Linq;
using System.IO;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using Avalonia.VisualTree;
using Avalonia.Input;
using GamerVII.Notification.Avalonia;
using Gml.Launcher.Assets;
using Gml.Launcher.ViewModels.Pages;
using ReactiveUI;
using Sentry;

// using Sentry;

namespace Gml.Launcher.Views.Pages;

public partial class SettingsPageView : ReactiveUserControl<SettingsPageViewModel>
{
    public SettingsPageView()
    {
        this.WhenActivated(disposables => { });
        AvaloniaXamlLoader.Load(this);
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        // Allow only numeric characters, backspace, delete, and navigation keys
        if (!char.IsDigit((char)e.Key) && 
            e.Key != Key.Back && 
            e.Key != Key.Delete && 
            e.Key != Key.Left && 
            e.Key != Key.Right && 
            e.Key != Key.Tab)
        {
            e.Handled = true;
        }
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox) 
        {
            // Ensure only numeric input
            var originalCaretIndex = textBox.CaretIndex;
            var originalLength = textBox.Text?.Length ?? 0;
            
            textBox.Text = string.Concat(textBox.Text?.Where(char.IsDigit) ?? string.Empty);
            
            // Try to maintain cursor position
            var newLength = textBox.Text?.Length ?? 0;
            if (originalCaretIndex <= newLength)
            {
                textBox.CaretIndex = originalCaretIndex - (originalLength - newLength);
            }
        }
    }
    
    private void OnRamValueLostFocus(object? sender, RoutedEventArgs e)
    {
        // When the RAM text box loses focus, ensure the value is applied
        if (sender is TextBox textBox && ViewModel != null)
        {
            ViewModel.RamValueView = textBox.Text ?? string.Empty;
        }
    }

    private async void OpenFileDialog(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (this.GetVisualRoot() is MainWindow mainWindow)
            {
                var folders = await mainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    AllowMultiple = false,
                    Title = "Select a folder"
                });

                if (folders.Count != 1) return;

                var path = folders[0].Path.LocalPath;

                ViewModel!.InstallationFolder = Path.GetFullPath(path);
                ViewModel!.ChangeFolder();
            }
        }
        catch (Exception exception)
        {
            SentrySdk.CaptureException(exception);

            // Existing log statement
            Console.WriteLine(exception.ToString());

            // Show error notification
            ViewModel?.MainViewModel.Manager
                .CreateMessage(true, "#D03E3E",
                ViewModel.LocalizationService.GetString(ResourceKeysDictionary.Error),
                ViewModel.LocalizationService.GetString(ResourceKeysDictionary.InvalidFolder))
                .Dismiss()
                .WithDelay(TimeSpan.FromSeconds(3))
                .Queue();
        }
    }
    private void OpenLocation(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (!Directory.Exists(ViewModel!.InstallationFolder))
                throw new DirectoryNotFoundException();

            if (OperatingSystem.IsWindows())
            {
                // ИСПРАВЛЕНО: Безопасный запуск explorer с путями, содержащими пробелы
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{ViewModel!.InstallationFolder}\"", // Добавляем кавычки
                    UseShellExecute = false
                });
            }
            else if (OperatingSystem.IsMacOS())
            {
                // ИСПРАВЛЕНО: Безопасный запуск для macOS
                Process.Start(new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = $"\"{ViewModel!.InstallationFolder}\"", // Добавляем кавычки
                    UseShellExecute = false
                });
            }
            else if (OperatingSystem.IsLinux())
            {
                // ИСПРАВЛЕНО: Безопасный запуск для Linux
                Process.Start(new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = $"\"{ViewModel!.InstallationFolder}\"", // Добавляем кавычки
                    UseShellExecute = false
                });
            }
        }
        catch (Exception exception)
        {
            SentrySdk.CaptureException(exception);

            // Existing log statement
            Console.WriteLine(exception.ToString());

            // Show error notification
            ViewModel?.MainViewModel.Manager
                .CreateMessage(true, "#D03E3E",
                ViewModel.LocalizationService.GetString(ResourceKeysDictionary.Error),
                ViewModel.LocalizationService.GetString(ResourceKeysDictionary.InvalidFolder))
                .Dismiss()
                .WithDelay(TimeSpan.FromSeconds(3))
                .Queue();
        }
    }
}

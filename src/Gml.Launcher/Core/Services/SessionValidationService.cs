using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Gml.Client;
using Gml.Client.Models;
using Gml.Launcher.Core.Exceptions;
using Splat;

namespace Gml.Launcher.Core.Services;

public class SessionValidationService : ISessionValidationService, IDisposable
{
    private readonly IGmlClientManager _gmlManager;
    private readonly IStorageService _storageService;
    private readonly ISettingsService _settingsService;
    private Timer? _validationTimer;
    private IUser? _currentUser;
    private bool _disposed;

    public event EventHandler<IUser>? SessionExpired;

    public SessionValidationService(IGmlClientManager? gmlManager = null, IStorageService? storageService = null, ISettingsService? settingsService = null)
    {
        _gmlManager = gmlManager ?? Locator.Current.GetService<IGmlClientManager>()
            ?? throw new ServiceNotFoundException(typeof(IGmlClientManager));
        
        _storageService = storageService ?? Locator.Current.GetService<IStorageService>()
            ?? throw new ServiceNotFoundException(typeof(IStorageService));
        
        _settingsService = settingsService ?? Locator.Current.GetService<ISettingsService>()
            ?? throw new ServiceNotFoundException(typeof(ISettingsService));
    }

    public async Task StartSessionValidationAsync(IUser user, CancellationToken cancellationToken = default)
    {
        if (_disposed) return;
        
        _currentUser = user;
        
        // Останавливаем предыдущий таймер если он существует
        StopSessionValidation();
        
        // Используем фиксированный интервал проверки (5 минут)
        var validationInterval = TimeSpan.FromMinutes(5);
        
        // Запускаем периодическую проверку сессии
        _validationTimer = new Timer(async _ => await ValidateSessionAsync(), null, 
            validationInterval, validationInterval);
        
        // Выполняем первую проверку сразу
        await ValidateSessionAsync();
    }

    public void StopSessionValidation()
    {
        _validationTimer?.Dispose();
        _validationTimer = null;
        _currentUser = null;
    }

    public async Task<bool> ValidateUserSessionAsync(IUser user)
    {
        if (_disposed) return false;
        
        try
        {
            // Проверяем локальную валидность токена (JWT) асинхронно без .Result
            if (!await ValidateTokenLocallyAsync(user))
            {
                return false;
            }
            
            // Проверяем валидность токена через API
            if (!await ValidateTokenWithApiAsync(user))
            {
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            // Log the exception for debugging
            Debug.WriteLine($"Error validating session: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    private async Task ValidateSessionAsync()
    {
        if (_currentUser == null || _disposed)
        {
            Debug.WriteLine("ValidateSessionAsync: Current user is null or service is disposed");
            return;
        }
        
        try
        {
            Debug.WriteLine($"Starting session validation for user: {_currentUser.Name}");
            var isValid = await ValidateUserSessionAsync(_currentUser);
            
            if (!isValid)
            {
                Debug.WriteLine("Session validation failed, clearing user data");
                // Сессия невалидна, очищаем данные пользователя
                await _storageService.SetAsync<AuthUser?>(StorageConstants.User, null);
                
                // Вызываем событие
                SessionExpired?.Invoke(this, _currentUser);
                
                // Останавливаем проверку
                StopSessionValidation();
            }
            else
            {
                Debug.WriteLine("Session validation successful");
            }
        }
        catch (Exception ex)
        {
            // В случае ошибки считаем сессию невалидной
            Debug.WriteLine($"Exception during session validation: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            
            await _storageService.SetAsync<AuthUser?>(StorageConstants.User, null);
            SessionExpired?.Invoke(this, _currentUser);
            StopSessionValidation();
        }
    }

    private Task<bool> ValidateTokenLocallyAsync(IUser user)
    {
        try
        {
            // Always check for null to prevent exceptions
            if (user == null)
            {
                Debug.WriteLine("Validate token failed: User is null");
                return Task.FromResult(false);
            }
            
            // Проверяем, не истек ли токен по времени
            if (user.ExpiredDate <= DateTime.Now)
            {
                Debug.WriteLine($"Validate token failed: Token expired at {user.ExpiredDate}, current time is {DateTime.Now}");
                return Task.FromResult(false);
            }
            
            // Check if the token is actually present
            if (string.IsNullOrEmpty(user.AccessToken))
            {
                Debug.WriteLine("Validate token failed: Token is null or empty");
                return Task.FromResult(false);
            }
            
            // Здесь можно добавить дополнительную локальную валидацию JWT токена
            // Например, проверку подписи, структуры и т.д.
            
            Debug.WriteLine("Local token validation successful");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in ValidateTokenLocallyAsync: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            return Task.FromResult(false);
        }
    }

    private async Task<bool> ValidateTokenWithApiAsync(IUser user)
    {
        try
        {
            var authResult = await _gmlManager.Auth(user.AccessToken);
            return authResult.User?.IsAuth == true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        StopSessionValidation();
        _disposed = true;
    }
}

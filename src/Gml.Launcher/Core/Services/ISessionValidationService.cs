using System;
using System.Threading;
using System.Threading.Tasks;
using Gml.Client.Models;

namespace Gml.Launcher.Core.Services;

public interface ISessionValidationService
{
    /// <summary>
    /// Запускает периодическую проверку валидности сессии
    /// </summary>
    /// <param name="user">Пользователь для проверки</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task StartSessionValidationAsync(IUser user, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Останавливает периодическую проверку сессии
    /// </summary>
    void StopSessionValidation();
    
    /// <summary>
    /// Проверяет валидность токена пользователя
    /// </summary>
    /// <param name="user">Пользователь для проверки</param>
    /// <returns>True если токен валиден, false в противном случае</returns>
    Task<bool> ValidateUserSessionAsync(IUser user);
    
    /// <summary>
    /// Событие, которое срабатывает когда сессия становится невалидной
    /// </summary>
    event EventHandler<IUser> SessionExpired;
}


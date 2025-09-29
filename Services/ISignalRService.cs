using System;
using System.Threading.Tasks;
using VEPS_Plus.ViewModels;

namespace VEPS_Plus.Services
{
    public interface ISignalRService
    {
        /// <summary>
        /// Инициализирует SignalR соединение
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Подключается к SignalR Hub
        /// </summary>
        Task ConnectAsync();

        /// <summary>
        /// Отключается от SignalR Hub
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Проверяет состояние соединения
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Событие получения нового уведомления
        /// </summary>
        event EventHandler<Notification>? NotificationReceived;

        /// <summary>
        /// Устанавливает токен авторизации
        /// </summary>
        Task SetAuthTokenAsync(string token);
    }
}

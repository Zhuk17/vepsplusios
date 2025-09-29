using System.Threading.Tasks;

namespace VEPS_Plus.Services
{
    public interface IPushNotificationService
    {
        /// <summary>
        /// Инициализирует сервис push-уведомлений
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Получает токен устройства для push-уведомлений
        /// </summary>
        Task<string?> GetTokenAsync();

        /// <summary>
        /// Отправляет тестовое локальное уведомление
        /// </summary>
        Task SendTestNotificationAsync(string title, string message);

        /// <summary>
        /// Регистрирует токен на сервере
        /// </summary>
        Task RegisterTokenAsync(string token);

        /// <summary>
        /// Проверяет, разрешены ли уведомления
        /// </summary>
        Task<bool> AreNotificationsEnabledAsync();

        /// <summary>
        /// Запрашивает разрешение на уведомления
        /// </summary>
        Task<bool> RequestPermissionAsync();
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage; // Для SecureStorage
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using VEPS_Plus.Services;
using System.Text.Json.Serialization; // Для [JsonPropertyName]
using System.Text.Json; // Для JsonSerializerOptions и JsonNamingPolicy
using Microsoft.Extensions.Logging;
using VEPS_Plus.Constants;
// using VEPS_Plus.ViewModels.Shared; // Возможно, понадобится, если ApiResponses.cs в Shared

namespace VEPS_Plus.ViewModels
{
    public partial class NotificationsViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Notification> notifications = new ObservableCollection<Notification>();

        private readonly ApiService _apiService;
        private int _currentUserId; // Поле для хранения UserId текущего пользователя
        private readonly ILogger<NotificationsViewModel> _logger;
        private readonly IToastService _toastService;
        private readonly ISignalRService _signalRService;

        public NotificationsViewModel(ApiService apiService, ILogger<NotificationsViewModel> logger, IToastService toastService, ISignalRService signalRService)
        {
            _apiService = apiService;
            _logger = logger;
            _toastService = toastService;
            _signalRService = signalRService;

            // Подписываемся на получение новых уведомлений через SignalR
            _signalRService.NotificationReceived += OnNotificationReceived;
            // Загружаем UserId при инициализации ViewModel
            Task.Run(LoadCurrentUserIdAsync);
        }

        // Асинхронный метод для загрузки UserId текущего пользователя
        private async Task LoadCurrentUserIdAsync()
        {
            var userIdString = await SecureStorage.GetAsync(SecureStorageKeys.UserId);
            if (int.TryParse(userIdString, out int userId) && userId > 0)
            {
                _currentUserId = userId;
                _logger.LogDebug("[NotificationsViewModel] UserId loaded: {UserId}", userId);
                await LoadNotificationsAsync(); // Автоматическая загрузка уведомлений
            }
            else
            {
                _logger.LogWarning("[NotificationsViewModel] UserId not found or invalid in SecureStorage. Redirecting to LoginPage.");
                _toastService.ShowToast("ID пользователя не найден. Пожалуйста, войдите снова.", isError: true);
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }

        [RelayCommand]
        public async Task LoadNotificationsAsync()
        {
            try
            {
                _logger.LogDebug("[NotificationsViewModel] LoadNotificationsAsync: Checking User ID validity... Current _currentUserId: {CurrentUserId}", _currentUserId);
                // Проверяем авторизацию перед запросом
                if (!await IsUserLoggedInAndUserIdValid()) return;

                Notifications.Clear();

                // API вызов для загрузки уведомлений
                var apiResponse = await _apiService.GetAsync<ApiResponse<List<Notification>>>($"/api/v1/notifications"); // userId берется из JWT

                if (apiResponse.IsSuccess && apiResponse.Data != null)
                {
                    foreach (var notification in apiResponse.Data)
                    {
                        Notifications.Add(notification);
                    }
                }
                else
                {
                    _logger.LogError("Failed to load notifications. Message: {Message}", apiResponse?.Message ?? "Unknown error.");
                    _toastService.ShowToast(apiResponse?.Message ?? "Не удалось загрузить уведомления", isError: true);
                }
            }
            catch (UnauthorizedException ex) { await HandleAuthError(ex); }
            catch (HttpRequestException ex) { await HandleNetworkError(ex); }
            catch (Exception ex) { await HandleGenericError(ex); }
        }

        [RelayCommand]
        public async Task MarkAsReadAsync(Notification notification)
        {
            try
            {
                // Проверяем авторизацию перед запросом
                if (!await IsUserLoggedInAndUserIdValid()) return;

                var updateData = new Notification { Id = notification.Id, UserId = notification.UserId, Title = notification.Title, Message = notification.Message, IsRead = true, CreatedAt = notification.CreatedAt };
                // Отправляем PATCH-запрос и ожидаем ApiResponse (без Data)
                var apiResponse = await _apiService.PatchAsync<ApiResponse>($"/api/v1/notifications/{notification.Id}", updateData); // userId берется из JWT

                if (apiResponse.IsSuccess)
                {
                    _toastService.ShowToast(apiResponse.Message ?? "Уведомление отмечено как прочитанное.", isError: false);
                    notification.IsRead = true;
                }
                else
                {
                    _logger.LogError("Failed to mark notification {NotificationId} as read. Message: {Message}", notification.Id, apiResponse?.Message ?? "Unknown error.");
                    _toastService.ShowToast(apiResponse?.Message ?? "Не удалось отметить уведомление как прочитанное.", isError: true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read.");
                _toastService.ShowToast($"Не удалось отметить уведомление как прочитанное: {ex.Message}", isError: true);
            }
        }

        // Вспомогательный метод для проверки авторизации и валидности UserId
        private async Task<bool> IsUserLoggedInAndUserIdValid()
        {
            if (_currentUserId <= 0)
            {
                // Если _currentUserId еще не установлен, пытаемся загрузить его
                await LoadCurrentUserIdAsync(); 
                if (_currentUserId <= 0) // Повторная проверка после попытки загрузки
                {
                    _logger.LogWarning("[NotificationsViewModel] IsUserLoggedInAndUserIdValid: Failed to get valid UserId. Redirecting to LoginPage.");
                    _toastService.ShowToast("ID пользователя не найден. Пожалуйста, войдите снова.", isError: true);
                    await Shell.Current.GoToAsync("//LoginPage");
                    return false;
                }
            }
            // Проверка флага isLoggedInFlag не требуется, так как ApiService уже обрабатывает 401
            return true;
        }

        // Унифицированные обработчики ошибок (можно вынести в базовый класс ViewModel)
        private async Task HandleAuthError(UnauthorizedException ex)
        {
            _toastService.ShowToast($"Ошибка авторизации: {ex.Message}", isError: true);
            // ApiService уже перенаправляет на LoginPage и очищает токены, здесь дублировать не нужно.
        }

        private async Task HandleNetworkError(HttpRequestException ex)
        {
            _toastService.ShowToast($"Ошибка сети: Не удалось подключиться к серверу: {ex.Message}", isError: true);
        }

        private async Task HandleGenericError(Exception ex)
        {
            _toastService.ShowToast($"Ошибка: Произошла ошибка: {ex.Message}", isError: true);
        }

        /// <summary>
        /// Обработчик получения нового уведомления через SignalR
        /// </summary>
        private void OnNotificationReceived(object? sender, Notification notification)
        {
            try
            {
                _logger.LogInformation("[NotificationsViewModel] Received SignalR notification: {Title}", notification.Title);
                
                // Добавляем уведомление в коллекцию на главном потоке
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                {
                    // Добавляем в начало списка (самые новые сверху)
                    Notifications.Insert(0, notification);
                    
                    // Показываем toast уведомление
                    _toastService.ShowToast($"Новое уведомление: {notification.Title}", isError: false);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[NotificationsViewModel] Error handling SignalR notification");
            }
        }
    }
}


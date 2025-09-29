using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage; // Для SecureStorage
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using VEPS_Plus.Services;
using System.Text.Json.Serialization; // Для [JsonPropertyName]
using System.Text.Json; // Для JsonSerializerOptions и JsonNamingPolicy
using System.Net.Http; // Для HttpRequestException
// using VEPS_Plus.ViewModels.Shared; // Возможно, понадобится, если ApiResponses.cs в Shared

namespace VEPS_Plus.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool darkTheme = true; // Начальное значение по умолчанию

        [ObservableProperty]
        private bool pushNotifications = true; // Начальное значение по умолчанию

        [ObservableProperty]
        private string language = "ru"; // Начальное значение по умолчанию

        [ObservableProperty]
        private ObservableCollection<string> languages = new ObservableCollection<string> { "ru", "en" };

        private readonly ApiService _apiService;
        private int _currentUserId; // Поле для хранения UserId текущего пользователя

        public SettingsViewModel(ApiService apiService)
        {
            _apiService = apiService;
            // Загружаем UserId при инициализации ViewModel
            // Task.Run(LoadCurrentUserIdAndSettingsAsync);
        }

        // Метод для загрузки UserId и последующей загрузки настроек
        public async Task LoadCurrentUserIdAndSettingsAsync()
        {
            var userIdString = await SecureStorage.GetAsync("user_id");
            if (int.TryParse(userIdString, out int userId) && userId > 0)
            {
                _currentUserId = userId;
                await LoadSettingsAsync(); // Автоматическая загрузка настроек после получения UserId
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка авторизации", "ID пользователя не найден. Пожалуйста, войдите снова.", "OK");
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }

        // Команда для загрузки настроек
        [RelayCommand]
        public async Task LoadSettingsAsync()
        {
            try
            {
                // Проверяем авторизацию и валидность UserId
                if (!await IsUserLoggedInAndUserIdValid()) return;

                // Выполняем GET-запрос к API
                var apiResponse = await _apiService.GetAsync<ApiResponse<Settings>>($"/api/v1/settings"); // userId берется из JWT

                if (apiResponse.IsSuccess && apiResponse.Data != null)
                {
                    // Обновляем ObservableProperties данными из настроек
                    DarkTheme = apiResponse.Data.DarkTheme;
                    PushNotifications = apiResponse.Data.PushNotifications;
                    Language = apiResponse.Data.Language ?? "ru"; // Дефолтное значение, если Language null
                    // UpdatedAt не отображается на UI

                    // Применяем темную тему, если она загружена из настроек
                    Application.Current.UserAppTheme = DarkTheme ? AppTheme.Dark : AppTheme.Light;
                }
                else
                {
                    // Если настроек нет, инициализируем UI дефолтными значениями
                    DarkTheme = true;
                    PushNotifications = true;
                    Language = "ru";
                }
            }
            catch (UnauthorizedException ex) { await HandleAuthError(ex); }
            catch (HttpRequestException ex) { await HandleNetworkError(ex); }
            catch (Exception ex) { await HandleGenericError(ex); }
        }

        // Команда для обновления настроек
        [RelayCommand]
        public async Task UpdateSettingsAsync()
        {
            try
            {
                if (!await IsUserLoggedInAndUserIdValid()) return;

                // Создаем анонимный объект с обновленными данными
                var updateData = new { DarkTheme, PushNotifications, Language };

                // Отправляем PUT-запрос
                var apiResponse = await _apiService.PutAsync<ApiResponse<Settings>>($"/api/v1/settings", updateData); // userId берется из JWT

                if (apiResponse.IsSuccess && apiResponse.Data != null)
                {
                    // Применяем темную тему сразу после сохранения
                    Application.Current.UserAppTheme = DarkTheme ? AppTheme.Dark : AppTheme.Light;
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка", apiResponse.Message ?? "Не удалось обновить настройки.", "OK");
                }
            }
            catch (UnauthorizedException ex) { await HandleAuthError(ex); }
            catch (HttpRequestException ex) { await HandleNetworkError(ex); }
            catch (Exception ex) { await HandleGenericError(ex); }
        }

        // Вспомогательный метод для проверки авторизации и валидности UserId
        private async Task<bool> IsUserLoggedInAndUserIdValid()
        {
            if (_currentUserId <= 0)
            {
                // Если _currentUserId не инициализирован, пытаемся загрузить его
                await LoadCurrentUserIdAndSettingsAsync();
                if (_currentUserId <= 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка авторизации", "ID пользователя не найден. Пожалуйста, войдите снова.", "OK");
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
            await Application.Current.MainPage.DisplayAlert("Ошибка авторизации", ex.Message, "OK");
            // ApiService уже перенаправляет на LoginPage и очищает токены, здесь дублировать не нужно.
        }

        private async Task HandleNetworkError(HttpRequestException ex)
        {
            await Application.Current.MainPage.DisplayAlert("Ошибка сети", $"Не удалось подключиться к серверу: {ex.Message}", "OK");
        }

        private async Task HandleGenericError(Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Ошибка", "Произошла ошибка: " + ex.Message, "OK");
        }
    }
}
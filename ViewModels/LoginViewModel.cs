using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using VEPS_Plus.Services;
using Microsoft.Maui.Storage; // Убедись, что этот using есть!
using VEPS_Plus.Constants; // ДОБАВЛЕНО: Для SecureStorageKeys
using Microsoft.Extensions.Logging;
using System.Collections.Generic; // Added for Dictionary

namespace VEPS_Plus.ViewModels
{
    public class UnauthorizedException : HttpRequestException
    {
        public UnauthorizedException(string message) : base(message) { }
    }

    public partial class LoginViewModel : ObservableObject
    {
        [ObservableProperty]
        private string username;

        [ObservableProperty]
        private string password;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool isPasswordVisible;

        private readonly ApiService _apiService;
        private readonly ILogger<LoginViewModel> _logger;
        private readonly IToastService _toastService;
        private readonly IVpnService _vpnService; // Внедряем IVpnService

        public LoginViewModel(ApiService apiService, ILogger<LoginViewModel> logger, IToastService toastService, IVpnService vpnService)
        {
            _apiService = apiService;
            _logger = logger;
            _toastService = toastService;
            _vpnService = vpnService; // Инициализируем IVpnService
            Username = string.Empty; // Инициализация
            Password = string.Empty; // Инициализация
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
                {
                    Page? currentPage = Application.Current?.Windows[0]?.Page;
                    if (currentPage == null) { _logger.LogError("Could not get current page to display alert in LoginAsync (empty credentials)."); return; }
                    await currentPage.DisplayAlert("Ошибка", "Введите логин и пароль", "OK");
                    return;
                }

                if (Username.Length < 3)
                {
                    Page? currentPage = Application.Current?.Windows[0]?.Page;
                    if (currentPage == null) { _logger.LogError("Could not get current page to display alert in LoginAsync (short username)."); return; }
                    await currentPage.DisplayAlert("Ошибка", "Логин должен содержать минимум 3 символа", "OK");
                    return;
                }

                if (Password.Length < 4)
                {
                    Page? currentPage = Application.Current?.Windows[0]?.Page;
                    if (currentPage == null) { _logger.LogError("Could not get current page to display alert in LoginAsync (short password)."); return; }
                    await currentPage.DisplayAlert("Ошибка", "Пароль должен содержать минимум 4 символа", "OK");
                    return;
                }

                IsLoading = true;

                _logger.LogDebug("LoginViewModel: Username = '{Username}', Password = '{Password}'", Username, Password);

                // 1. Проверяем, активен ли VPN. Если нет, сообщаем пользователю.
                if (!_vpnService.IsVpnActive())
                {
                    Page? currentPage = Application.Current?.Windows[0]?.Page;
                    if (currentPage == null) { _logger.LogError("Could not get current page to display alert in LoginAsync (VPN inactive)."); return; }
                    await currentPage.DisplayAlert("Ошибка WireGuard", "VPN не активен. Пожалуйста, запустите приложение через основной экран или убедитесь, что WireGuard туннель \"veps\" активен.", "OK");
                    IsLoading = false;
                    return;
                }

                var loginData = new LoginRequest
                {
                    Username = Username,
                    Password = Password
                };

                string serializedData = JsonSerializer.Serialize(loginData);
                _logger.LogDebug("LoginViewModel: Serializing loginData: {SerializedData}", serializedData);

                // ИСПРАВЛЕНИЕ: Ожидаем ApiResponse<AuthSuccessResponse> от сервера
                ApiResponse<AuthSuccessResponse>? apiResponse = null; // Добавлено '?'
                long apiCallTime = 0;
                long secureStorageTime = 0;

                stopwatch.Restart();
                try
                {
                    apiResponse = await _apiService.PostAsync<ApiResponse<AuthSuccessResponse>>("/api/v1/auth/login", loginData);
                    apiCallTime = stopwatch.ElapsedMilliseconds;
                    _logger.LogDebug("[LoginViewModel] API Call Duration: {ApiCallTime} ms", apiCallTime);
                }
                catch (HttpRequestException)
                {
#if DEBUG
                    var local = TryMockLogin(Username, Password);
                    if (local != null)
                    {
                        apiResponse = new ApiResponse<AuthSuccessResponse>
                        {
                            IsSuccess = true,
                            Data = local,
                            Message = "Вход в офлайн‑режиме"
                        };
                    }
                    else
                    {
                        throw;
                    }
#else
                    throw; // В релиз-режиме перебрасываем сетевые ошибки
#endif
                }

                if (apiResponse != null)
                {
#if DEBUG
                    _logger.LogDebug("[DEBUG CLIENT] Server Response - IsSuccess: {IsSuccess}, Data.UserId: {UserId}, Data.Username: '{Username}', Data.Role: '{Role}', Data.Token: '{Token}', Message: '{Message}'", apiResponse.IsSuccess, apiResponse.Data?.UserId, apiResponse.Data?.Username, apiResponse.Data?.Role, apiResponse.Data?.Token, apiResponse.Message);
#endif
                }
                else
                {
#if DEBUG
                    _logger.LogDebug("[DEBUG CLIENT] Server Response is null after deserialization.");
#endif
                }

                if (apiResponse != null && apiResponse.IsSuccess)
                {
                    // Устанавливаем флаг успешной авторизации в SecureStorage
                    stopwatch.Restart();
                    await SecureStorage.SetAsync(SecureStorageKeys.IsUserLoggedIn, "true");

                    // !!! КЛЮЧЕВОЕ ИСПРАВЛЕНИЕ: СОХРАНЯЕМ user_id и JWT токен в SecureStorage !!!
                    if (apiResponse.Data != null)
                    {
                        await SecureStorage.SetAsync(SecureStorageKeys.UserId, apiResponse.Data.UserId.ToString());
                        await SecureStorage.SetAsync(SecureStorageKeys.JwtToken, apiResponse.Data.Token); // Сохраняем JWT токен
                        _apiService.SetAuthToken(apiResponse.Data.Token); // Устанавливаем токен в ApiService
                        await SecureStorage.SetAsync(SecureStorageKeys.UserRole, apiResponse.Data.Role); // Сохраняем роль пользователя
                        await SecureStorage.SetAsync(SecureStorageKeys.Username, apiResponse.Data.Username); // Сохраняем имя пользователя
                        secureStorageTime = stopwatch.ElapsedMilliseconds;
                        _logger.LogDebug("[LoginViewModel] SecureStorage Write Duration: {SecureStorageTime} ms", secureStorageTime);
                        _logger.LogDebug("[LoginViewModel] Saved to SecureStorage: UserId='{UserId}', Role='{Role}', Username='{Username}', Token length={TokenLength}'", apiResponse.Data.UserId, apiResponse.Data.Role, apiResponse.Data.Username, apiResponse.Data.Token.Length);
                    }
                    else
                    {
                        // Если Data оказалась null, хотя IsSuccess = true, это ошибка сервера.
                        _logger.LogError("[DEBUG CLIENT] AuthSuccessResponse.Data is NULL, cannot save UserId or Token, despite IsSuccess being true.");
                        Page? currentPage = Application.Current?.Windows[0]?.Page;
                        if (currentPage == null) { _logger.LogError("Could not get current page to display alert in LoginAsync (auth success data null)."); return; }
                        await currentPage.DisplayAlert("Ошибка входа", "Сервер не вернул необходимые данные пользователя. Пожалуйста, попробуйте снова.", "OK");
                        await SecureStorage.SetAsync(SecureStorageKeys.IsUserLoggedIn, "false");
                        SecureStorage.Remove(SecureStorageKeys.UserId);
                        SecureStorage.Remove(SecureStorageKeys.JwtToken); // Удаляем токен тоже
                        _apiService.ClearAuthToken(); // Очищаем токен в ApiService
                        secureStorageTime = stopwatch.ElapsedMilliseconds;
                        _logger.LogDebug("[LoginViewModel] SecureStorage Clear Duration (on error): {SecureStorageTime} ms", secureStorageTime);
                        return;
                    }

                    Page? successPage = Application.Current?.Windows[0]?.Page;
                    if (successPage == null) { _logger.LogError("Could not get current page to display alert in LoginAsync (login success)."); return; }
                    await successPage.DisplayAlert("Успех", $"Добро пожаловать, {apiResponse.Data.Username}!", "OK");
                    await ((AppShell)Shell.Current).LoadHeaderData(); // Обновляем данные заголовка после успешного входа
                    await Shell.Current.GoToAsync("///MainPage"); // Исправлено на абсолютную навигацию на MainPage
                }
                else
                {
                    // Сбрасываем флаги, если вход не удался
                    stopwatch.Restart();
                    await SecureStorage.SetAsync(SecureStorageKeys.IsUserLoggedIn, "false");
                    SecureStorage.Remove(SecureStorageKeys.UserId);
                    SecureStorage.Remove(SecureStorageKeys.JwtToken); // Удаляем токен тоже
                    _apiService.ClearAuthToken(); // Очищаем токен в ApiService

                    secureStorageTime = stopwatch.ElapsedMilliseconds;
                    _logger.LogDebug("[LoginViewModel] SecureStorage Clear Duration (on failure): {SecureStorageTime} ms", secureStorageTime);

                    string errorMessage = apiResponse?.Message ?? "Неизвестная ошибка при входе";
                    _toastService.ShowToast(errorMessage, isError: true);

                    // Если логин не удался, не отключаем VPN, т.к. он управляется внешним приложением
                    // _vpnService.Disconnect();
                }
            }
            catch (UnauthorizedException ex)
            {
                // Пытаемся перейти в офлайн‑режим при 401
#if DEBUG
                var local = TryMockLogin(Username, Password);
                if (local != null)
                {
                    await SecureStorage.SetAsync(SecureStorageKeys.IsUserLoggedIn, "true");
                    await SecureStorage.SetAsync(SecureStorageKeys.UserId, local.UserId.ToString());
                    await SecureStorage.SetAsync(SecureStorageKeys.JwtToken, local.Token);
                    _apiService.SetAuthToken(local.Token);
                    await SecureStorage.SetAsync(SecureStorageKeys.UserRole, local.Role);
                    await SecureStorage.SetAsync(SecureStorageKeys.Username, local.Username);
                    _logger.LogDebug("[LoginViewModel] Mock Login Saved to SecureStorage: UserId='{UserId}', Role='{Role}', Username='{Username}', Token length={TokenLength}'", local.UserId, local.Role, local.Username, local.Token.Length);
                    _toastService.ShowToast($"Офлайн режим: Добро пожаловать, {local.Username}!", isError: false);
                    await ((AppShell)Shell.Current).LoadHeaderData(); // Обновляем данные заголовка после успешного входа
                    await Shell.Current.GoToAsync("///MainPage"); // Исправлено на абсолютную навигацию на MainPage
                }
                else
                {
                    _logger.LogError(ex, "Ошибка авторизации при попытке мок-логина.");
                    _toastService.ShowToast(ex.Message, isError: true);
                    await SecureStorage.SetAsync(SecureStorageKeys.IsUserLoggedIn, "false");
                    SecureStorage.Remove(SecureStorageKeys.UserId);
                    SecureStorage.Remove(SecureStorageKeys.JwtToken);
                    _apiService.ClearAuthToken();
                    // _vpnService.Disconnect(); // Отключаем VPN при ошибке авторизации - больше не нужно
                }
#else
                _logger.LogError(ex, "Ошибка авторизации.");
                _toastService.ShowToast(ex.Message, isError: true);
                await SecureStorage.SetAsync(SecureStorageKeys.IsUserLoggedIn, "false");
                SecureStorage.Remove(SecureStorageKeys.UserId);
                SecureStorage.Remove(SecureStorageKeys.JwtToken);
                _apiService.ClearAuthToken();
                // _vpnService.Disconnect(); // Отключаем VPN при ошибке авторизации - больше не нужно
#endif
            }
            catch (HttpRequestException ex)
            {
                // Обрабатываем сетевые ошибки
                _logger.LogError(ex, "Ошибка сети при входе.");
                _toastService.ShowToast($"Не удалось подключиться к серверу: {ex.Message}", isError: true);
                // _vpnService.Disconnect(); // Отключаем VPN при сетевой ошибке - больше не нужно
            }
            catch (Exception ex)
            {
                // Обрабатываем все остальные ошибки
                _logger.LogError(ex, "Неожиданная ошибка при входе.");
                _toastService.ShowToast($"Произошла неожиданная ошибка: {ex.Message}", isError: true);
                // _vpnService.Disconnect(); // Отключаем VPN при любой другой ошибке - больше не нужно
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogDebug("[LoginViewModel] Total LoginAsync Duration: {TotalDuration} ms", stopwatch.ElapsedMilliseconds);
                IsLoading = false;
            }
        }

#if DEBUG
        private AuthSuccessResponse? TryMockLogin(string username, string password) // Добавлено '?'
        {
            // Мок‑набор пользователей (как вы предоставили)
            var users = new Dictionary<string, (string Username, string Password, int UserId, string Role)>(StringComparer.OrdinalIgnoreCase)
            {
                { "testuser", ("testuser", "testpassword", 10, "user") },
                { "i.boyko", ("i.boyko", "1234", 20, "engineer1") },
                { "ar.lukashev", ("ar.lukashev", "1234", 30, "engineer2") },
                { "d.zotov", ("d.zotov", "1234", 40, "engineerp") },
                { "a.zubko", ("a.zubko", "1234", 50, "engineer2") },
                { "m.kerimov", ("m.kerimov", "1234", 60, "engineer3") },
                { "p.timofeev", ("p.timofeev", "1234", 70, "engineer3") },
                { "al.lukashev", ("al.lukashev", "1234", 80, "boss") }
            };

            if (users.TryGetValue(username, out var u) && u.Password == password)
            {
                // Генерируем псевдо‑токен
                var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                return new AuthSuccessResponse { UserId = u.UserId, Username = u.Username, Role = u.Role, Token = token };
            }
            return null;
        }
#endif

        // Команда для переключения видимости пароля
        [RelayCommand]
        private void TogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        // Команда для очистки полей
        [RelayCommand]
        private void ClearFields()
        {
            Username = string.Empty;
            Password = string.Empty;
        }
    }
}
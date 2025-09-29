using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage; // Для SecureStorage
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using VEPS_Plus.Services;
using Microsoft.Extensions.Logging;
using VEPS_Plus.Constants;

namespace VEPS_Plus.ViewModels
{
    public partial class ProfileViewModel : ObservableObject
    {
        [ObservableProperty]
        private string fullName;
        
        [ObservableProperty]
        private string email;
        
        [ObservableProperty]
        private string phone;
        
        [ObservableProperty]
        private string username;
        
        [ObservableProperty]
        private string role;
        
        [ObservableProperty]
        private string roleDisplayName;
        
        [ObservableProperty]
        private bool isEditing;
        
        [ObservableProperty]
        private bool isLoading;
        
        [ObservableProperty]
        private string accountCreatedAt; // от Users.CreatedAt

        [ObservableProperty]
        private string profileUpdatedAt; // от Profiles.UpdatedAt

        [ObservableProperty]
        private string currentPassword;
        
        [ObservableProperty]
        private string newPassword;
        
        [ObservableProperty]
        private string confirmPassword;

        private readonly ApiService _apiService;
        private int _currentUserId; // Поле для хранения UserId текущего пользователя
        private readonly ILogger<ProfileViewModel> _logger;
        private readonly IToastService _toastService;

        // Явные команды для привязки
        public ICommand LogoutAsyncCommand => new AsyncRelayCommand(LogoutAsync);
        public ICommand SaveProfileAsyncCommand => new AsyncRelayCommand(SaveProfileAsync);
        public ICommand ChangePasswordAsyncCommand => new AsyncRelayCommand(ChangePasswordAsync);

        public ProfileViewModel(ApiService apiService, ILogger<ProfileViewModel> logger, IToastService toastService)
        {
            _apiService = apiService;
            _logger = logger;
            _toastService = toastService;
            FullName = string.Empty;
            Email = string.Empty;
            Phone = string.Empty;
            Username = string.Empty;
            Role = string.Empty;
            RoleDisplayName = string.Empty;
            IsEditing = false;
            IsLoading = false;
            AccountCreatedAt = string.Empty;
            ProfileUpdatedAt = string.Empty;
            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
        }

        public async Task LoadProfileDataAsync()
        {
            IsLoading = true;
            try
            {
                var userIdString = await SecureStorage.GetAsync(SecureStorageKeys.UserId);
                if (int.TryParse(userIdString, out int userId) && userId > 0)
                {
                    _currentUserId = userId;
                }
                else
                {
                    _logger.LogWarning("User ID not found in SecureStorage. Redirecting to LoginPage.");
                    _toastService.ShowToast("ID пользователя не найден. Пожалуйста, войдите снова.", isError: true);
                    await Shell.Current.GoToAsync("///LoginPage");
                    return;
                }

                await LoadUserDataAsync();

                await LoadProfileAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Команда для загрузки данных пользователя
        [RelayCommand]
        public async Task LoadUserDataAsync()
        {
            IsLoading = true;
            try
            {
                if (!await IsUserLoggedInAndUserIdValid()) return;

                var apiResponse = await _apiService.GetAsync<ApiResponse<User>>($"/api/v1/users/{_currentUserId}");

                if (apiResponse.IsSuccess && apiResponse.Data != null)
                {
                    Username = apiResponse.Data.Username ?? string.Empty;
                    Role = apiResponse.Data.Role ?? string.Empty;
                    RoleDisplayName = UserRoles.GetDisplayName(Role);
                    if (apiResponse.Data.CreatedAt != default)
                    {
                        AccountCreatedAt = $"Аккаунт создан: {apiResponse.Data.CreatedAt:dd.MM.yyyy HH:mm}";
                    }
                }
                else
                {
                    var username = await SecureStorage.GetAsync(SecureStorageKeys.Username);
                    var userRole = await SecureStorage.GetAsync(SecureStorageKeys.UserRole);
                    Username = username ?? "Пользователь";
                    Role = userRole ?? "user";
                    RoleDisplayName = UserRoles.GetDisplayName(Role);
                    AccountCreatedAt = "Аккаунт создан: недавно";
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Команда для загрузки профиля
        [RelayCommand]
        public async Task LoadProfileAsync()
        {
            IsLoading = true;
            try
            {
                if (!await IsUserLoggedInAndUserIdValid()) return;

                var apiResponse = await _apiService.GetAsync<ApiResponse<Profile>>("/api/v1/profile");

                if (apiResponse.IsSuccess && apiResponse.Data != null)
                {
                    FullName = apiResponse.Data.FullName ?? string.Empty;
                    Email = apiResponse.Data.Email ?? string.Empty;
                    Phone = apiResponse.Data.Phone ?? string.Empty;
                    if (apiResponse.Data.UpdatedAt != default)
                    {
                        ProfileUpdatedAt = $"Обновлен: {apiResponse.Data.UpdatedAt:dd.MM.yyyy HH:mm}";
                    }
                }
                else
                {
                    FullName = "Тимофеев Павел Александрович";
                    Email = "p.timofeev@company.com";
                    Phone = "+7 (999) 123-45-67";
                    ProfileUpdatedAt = "Обновлен: недавно";
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Команда для начала редактирования профиля
        [RelayCommand]
        public void StartEditing()
        {
            IsEditing = true;
        }

        // Команда для отмены редактирования
        [RelayCommand]
        public async Task CancelEditing()
        {
            IsEditing = false;
            await LoadProfileAsync(); // Добавлено await
        }

        // Команда для выхода из аккаунта
        [RelayCommand]
        public async Task LogoutAsync()
        {
            try
            {
                Page? currentPage = Application.Current?.Windows[0]?.Page;
                if (currentPage == null)
                {
                    _logger.LogError("Could not get current page to display alert in LogoutAsync.");
                    _toastService.ShowToast("Произошла ошибка при выходе.", isError: true);
                    return;
                }

                var result = await currentPage.DisplayAlert(
                    "Подтверждение", 
                    "Вы уверены, что хотите выйти из аккаунта?", 
                    "Да", "Нет");

                if (result)
                {
                    await _apiService.ClearAuthToken();
                    await Shell.Current.GoToAsync("///LoginPage");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout.");
                _toastService.ShowToast($"Ошибка при выходе: {ex.Message}", isError: true);
            }
        }

        // Команда для сохранения профиля
        [RelayCommand]
        public async Task SaveProfileAsync()
        {
            Page? currentPage = Application.Current?.Windows[0]?.Page;
            if (currentPage == null)
            {
                _logger.LogError("Could not get current page to display alert in SaveProfileAsync.");
                _toastService.ShowToast("Произошла ошибка при сохранении профиля.", isError: true);
                return;
            }
            try
            {
                if (string.IsNullOrWhiteSpace(FullName))
                {
                    await currentPage.DisplayAlert("Ошибка", "Введите ФИО", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(Email))
                {
                    await currentPage.DisplayAlert("Ошибка", "Введите email", "OK");
                    return;
                }

                var updateData = new ProfileUpdateRequest
                {
                    FullName = FullName,
                    Email = Email,
                    Phone = Phone
                };

                var apiResponse = await _apiService.PutAsync<ApiResponse<Profile>>("/api/v1/profile", updateData);

                if (apiResponse.IsSuccess)
                {
                    await currentPage.DisplayAlert("Успех", "Профиль успешно обновлен", "OK");
                    IsEditing = false;
                    await LoadProfileAsync();
                }
                else
                {
                    await currentPage.DisplayAlert("Ошибка", apiResponse.Message ?? "Не удалось обновить профиль", "OK");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving profile.");
                await currentPage.DisplayAlert("Ошибка", $"Не удалось сохранить профиль: {ex.Message}", "OK");
            }
        }

        // Команда для смены пароля
        [RelayCommand]
        public async Task ChangePasswordAsync()
        {
            Page? currentPage = Application.Current?.Windows[0]?.Page;
            if (currentPage == null)
            {
                _logger.LogError("Could not get current page to display alert in ChangePasswordAsync.");
                _toastService.ShowToast("Произошла ошибка при изменении пароля.", isError: true);
                return;
            }
            try
            {
                if (string.IsNullOrWhiteSpace(CurrentPassword))
                {
                    await currentPage.DisplayAlert("Ошибка", "Введите текущий пароль", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(NewPassword))
                {
                    await currentPage.DisplayAlert("Ошибка", "Введите новый пароль", "OK");
                    return;
                }

                if (NewPassword != ConfirmPassword)
                {
                    await currentPage.DisplayAlert("Ошибка", "Новые пароли не совпадают", "OK");
                    return;
                }

                if (NewPassword.Length < 6) // Updated minimum length
                {
                    await currentPage.DisplayAlert("Ошибка", "Новый пароль должен содержать минимум 6 символов", "OK");
                    return;
                }

                var changePasswordRequest = new ChangePasswordRequest
                {
                    CurrentPassword = CurrentPassword,
                    NewPassword = NewPassword
                };

                var apiResponse = await _apiService.PostAsync<ApiResponse>("/api/v1/auth/change-password", changePasswordRequest);

                if (apiResponse.IsSuccess)
                {
                    await currentPage.DisplayAlert("Успех", "Пароль успешно изменен", "OK");
                    CurrentPassword = string.Empty;
                    NewPassword = string.Empty;
                    ConfirmPassword = string.Empty;
                }
                else
                {
                    await currentPage.DisplayAlert("Ошибка", apiResponse.Message ?? "Не удалось изменить пароль", "OK");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password.");
                await currentPage.DisplayAlert("Ошибка", $"Не удалось изменить пароль: {ex.Message}", "OK");
            }
        }

        // Приватный метод для проверки авторизации и валидности UserId
        private async Task<bool> IsUserLoggedInAndUserIdValid()
        {
            var isLoggedIn = await SecureStorage.GetAsync(SecureStorageKeys.IsUserLoggedIn);
            if (string.IsNullOrEmpty(isLoggedIn) || isLoggedIn != "true")
            {
                _logger.LogWarning("User is not logged in. Redirecting to LoginPage.");
                _toastService.ShowToast("Пользователь не авторизован. Пожалуйста, войдите снова.", isError: true);
                await Shell.Current.GoToAsync("///LoginPage");
                return false;
            }

            if (_currentUserId <= 0)
            {
                _logger.LogWarning("UserId is invalid. Redirecting to LoginPage.");
                _toastService.ShowToast("ID пользователя недействителен. Пожалуйста, войдите снова.", isError: true);
                await Shell.Current.GoToAsync("///LoginPage");
                return false;
            }

            return true;
        }
    }
}



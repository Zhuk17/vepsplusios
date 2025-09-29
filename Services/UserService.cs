using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VEPS_Plus.ViewModels;
using Microsoft.Maui.Storage;
using VEPS_Plus.Constants; // ДОБАВЛЕНО: Для SecureStorageKeys

namespace VEPS_Plus.Services
{
    public interface IUserService
    {
        Task<User> GetCurrentUserAsync();
        Task<Profile> GetCurrentProfileAsync();
        Task<bool> UpdateProfileAsync(ProfileUpdateRequest request);
        Task<bool> ChangePasswordAsync(ChangePasswordRequest request);
        Task<bool> HasPermissionAsync(string requiredRole);
        Task<string> GetCurrentUserRoleAsync();
        Task<string> GetCurrentUsernameAsync();
        Task<bool> IsUserLoggedInAsync();
        Task LogoutAsync();
    }

    public class UserService : IUserService
    {
        private readonly ApiService _apiService;

        public UserService(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<User> GetCurrentUserAsync()
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (userId <= 0) return null;

                var response = await _apiService.GetAsync<ApiResponse<User>>($"/api/v1/users/{userId}");
                return response?.IsSuccess == true ? response.Data : null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting current user: {ex.Message}");
                return null;
            }
        }

        public async Task<Profile> GetCurrentProfileAsync()
        {
            try
            {
                var response = await _apiService.GetAsync<ApiResponse<Profile>>("/api/v1/profile");
                return response?.IsSuccess == true ? response.Data : null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting current profile: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateProfileAsync(ProfileUpdateRequest request)
        {
            try
            {
                var response = await _apiService.PutAsync<ApiResponse<Profile>>("/api/v1/profile", request);
                return response?.IsSuccess == true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating profile: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request)
        {
            try
            {
                var response = await _apiService.PostAsync<ApiResponse>("/api/v1/auth/change-password", request);
                return response?.IsSuccess == true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error changing password: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> HasPermissionAsync(string requiredRole)
        {
            try
            {
                var currentRole = await GetCurrentUserRoleAsync();
                if (string.IsNullOrEmpty(currentRole)) return false;

                return UserRoles.HasPermission(currentRole, requiredRole);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking permission: {ex.Message}");
                return false;
            }
        }

        public async Task<string> GetCurrentUserRoleAsync()
        {
            try
            {
                return await SecureStorage.GetAsync(SecureStorageKeys.UserRole) ?? string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting current user role: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<string> GetCurrentUsernameAsync()
        {
            try
            {
                return await SecureStorage.GetAsync(SecureStorageKeys.Username) ?? string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting current username: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<bool> IsUserLoggedInAsync()
        {
            try
            {
                var isLoggedIn = await SecureStorage.GetAsync(SecureStorageKeys.IsUserLoggedIn);
                return isLoggedIn == "true";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking login status: {ex.Message}");
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                await _apiService.ClearAuthToken();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during logout: {ex.Message}");
            }
        }

        private async Task<int> GetCurrentUserIdAsync()
        {
            try
            {
                var userIdString = await SecureStorage.GetAsync(SecureStorageKeys.UserId);
                System.Diagnostics.Debug.WriteLine($"[UserService] GetCurrentUserIdAsync: Raw UserId from SecureStorage: '{userIdString ?? "null"}'");
                if (int.TryParse(userIdString, out int userId))
                {
                    System.Diagnostics.Debug.WriteLine($"[UserService] GetCurrentUserIdAsync: Parsed UserId: {userId}");
                    return userId;
                }
                System.Diagnostics.Debug.WriteLine("[UserService] GetCurrentUserIdAsync: Failed to parse UserId. Returning 0.");
                return 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting current user ID: {ex.Message}");
                return 0;
            }
        }
    }
}

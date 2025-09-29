using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using VEPS_Plus.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using System.Collections.Generic;
using VEPS_Plus.Constants;
using Microsoft.Extensions.Logging;

namespace VEPS_Plus.Services
{
    public class ErrorResponse
    {
        public string Error { get; set; }
    }

    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private string _jwtToken;
#if DEBUG
        private static bool _isOfflineMode = false;

        private static readonly Dictionary<string, Func<object>> _mockDataFactories = new()
        {
            ["/api/v1/profile"] = () => new Profile
            {
                Id = 70,
                FullName = "Павел Тимофеев",
                Email = "p.timofeev@company.com",
                Phone = "+7 (999) 123-45-67",
                UpdatedAt = DateTime.Now
            },
            ["/api/v1/users/70"] = () => new User
            {
                Id = 70,
                Username = "p.timofeev",
                Role = "engineer3",
                CreatedAt = DateTime.Parse("2023-01-15T09:00:00")
            },
            ["/api/v1/users/80"] = () => new User
            {
                Id = 80,
                Username = "al.lukashev",
                Role = "boss",
                CreatedAt = DateTime.Parse("2022-06-01T09:00:00")
            },
            ["/api/v1/dashboard"] = () => new
            {
                unreadNotifications = 3,
                recentActivities = new[]
                {
                    new { type = "timesheet", message = "Заполнен табель за текущую неделю", date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd") },
                    new { type = "fuel", message = "Заправка автомобиля", date = DateTime.Now.AddDays(-2).ToString("yyyy-MM-dd") }
                }
            },
            ["/api/v1/settings"] = () => new Settings
            {
                Id = 1,
                UserId = 70,
                DarkTheme = true,
                PushNotifications = true,
                Language = "ru",
                UpdatedAt = DateTime.Now
            }
        };
#endif

        private readonly ILogger<ApiService> _logger;

        private static object GetUserDataById(int userId)
        {
            switch (userId)
            {
                case 70:
                    return new User
                    {
                        Id = 70,
                        Username = "p.timofeev",
                        Role = "engineer3",
                        CreatedAt = DateTime.Parse("2023-01-15T09:00:00")
                    };
                case 80:
                    return new User
                    {
                        Id = 80,
                        Username = "al.lukashev",
                        Role = "boss",
                        CreatedAt = DateTime.Parse("2022-06-01T09:00:00")
                    };
                default:
                    return new User
                    {
                        Id = userId,
                        Username = "unknown",
                        Role = "user",
                        CreatedAt = DateTime.Now
                    };
            }
        }

        public ApiService(ILogger<ApiService> logger)
        {
            _logger = logger;
            Uri baseUri;
            baseUri = new Uri("http://91.190.80.208:5000");

            var handler = new HttpClientHandler
            {
                // UseProxy = false,
                UseDefaultCredentials = false,
                // Proxy = null,
#if DEBUG
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
#endif
            };
            _httpClient = new HttpClient(handler) { BaseAddress = baseUri };
            _httpClient.DefaultRequestHeaders.UserAgent.Clear();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("VEPS-Plus-App/1.0");
            _httpClient.DefaultRequestHeaders.Add("bypass-tunnel-reminder", "true");
        }

        public void SetAuthToken(string token)
        {
            _jwtToken = token;
            if (!string.IsNullOrEmpty(_jwtToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);
                _logger.LogDebug("[ApiService] SetAuthToken: Authorization header set for token: {TokenChars}...", _jwtToken.Substring(0, Math.Min(_jwtToken.Length, 10)));
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
                _logger.LogDebug("[ApiService] SetAuthToken: Authorization header cleared (token was null or empty).");
            }
        }

        public async Task ClearAuthToken()
        {
            _jwtToken = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
            _logger.LogDebug("[ApiService] ClearAuthToken: Attempting to clear SecureStorage keys.");
            SecureStorage.Remove(SecureStorageKeys.JwtToken);
            SecureStorage.Remove(SecureStorageKeys.UserId);
            await SecureStorage.SetAsync(SecureStorageKeys.IsUserLoggedIn, "false");
            _logger.LogDebug("[ApiService] ClearAuthToken: SecureStorage cleared. IsUserLoggedIn set to 'false'.");
            _logger.LogDebug("[ApiService] ClearAuthToken: All auth data cleared.");
        }

#if DEBUG
        public void EnableOfflineMode()
        {
            _isOfflineMode = true;
            _logger.LogDebug("[ApiService] Offline mode enabled");
        }

        public void DisableOfflineMode()
        {
            _isOfflineMode = false;
            _logger.LogDebug("[ApiService] Offline mode disabled");
        }
#endif

        public async Task InitializeAuthAsync()
        {
            _logger.LogDebug("[ApiService] InitializeAuthAsync: Attempting to load JWT token from SecureStorage.");
            var token = await SecureStorage.GetAsync(SecureStorageKeys.JwtToken);
            var userId = await SecureStorage.GetAsync(SecureStorageKeys.UserId);
            var isLoggedIn = await SecureStorage.GetAsync(SecureStorageKeys.IsUserLoggedIn);

            _logger.LogDebug("[ApiService] InitializeAuthAsync: SecureStorage values - JwtToken: {TokenPresent}, UserId: {UserId}, IsUserLoggedIn: {IsLoggedIn}", 
                string.IsNullOrEmpty(token) ? "(empty)" : "(present)", userId ?? "(empty)", isLoggedIn ?? "(empty)");

            if (!string.IsNullOrEmpty(token))
            {
                _logger.LogDebug("[ApiService] InitializeAuthAsync: Token found in SecureStorage (length: {TokenLength}).", token.Length);
            }
            else
            {
                _logger.LogDebug("[ApiService] InitializeAuthAsync: No token found in SecureStorage.");
            }
            SetAuthToken(token);
        }

        private async Task HandleError(HttpResponseMessage response)
        {
            string rawContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("[ApiService] HandleError: Received HTTP Status Code: {StatusCode}. Raw Content: {RawContent}", response.StatusCode, rawContent);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await ClearAuthToken();
                await Shell.Current.GoToAsync("//LoginPage");
                throw new UnauthorizedException($"Неверные учетные данные или сессия истекла. Пожалуйста, проверьте логин и пароль. API Response: {rawContent}");
            }
            else if (response.StatusCode == (HttpStatusCode)511)
            {
                _logger.LogError("[ApiService] Received 511 NetworkAuthenticationRequired. This might be a proxy issue, not API. Raw content: {RawContent}", rawContent);
                throw new HttpRequestException($"Ошибка сервера: NetworkAuthenticationRequired (511). Это может быть проблема с прокси-сервером.");
            }

            string errorJson = rawContent;
            ErrorResponse errorResponse = null;
            string contentType = response.Content.Headers.ContentType?.MediaType;

            if (!string.IsNullOrEmpty(errorJson) && contentType == "application/json")
            {
                try
                {
                    errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Ошибка десериализации ответа об ошибке. Raw JSON: {ErrorJson}", errorJson);
                }
            }
            else
            {
                _logger.LogWarning("API вернул не-JSON ответ или пустой ответ. Content-Type: {ContentType}, Raw Content: {RawContent}", contentType ?? "null", errorJson ?? "(empty)");
            }

            string errorMessage = errorResponse?.Error ?? $"Ошибка сервера: {response.StatusCode} ({(int)response.StatusCode})";
            throw new HttpRequestException(errorMessage);
        }

        public async Task<T> GetAsync<T>(string endpoint)
        {
            string cleanEndpoint = endpoint.TrimStart('/');
            
            _logger.LogDebug("[ApiService] Attempting GET Request to: {Endpoint}", cleanEndpoint);
#if DEBUG
            if (_isOfflineMode && _mockDataFactories.ContainsKey(cleanEndpoint))
            {
                _logger.LogDebug("[ApiService] Offline mode: returning mock data for {Endpoint}", cleanEndpoint);
                var mockDataFactory = _mockDataFactories[cleanEndpoint];
                var mockData = mockDataFactory();
                var json = JsonSerializer.Serialize(mockData);
                return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            if (_isOfflineMode && cleanEndpoint.StartsWith("api/v1/users/"))
            {
                var userId = cleanEndpoint.Replace("api/v1/users/", "");
                if (int.TryParse(userId, out int id))
                {
                    _logger.LogDebug("[ApiService] Offline mode: returning user data for ID {UserId}", id);
                    var userData = GetUserDataById(id);
                    var json = JsonSerializer.Serialize(userData);
                    return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
#endif
            var authHeader = _httpClient.DefaultRequestHeaders.Authorization;
            _logger.LogDebug("[ApiService] GET Request to: {BaseAddress}{Endpoint}. Auth Header Present: {AuthPresent}", 
                _httpClient.BaseAddress, cleanEndpoint, authHeader != null);
            if (authHeader != null) _logger.LogDebug("[ApiService] Auth Header Scheme: {Scheme}, Token Start: {TokenStart}...", authHeader.Scheme, authHeader.Parameter?.Substring(0, Math.Min(authHeader.Parameter.Length, 10)));
            
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(cleanEndpoint);

                if (response.IsSuccessStatusCode)
                {
                    string responseJson = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrEmpty(responseJson))
                    {
                        return default(T)!;
                    }
                    return JsonSerializer.Deserialize<T>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
                }
                else
                {
                    await HandleError(response);
                    return default(T)!;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[ApiService] Network error for {Endpoint}", cleanEndpoint);
#if DEBUG
                EnableOfflineMode();
                if (_mockDataFactories.ContainsKey(cleanEndpoint))
                {
                    _logger.LogDebug("[ApiService] Fallback to mock data for {Endpoint}", cleanEndpoint);
                    var mockDataFactory = _mockDataFactories[cleanEndpoint];
                    var mockData = mockDataFactory();
                    var json = JsonSerializer.Serialize(mockData);
                    return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }

                if (cleanEndpoint.StartsWith("api/v1/users/"))
                {
                    var userId = cleanEndpoint.Replace("api/v1/users/", "");
                    if (int.TryParse(userId, out int id))
                    {
                        _logger.LogDebug("[ApiService] Fallback to user data for ID {UserId}", id);
                        var userData = GetUserDataById(id);
                        var json = JsonSerializer.Serialize(userData);
                        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                }
#endif
                throw;
            }
        }

        public async Task<T> PostAsync<T>(string endpoint, object data)
        {
            string cleanEndpoint = endpoint.TrimStart('/');
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogDebug("[ApiService] Attempting POST Request to: {Endpoint} with data: {JsonData}", cleanEndpoint, json);
            var authHeader = _httpClient.DefaultRequestHeaders.Authorization;
            _logger.LogDebug("[ApiService] POST Request to: {BaseAddress}{Endpoint}. Auth Header Present: {AuthPresent}", 
                _httpClient.BaseAddress, cleanEndpoint, authHeader != null);
            if (authHeader != null) _logger.LogDebug("[ApiService] Auth Header Scheme: {Scheme}, Token Start: {TokenStart}...", authHeader.Scheme, authHeader.Parameter?.Substring(0, Math.Min(authHeader.Parameter.Length, 10)));

            HttpResponseMessage response = await _httpClient.PostAsync(cleanEndpoint, content);

            if (response.IsSuccessStatusCode)
            {
                string responseJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(responseJson))
                {
                    return default(T)!;
                }
                return JsonSerializer.Deserialize<T>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            }
            else
            {
                await HandleError(response);
                return default(T)!;
            }
        }

        public async Task<T> PutAsync<T>(string endpoint, object data)
        {
            string cleanEndpoint = endpoint.TrimStart('/');
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogDebug("[ApiService] Attempting PUT Request to: {Endpoint} with data: {JsonData}", cleanEndpoint, json);
            var authHeader = _httpClient.DefaultRequestHeaders.Authorization;
            _logger.LogDebug("[ApiService] PUT Request to: {BaseAddress}{Endpoint}. Auth Header Present: {AuthPresent}", 
                _httpClient.BaseAddress, cleanEndpoint, authHeader != null);
            if (authHeader != null) _logger.LogDebug("[ApiService] Auth Header Scheme: {Scheme}, Token Start: {TokenStart}...", authHeader.Scheme, authHeader.Parameter?.Substring(0, Math.Min(authHeader.Parameter.Length, 10)));

            HttpResponseMessage response = await _httpClient.PutAsync(cleanEndpoint, content);

            if (response.IsSuccessStatusCode)
            {
                string responseJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(responseJson))
                {
                    return default(T)!;
                }
                return JsonSerializer.Deserialize<T>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            }
            else
            {
                await HandleError(response);
                return default(T)!;
            }
        }

        public async Task<T> PatchAsync<T>(string endpoint, object data)
        {
            string cleanEndpoint = endpoint.TrimStart('/');
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogDebug("[ApiService] Attempting PATCH Request to: {Endpoint} with data: {JsonData}", cleanEndpoint, json);
            var authHeader = _httpClient.DefaultRequestHeaders.Authorization;
            _logger.LogDebug("[ApiService] PATCH Request to: {BaseAddress}{Endpoint}. Auth Header Present: {AuthPresent}", 
                _httpClient.BaseAddress, cleanEndpoint, authHeader != null);
            if (authHeader != null) _logger.LogDebug("[ApiService] Auth Header Scheme: {Scheme}, Token Start: {TokenStart}...", authHeader.Scheme, authHeader.Parameter?.Substring(0, Math.Min(authHeader.Parameter.Length, 10)));

            var request = new HttpRequestMessage(new HttpMethod("PATCH"), cleanEndpoint) { Content = content };
            HttpResponseMessage response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                string responseJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(responseJson))
                {
                    return default(T)!;
                }
                return JsonSerializer.Deserialize<T>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            }
            else
            {
                await HandleError(response);
                return default(T)!;
            }
        }

        public async Task<ApiResponse> DeleteAsync(string endpoint)
        {
            string cleanEndpoint = endpoint.TrimStart('/');
            _logger.LogDebug("[ApiService] Attempting DELETE Request to: {Endpoint}", cleanEndpoint);
            var authHeader = _httpClient.DefaultRequestHeaders.Authorization;
            _logger.LogDebug("[ApiService] DELETE Request to: {BaseAddress}{Endpoint}. Auth Header Present: {AuthPresent}", 
                _httpClient.BaseAddress, cleanEndpoint, authHeader != null);
            if (authHeader != null) _logger.LogDebug("[ApiService] Auth Header Scheme: {Scheme}, Token Start: {TokenStart}...", authHeader.Scheme, authHeader.Parameter?.Substring(0, Math.Min(authHeader.Parameter.Length, 10)));
            HttpResponseMessage response = await _httpClient.DeleteAsync(cleanEndpoint);

            if (response.IsSuccessStatusCode)
            {
                string responseJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(responseJson))
                {
                    return new ApiResponse { IsSuccess = true, Message = "Успешно удалено (пустой ответ)." };
                }
                return JsonSerializer.Deserialize<ApiResponse>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ApiResponse { IsSuccess = false, Message = "Ошибка десериализации ответа." };
            }
            else
            {
                await HandleError(response);
                return new ApiResponse { IsSuccess = false, Message = "Ошибка удаления." };
            }
        }

        public async Task<string?> GetWireGuardConfigAsync()
        {
            string cleanEndpoint = "api/v1/WireGuard/config";

            _logger.LogDebug("[ApiService] Attempting GET WireGuard Config Request to: {Endpoint}", cleanEndpoint);
            _logger.LogDebug("[ApiService] GET WireGuard Config Request to: {BaseAddress}{Endpoint}. Auth Header Present: {AuthPresent}", 
                _httpClient.BaseAddress, cleanEndpoint, _httpClient.DefaultRequestHeaders.Authorization != null);
            if (_httpClient.DefaultRequestHeaders.Authorization != null) _logger.LogDebug("[ApiService] WireGuard Config Auth Header Scheme: {Scheme}, Token Start: {TokenStart}...", _httpClient.DefaultRequestHeaders.Authorization.Scheme, _httpClient.DefaultRequestHeaders.Authorization.Parameter?.Substring(0, Math.Min(_httpClient.DefaultRequestHeaders.Authorization.Parameter.Length, 10)));

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(cleanEndpoint);

                if (response.IsSuccessStatusCode)
                {
                    string configContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("[ApiService] Successfully retrieved WireGuard config (length: {ConfigLength} characters).", configContent.Length);
                    return configContent;
                }
                else
                {
                    _logger.LogWarning("[ApiService] Failed to retrieve WireGuard config. Status Code: {StatusCode}. Message: {Message}", response.StatusCode, await response.Content.ReadAsStringAsync());
                    await HandleError(response);
                    return null;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[ApiService] Network error when fetching WireGuard config for {Endpoint}", cleanEndpoint);
                throw;
            }
        }
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using VEPS_Plus.Services;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Globalization; // Для CultureInfo
using VEPS_Plus.Constants; // ДОБАВЛЕНО: Для SecureStorageKeys
using Microsoft.Maui.ApplicationModel; // ДОБАВЛЕНО: Для MainThread

namespace VEPS_Plus.ViewModels
{
    // Модель для строки табеля, возвращаемой с сервера (соответствует TimesheetResponseDto на сервере)
    public class TimesheetRowDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("fio")]
        public string? Fio { get; set; }

        [JsonPropertyName("project")]
        public string? Project { get; set; }

        [JsonPropertyName("hours")]
        public int Hours { get; set; }

        [JsonPropertyName("businessTrip")]
        public bool BusinessTrip { get; set; }

        [JsonPropertyName("comment")]
        public string? Comment { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }
    }

    public partial class TimesheetViewModel : ObservableObject
    {
        // --- ObservableProperties для связи с UI ---
        [ObservableProperty]
        private ObservableCollection<TimesheetNotificationDto> notifications = new ObservableCollection<TimesheetNotificationDto>();

        [ObservableProperty]
        private ObservableCollection<TimesheetResponseDto> rows = new ObservableCollection<TimesheetResponseDto>();

        // Списки для Picker'ов
        [ObservableProperty]
        private ObservableCollection<string> projects = new ObservableCollection<string> { "Все проекты", "9401", "9005", "9002", "9403", "9404", "9402", "9405", "1887", "2119", "9003", "2129" };

        [ObservableProperty]
        private ObservableCollection<int> hoursOptions = new ObservableCollection<int>(Enumerable.Range(1, 24)); // Changed to 1-24 hours

        [ObservableProperty]
        private ObservableCollection<string> workers = new ObservableCollection<string> { "Все работники", "Работник А.А.", "Работник Б.Б." }; // Added "Все работники"

        [ObservableProperty]
        private ObservableCollection<string> statuses = new ObservableCollection<string> { "На рассмотрении", "Одобрено", "Отклонено" };

        // Поля для добавления новой записи
        [ObservableProperty]
        private DateTime date = DateTime.Today;

        [ObservableProperty]
        private string? selectedProject;

        [ObservableProperty]
        private int selectedHours;

        [ObservableProperty]
        private bool businessTrip;

        [ObservableProperty]
        private string? comment;

        // Поля для сортировки/фильтрации
        [ObservableProperty]
        private string? selectedWorker;

        [ObservableProperty]
        private DateTime startDate = DateTime.Today.AddDays(-7);

        [ObservableProperty]
        private DateTime endDate = DateTime.Today;

        [ObservableProperty]
        private string? selectedFilterProject;

        [ObservableProperty]
        private string? selectedFilterStatus;

        // Вычисляемое свойство
        [ObservableProperty]
        private int workedHours;

        // Количество рабочих часов в месяце
        [ObservableProperty]
        private int workingHoursInMonth = 168; // Значение по умолчанию

        // Выбранная строка для одобрения/отклонения
        [ObservableProperty]
        private TimesheetResponseDto? selectedRow;

        [ObservableProperty] // Make it an ObservableProperty
        private CultureInfo culture = new CultureInfo("ru-RU"); // Initialize with Russian culture

        // --- ObservableProperties для управления UI-видимостью (расширение/сворачивание) ---
        [ObservableProperty]
        private bool isNotificationsBlockVisible = false; // Для Not_Border
        [ObservableProperty]
        private double notificationsBlockHeight = 50; // Для Not_Big_Border
        [ObservableProperty]
        private double notificationsRollButtonRotation = 0; // Для btn_roll_not Rotation

        [ObservableProperty]
        private bool isHistoryBlockVisible = false; // Для Border_Small_History
        [ObservableProperty]
        private double historyBlockHeight = 50; // Для Border_Big_History
        [ObservableProperty]
        private double historyRollButtonRotation = 0; // Для btn_roll_history Rotation

        [ObservableProperty]
        private bool isLeader = false; // Для управления видимостью кнопок одобрения/отклонения

        private readonly ApiService _apiService;
        private int _currentUserId; // Поле для хранения UserId текущего пользователя

        public TimesheetViewModel(ApiService apiService)
        {
            _apiService = apiService;
            System.Diagnostics.Debug.WriteLine("[TimesheetViewModel] Constructor called."); // ДОБАВЛЕНО ЛОГИРОВАНИЕ ИНИЦИАЛИЗАЦИИ
            SelectedFilterProject = "Все проекты"; // Устанавливаем "Все проекты" по умолчанию
            // Загрузка UserId при инициализации ViewModel
            // и последующий вызов загрузки данных.
            // Task.Run(async () => await LoadInitialDataAsync()); // Раскомментировано и добавлено async/await
            System.Diagnostics.Debug.WriteLine($"[TimesheetViewModel] Constructor: StartDate={StartDate:yyyy-MM-dd}, EndDate={EndDate:yyyy-MM-dd}");
        }

        // Метод входа для загрузки UserId и последующей загрузки данных
        public async Task LoadInitialDataAsync()
        {
            System.Diagnostics.Debug.WriteLine("[TimesheetViewModel] LoadInitialDataAsync: Starting initial data load.");
            // Проверяем _currentUserId перед попыткой чтения из SecureStorage, чтобы увидеть, если он был сброшен
            System.Diagnostics.Debug.WriteLine($"[TimesheetViewModel] LoadInitialDataAsync: Initial _currentUserId value: {_currentUserId} (before SecureStorage.GetAsync)."); // ДОБАВЛЕНО ЛОГИРОВАНИЕ

            var userIdString = await SecureStorage.GetAsync(SecureStorageKeys.UserId);
            var userRole = await SecureStorage.GetAsync(SecureStorageKeys.UserRole); // Get user role
            IsLeader = UserRoles.HasPermission(userRole, UserRoles.Boss); // ИСПРАВЛЕНО: Set IsLeader based on role hierarchy

            System.Diagnostics.Debug.WriteLine($"[TimesheetViewModel] LoadInitialDataAsync: Retrieved UserId='{userIdString ?? "null"}', UserRole='{userRole ?? "null"}'. IsLeader: {IsLeader}");

            if (int.TryParse(userIdString, out int userId) && userId > 0)
            {
                _currentUserId = userId;
                System.Diagnostics.Debug.WriteLine($"[TimesheetViewModel] LoadInitialDataAsync: _currentUserId set to {userId} after parsing SecureStorage. IsLeader: {IsLeader}"); // ДОБАВЛЕНО ЛОГИРОВАНИЕ
                // Опционально: Загрузка уведомлений (если они здесь нужны) и табелей
                await LoadNotificationsAsync(); // Раскомментировано и добавлено async/await
                System.Diagnostics.Debug.WriteLine($"[TimesheetViewModel] LoadInitialDataAsync: Calling LoadTimesheetsAsync. Current _currentUserId: {_currentUserId}"); // ДОБАВЛЕНО ЛОГИРОВАНИЕ
                await LoadTimesheetsAsync();
                await LoadWorkersAsync();
                await LoadWorkingHoursAsync(); // Загружаем информацию о рабочих часах

                // Опционально: Инициализация календаря
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[TimesheetViewModel] LoadInitialDataAsync: UserId not found or invalid from SecureStorage. Navigating to LoginPage."); // ДОБАВЛЕНО ЛОГИРОВАНИЕ
                // Если userId не найден, перенаправляем на логин
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка авторизации", "ID пользователя не найден. Пожалуйста, войдите снова.", "OK");
                    await Shell.Current.GoToAsync("//LoginPage");
                });
            }
        }

        // Команда для загрузки уведомлений в табеле
        [RelayCommand]
        public async Task LoadNotificationsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[TimesheetViewModel] LoadNotificationsAsync: Checking User ID validity... Current _currentUserId: {_currentUserId}");
                if (!await IsUserLoggedInAndUserIdValid()) return;

                Notifications.Clear();

                // Загружаем последние записи табелей для отображения в уведомлениях
                // API автоматически фильтрует по текущему пользователю для не-boss ролей
                var query = $"/api/v1/timesheets?startDate={DateTime.Today.AddDays(-30):yyyy-MM-dd}&endDate={DateTime.Today:yyyy-MM-dd}";
                var apiResponse = await _apiService.GetAsync<ApiResponse<List<TimesheetResponseDto>>>(query);
                
                if (apiResponse.IsSuccess && apiResponse.Data != null)
                {
                    var userTimesheets = apiResponse.Data;
                    var currentUsername = await GetCurrentUsernameAsync();
                    var userRole = await SecureStorage.GetAsync(SecureStorageKeys.UserRole);
                    
                    // Для boss-роли показываем все записи, для обычных пользователей - только свои
                    if (userRole != "boss" && !string.IsNullOrEmpty(currentUsername))
                    {
                        userTimesheets = userTimesheets.Where(t => t.Fio == currentUsername).ToList();
                        System.Diagnostics.Debug.WriteLine($"[TimesheetViewModel] LoadNotificationsAsync: Filtered to current user ({currentUsername}) records only");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[TimesheetViewModel] LoadNotificationsAsync: Showing all users records (boss role or no username)");
                    }
                    
                    // Берем последние 10 записей и преобразуем в формат уведомлений
                    var recentTimesheets = userTimesheets.Take(10).ToList();
                    
                    foreach (var timesheet in recentTimesheets)
                    {
                        var notification = new TimesheetNotificationDto
                        {
                            Date = timesheet.Date,
                            Project = timesheet.Project,
                            Hours = timesheet.Hours,
                            Status = timesheet.Status,
                            Comment = timesheet.Comment
                        };
                        Notifications.Add(notification);
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[TimesheetViewModel] LoadNotificationsAsync: Loaded {recentTimesheets.Count} notifications");
                }
                else 
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        Page? currentPage = Application.Current?.Windows[0]?.Page;
                        if (currentPage == null) { System.Diagnostics.Debug.WriteLine("ERROR: Could not get current page to display alert in LoadNotificationsAsync (error)."); return; }
                        await currentPage.DisplayAlert("Ошибка", apiResponse.Message ?? "Не удалось загрузить уведомления табеля.", "OK");
                    });
                }
            }
            catch (UnauthorizedException ex) { MainThread.BeginInvokeOnMainThread(async () => await HandleAuthError(ex)); }
            catch (HttpRequestException ex) { MainThread.BeginInvokeOnMainThread(async () => await HandleNetworkError(ex)); }
            catch (Exception ex) { MainThread.BeginInvokeOnMainThread(async () => await HandleGenericError(ex)); }
        }

        [RelayCommand]
        public async Task LoadWorkersAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[TimesheetViewModel] LoadWorkersAsync: Starting workers load. Current _currentUserId: {_currentUserId}");
                if (!await IsUserLoggedInAndUserIdValid()) return;

                var apiResponse = await _apiService.GetAsync<ApiResponse<List<string>>>("/api/v1/users/usernames");
                System.Diagnostics.Debug.WriteLine($"[TimesheetViewModel] LoadWorkersAsync: API Response - IsSuccess: {apiResponse?.IsSuccess}, Data Count: {apiResponse?.Data?.Count ?? 0}");
                
                if (apiResponse.IsSuccess && apiResponse.Data != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Workers.Clear();
                        Workers.Add("Все работники"); // Добавляем опцию "Все работники"
                        foreach (var worker in apiResponse.Data)
                        {
                            Workers.Add(worker);
                            System.Diagnostics.Debug.WriteLine($"[TimesheetViewModel] LoadWorkersAsync: Added worker: {worker}");
                        }
                        SelectedWorker = Workers.FirstOrDefault(); // Выбираем первый элемент по умолчанию
                        System.Diagnostics.Debug.WriteLine($"[TimesheetViewModel] LoadWorkersAsync: Total workers loaded: {Workers.Count}, Selected: {SelectedWorker}");
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[TimesheetViewModel] LoadWorkersAsync: API call failed. Message: {apiResponse?.Message}");
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        Page? currentPage = Application.Current?.Windows[0]?.Page;
                        if (currentPage == null) { System.Diagnostics.Debug.WriteLine("ERROR: Could not get current page to display alert in LoadWorkersAsync (error)."); return; }
                        await currentPage.DisplayAlert("Ошибка", apiResponse.Message ?? "Не удалось загрузить список работников.", "OK");
                    });
                }
            }
            catch (UnauthorizedException ex) 
            { 
                System.Diagnostics.Debug.WriteLine($"[TimesheetViewModel] LoadWorkersAsync: UnauthorizedException: {ex.Message}");
                MainThread.BeginInvokeOnMainThread(async () => await HandleAuthError(ex)); 
            }
            catch (HttpRequestException ex) 
            { 
                System.Diagnostics.Debug.WriteLine($"[TimesheetViewModel] LoadWorkersAsync: HttpRequestException: {ex.Message}");
                MainThread.BeginInvokeOnMainThread(async () => await HandleNetworkError(ex)); 
            }
            catch (Exception ex) 
            { 
                System.Diagnostics.Debug.WriteLine($"[TimesheetViewModel] LoadWorkersAsync: Exception: {ex.Message}");
                MainThread.BeginInvokeOnMainThread(async () => await HandleGenericError(ex)); 
            }
        }

        [RelayCommand]
        public async Task LoadWorkingHoursAsync()
        {
            try
            {
                if (!await IsUserLoggedInAndUserIdValid()) return;

                var apiResponse = await _apiService.GetAsync<Dashboard>("/api/v1/dashboard");
                if (apiResponse != null)
                {
                    WorkingHoursInMonth = apiResponse.WorkingHoursInMonth;
                    System.Diagnostics.Debug.WriteLine($"[TimesheetViewModel] LoadWorkingHoursAsync: Working hours in month set to {WorkingHoursInMonth}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[TimesheetViewModel] LoadWorkingHoursAsync: Failed to load dashboard data, using default value");
                    WorkingHoursInMonth = 168; // Значение по умолчанию
                }
            }
            catch (UnauthorizedException ex) { MainThread.BeginInvokeOnMainThread(async () => await HandleAuthError(ex)); }
            catch (HttpRequestException ex) { MainThread.BeginInvokeOnMainThread(async () => await HandleNetworkError(ex)); }
            catch (Exception ex) 
            { 
                System.Diagnostics.Debug.WriteLine($"[TimesheetViewModel] LoadWorkingHoursAsync: Exception: {ex.Message}");
                WorkingHoursInMonth = 168; // Значение по умолчанию при ошибке
            }
        }

        [RelayCommand]
        public async Task LoadTimesheetsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[TimesheetViewModel] LoadTimesheetsAsync: Checking User ID validity..."); // ДОБАВЛЕНО ЛОГИРОВАНИЕ
                if (!await IsUserLoggedInAndUserIdValid()) return;

                Rows.Clear();

                // API вызов для загрузки табелей
                var query = $"/api/v1/timesheets?startDate={StartDate:yyyy-MM-dd}&endDate={EndDate:yyyy-MM-dd}";
                System.Diagnostics.Debug.WriteLine($"[TimesheetViewModel] LoadTimesheetsAsync: API Request Query: {query}"); // ДОБАВЛЕНО ЛОГИРОВАНИЕ
                if (!string.IsNullOrEmpty(SelectedWorker) && SelectedWorker != "Все работники") query += $"&worker={SelectedWorker}";
                if (!string.IsNullOrEmpty(SelectedFilterProject) && SelectedFilterProject != "Все проекты") query += $"&project={SelectedFilterProject}";
                if (!string.IsNullOrEmpty(SelectedFilterStatus)) query += $"&status={SelectedFilterStatus}";

                var apiResponse = await _apiService.GetAsync<ApiResponse<List<TimesheetResponseDto>>>(query);
                if (apiResponse.IsSuccess && apiResponse.Data != null)
                {
                    foreach (var timesheet in apiResponse.Data) { Rows.Add(timesheet); }
                    // Считаем только одобренные часы
                    WorkedHours = Rows.Where(t => t.Status == "Одобрено").Sum(t => t.Hours);
                }
                else 
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        Page? currentPage = Application.Current?.Windows[0]?.Page;
                        if (currentPage == null) { System.Diagnostics.Debug.WriteLine("ERROR: Could not get current page to display alert in LoadTimesheetsAsync (error)."); return; }
                        await currentPage.DisplayAlert("Ошибка", apiResponse.Message ?? "Не удалось загрузить табели.", "OK");
                    });
                }
            }
            catch (UnauthorizedException ex) { MainThread.BeginInvokeOnMainThread(async () => await HandleAuthError(ex)); }
            catch (HttpRequestException ex) { MainThread.BeginInvokeOnMainThread(async () => await HandleNetworkError(ex)); }
            catch (Exception ex) { MainThread.BeginInvokeOnMainThread(async () => await HandleGenericError(ex)); }
        }

        [RelayCommand]
        public async Task AddTimesheetAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SelectedProject) || SelectedHours <= 0 || Date == default)
                {
                    MainThread.BeginInvokeOnMainThread(async () => await Application.Current.MainPage.DisplayAlert("Ошибка", "Заполните все поля для добавления", "OK"));
                    return;
                }

                System.Diagnostics.Debug.WriteLine("[TimesheetViewModel] AddTimesheetAsync: Checking User ID validity..."); // ДОБАВЛЕНО ЛОГИРОВАНИЕ
                if (!await IsUserLoggedInAndUserIdValid()) return;

                // API вызов для добавления новой записи табеля
                var requestData = new TimesheetCreateRequest { Date = Date, Project = SelectedProject, Hours = SelectedHours, BusinessTrip = BusinessTrip, Comment = Comment }; // Changed to TimesheetCreateRequest
                var apiResponse = await _apiService.PostAsync<ApiResponse<TimesheetResponseDto>>("/api/v1/timesheets", requestData); // Changed to TimesheetResponseDto
                if (apiResponse.IsSuccess && apiResponse.Data != null) 
                {
                    MainThread.BeginInvokeOnMainThread(async () => 
                    {
                        Page? currentPage = Application.Current?.Windows[0]?.Page;
                        if (currentPage == null) { System.Diagnostics.Debug.WriteLine("ERROR: Could not get current page to display alert in AddTimesheetAsync (success)."); return; }
                        Rows.Add(apiResponse.Data); await currentPage.DisplayAlert("Успех", apiResponse.Message, "OK"); 
                    }); 
                    ResetForm(); 
                    await LoadTimesheetsAsync(); 
                }
                else 
                {
                    MainThread.BeginInvokeOnMainThread(async () => 
                    {
                        Page? currentPage = Application.Current?.Windows[0]?.Page;
                        if (currentPage == null) { System.Diagnostics.Debug.WriteLine("ERROR: Could not get current page to display alert in AddTimesheetAsync (error)."); return; }
                        await currentPage.DisplayAlert("Ошибка", apiResponse.Message ?? "Не удалось добавить табель.", "OK");
                    }); 
                }
            }
            catch (UnauthorizedException ex) { MainThread.BeginInvokeOnMainThread(async () => await HandleAuthError(ex)); }
            catch (HttpRequestException ex) { MainThread.BeginInvokeOnMainThread(async () => await HandleNetworkError(ex)); }
            catch (Exception ex) { MainThread.BeginInvokeOnMainThread(async () => await HandleGenericError(ex)); }
        }

        [RelayCommand]
        public async Task SortTimesheetsAsync()
        {
            try
            {
                await LoadTimesheetsAsync(); // Просто перезагружаем с новыми фильтрами
                MainThread.BeginInvokeOnMainThread(async () => await Application.Current.MainPage.DisplayAlert("Успех", "Табели отфильтрованы (загружены)", "OK"));
            }
            catch (UnauthorizedException ex) { MainThread.BeginInvokeOnMainThread(async () => await HandleAuthError(ex)); }
            catch (HttpRequestException ex) { MainThread.BeginInvokeOnMainThread(async () => await HandleNetworkError(ex)); }
            catch (Exception ex) { MainThread.BeginInvokeOnMainThread(async () => await HandleGenericError(ex)); }
        }

        [RelayCommand]
        public async Task ApproveTimesheetAsync()
        {
            try
            {
                if (SelectedRow == null)
                {
                    MainThread.BeginInvokeOnMainThread(async () => await Application.Current.MainPage.DisplayAlert("Ошибка", "Выберите запись для одобрения", "OK"));
                    return;
                }
                if (SelectedRow.Status == "Одобрено")
                {
                    MainThread.BeginInvokeOnMainThread(async () => await Application.Current.MainPage.DisplayAlert("Предупреждение", "Запись уже одобрена.", "OK"));
                    return;
                }

                System.Diagnostics.Debug.WriteLine("[TimesheetViewModel] ApproveTimesheetAsync: Checking User ID validity..."); // ДОБАВЛЕНО ЛОГИРОВАНИЕ
                if (!await IsUserLoggedInAndUserIdValid()) return;

                // API вызов для одобрения
                var update = new TimesheetUpdateRequest { Status = "Одобрено" }; // Changed to TimesheetUpdateRequest
                var apiResponse = await _apiService.PatchAsync<ApiResponse<TimesheetResponseDto>>($"/api/v1/timesheets/{SelectedRow.Id}", update); // Changed to TimesheetResponseDto
                if (apiResponse.IsSuccess && apiResponse.Data != null) 
                {
                    MainThread.BeginInvokeOnMainThread(async () => 
                    {
                        Page? currentPage = Application.Current?.Windows[0]?.Page;
                        if (currentPage == null) { System.Diagnostics.Debug.WriteLine("ERROR: Could not get current page to display alert in ApproveTimesheetAsync (success)."); return; }
                        var existing = Rows.FirstOrDefault(r => r.Id == SelectedRow.Id); if (existing != null) { existing.Status = apiResponse.Data.Status; } await currentPage.DisplayAlert("Успех", apiResponse.Message, "OK"); 
                    }); 
                    await LoadTimesheetsAsync(); 
                }
                else 
                {
                    MainThread.BeginInvokeOnMainThread(async () => 
                    {
                        Page? currentPage = Application.Current?.Windows[0]?.Page;
                        if (currentPage == null) { System.Diagnostics.Debug.WriteLine("ERROR: Could not get current page to display alert in ApproveTimesheetAsync (error)."); return; }
                        await currentPage.DisplayAlert("Ошибка", apiResponse.Message ?? "Не удалось одобрить табель.", "OK");
                    });
                }
            }
            catch (UnauthorizedException ex) { MainThread.BeginInvokeOnMainThread(async () => await HandleAuthError(ex)); }
            catch (HttpRequestException ex) { MainThread.BeginInvokeOnMainThread(async () => await HandleNetworkError(ex)); }
            catch (Exception ex) { MainThread.BeginInvokeOnMainThread(async () => await HandleGenericError(ex)); }
        }

        [RelayCommand]
        public async Task RejectTimesheetAsync()
        {
            try
            {
                if (SelectedRow == null)
                {
                    MainThread.BeginInvokeOnMainThread(async () => await Application.Current.MainPage.DisplayAlert("Ошибка", "Выберите запись для отклонения", "OK"));
                    return;
                }
                if (SelectedRow.Status == "Отклонено")
                {
                    MainThread.BeginInvokeOnMainThread(async () => await Application.Current.MainPage.DisplayAlert("Предупреждение", "Запись уже отклонена.", "OK"));
                    return;
                }

                System.Diagnostics.Debug.WriteLine("[TimesheetViewModel] RejectTimesheetAsync: Checking User ID validity..."); // ДОБАВЛЕНО ЛОГИРОВАНИЕ
                if (!await IsUserLoggedInAndUserIdValid()) return;

                // API вызов для отклонения
                var update = new TimesheetUpdateRequest { Status = "Отклонено" }; // Changed to TimesheetUpdateRequest
                var apiResponse = await _apiService.PatchAsync<ApiResponse<TimesheetResponseDto>>($"/api/v1/timesheets/{SelectedRow.Id}", update); // Changed to TimesheetResponseDto
                if (apiResponse.IsSuccess && apiResponse.Data != null) 
                {
                    MainThread.BeginInvokeOnMainThread(async () => 
                    {
                        Page? currentPage = Application.Current?.Windows[0]?.Page;
                        if (currentPage == null) { System.Diagnostics.Debug.WriteLine("ERROR: Could not get current page to display alert in RejectTimesheetAsync (success)."); return; }
                        var existing = Rows.FirstOrDefault(r => r.Id == SelectedRow.Id); if (existing != null) { existing.Status = apiResponse.Data.Status; } await currentPage.DisplayAlert("Успех", apiResponse.Message, "OK"); 
                    }); 
                    await LoadTimesheetsAsync(); 
                }
                else 
                {
                    MainThread.BeginInvokeOnMainThread(async () => 
                    {
                        Page? currentPage = Application.Current?.Windows[0]?.Page;
                        if (currentPage == null) { System.Diagnostics.Debug.WriteLine("ERROR: Could not get current page to display alert in RejectTimesheetAsync (error)."); return; }
                        await currentPage.DisplayAlert("Ошибка", apiResponse.Message ?? "Не удалось отклонить табель.", "OK");
                    }); 
                }
            }
            catch (UnauthorizedException ex) { MainThread.BeginInvokeOnMainThread(async () => await HandleAuthError(ex)); }
            catch (HttpRequestException ex) { MainThread.BeginInvokeOnMainThread(async () => await HandleNetworkError(ex)); }
            catch (Exception ex) { MainThread.BeginInvokeOnMainThread(async () => await HandleGenericError(ex)); }
        }

        private void ResetForm()
        {
            Date = DateTime.Today;
            SelectedProject = null;
            SelectedHours = 0;
            BusinessTrip = false;
            Comment = null;
        }

        // Упрощенный метод для проверки авторизации и UserId
        private async Task<bool> IsUserLoggedInAndUserIdValid()
        {
            System.Diagnostics.Debug.WriteLine($"[TimesheetViewModel] IsUserLoggedInAndUserIdValid: Entering. _currentUserId = {_currentUserId}"); // ДОБАВЛЕНО ЛОГИРОВАНИЕ
            // Если _currentUserId 0, попробуем загрузить его из SecureStorage
            if (_currentUserId <= 0)
            {
                System.Diagnostics.Debug.WriteLine("[TimesheetViewModel] IsUserLoggedInAndUserIdValid: _currentUserId is 0. Attempting to load from SecureStorage...");
                var userIdString = await SecureStorage.GetAsync(SecureStorageKeys.UserId);
                System.Diagnostics.Debug.WriteLine($"[TimesheetViewModel] IsUserLoggedInAndUserIdValid: SecureStorage returned '{userIdString ?? "null"}'. TryParse result: {int.TryParse(userIdString, out int parsedUserId) && parsedUserId > 0}"); // ДОБАВЛЕНО ЛОГИРОВАНИЕ
                if (int.TryParse(userIdString, out int userId) && userId > 0)
                {
                    _currentUserId = userId;
                    System.Diagnostics.Debug.WriteLine($"[TimesheetViewModel] IsUserLoggedInAndUserIdValid: Successfully loaded UserId {userId} from SecureStorage. _currentUserId now: {_currentUserId}"); // ДОБАВЛЕНО ЛОГИРОВАНИЕ
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[TimesheetViewModel] IsUserLoggedInAndUserIdValid: Failed to get valid UserId from SecureStorage. Navigating to LoginPage."); // ДОБАВЛЕНО ЛОГИРОВАНИЕ
                    // Это должно быть обработано в LoadInitialDataAsync, но на всякий случай.
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        System.Diagnostics.Debug.WriteLine("WARNING: _currentUserId is 0 or less in IsUserLoggedInAndUserIdValid (after SecureStorage check).");
                        await Application.Current.MainPage.DisplayAlert("Ошибка авторизации", "ID пользователя не найден. Пожалуйста, войдите снова.", "OK");
                        await Shell.Current.GoToAsync("//LoginPage");
                    });
                    return false;
                }
            }
            System.Diagnostics.Debug.WriteLine($"[TimesheetViewModel] IsUserLoggedInAndUserIdValid: Exiting. Final _currentUserId = {_currentUserId}"); // ДОБАВЛЕНО ЛОГИРОВАНИЕ
            return true; // Теперь достаточно проверки _currentUserId, так как API сервис сам обрабатывает 401
        }

        // Переименованные и унифицированные обработчики ошибок (можно вынести в базовый класс ViewModel)
        private async Task HandleAuthError(UnauthorizedException ex)
        {
            await Application.Current.MainPage.DisplayAlert("Ошибка авторизации", ex.Message, "OK");
            // ApiService уже перенаправляет на LoginPage, здесь дублировать не нужно.
        }

        private async Task HandleNetworkError(HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TimesheetViewModel] Network Error: {ex.Message}. StackTrace: {ex.StackTrace}");
            await Application.Current.MainPage.DisplayAlert("Ошибка сети", $"Не удалось подключиться к серверу: {ex.Message}", "OK");
        }

        private async Task HandleGenericError(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TimesheetViewModel] Generic Error: {ex.Message}. StackTrace: {ex.StackTrace}");
            await Application.Current.MainPage.DisplayAlert("Ошибка", "Произошла ошибка: " + ex.Message, "OK");
        }

        // Вспомогательный метод для получения имени текущего пользователя
        private async Task<string?> GetCurrentUsernameAsync()
        {
            try
            {
                var username = await SecureStorage.GetAsync(SecureStorageKeys.Username);
                System.Diagnostics.Debug.WriteLine($"[TimesheetViewModel] GetCurrentUsernameAsync: Retrieved username: {username ?? "null"}");
                return username;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TimesheetViewModel] GetCurrentUsernameAsync: Error retrieving username: {ex.Message}");
                return null;
            }
        }
    }
}
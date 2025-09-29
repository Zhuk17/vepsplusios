using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage; // Для SecureStorage
using System;
using System.Collections.Generic; // Для List
using System.Collections.ObjectModel; // Для ObservableCollection
using System.Linq; // Для LINQ (FirstOrDefault, Max, Sum, Where)
using System.Threading.Tasks; // Для Task.Run
using VEPS_Plus.Services; // Для ApiService
using System.Text.Json.Serialization; // Для [JsonPropertyName]
using System.Text.Json; // Для JsonSerializerOptions и JsonNamingPolicy
using VEPS_Plus.ViewModels; // Для ApiResponse, ApiResponse<T>, UnauthorizedException (убедитесь, что ApiResponses.cs обновлен)
using VEPS_Plus.Constants; // ДОБАВЛЕНО: Для SecureStorageKeys

namespace VEPS_Plus.ViewModels
{
    public partial class FuelViewModel : ObservableObject
    {
        // --- Поля для FuelPage (добавление записи) ---
        [ObservableProperty] private string newLicensePlate; // Для Entry "Гос. номер"
        [ObservableProperty] private string? selectedCarModel; // Для Picker "Выберите автомобиль"
        [ObservableProperty] private ObservableCollection<string> carModels; // Заглушка
        [ObservableProperty] private string? selectedFuelType; // Для Picker "Выберите тип топлива" (95, 98, ДТ)
        [ObservableProperty] private ObservableCollection<string> fuelTypes; // Заглушка
        [ObservableProperty] private decimal newFuelVolume; // Для Entry "Объем топлива"
        [ObservableProperty] private int newFuelMileage; // Для Entry "Пробег автомобиля"
        [ObservableProperty] private DateTime newFuelDate; // Для DatePicker "Дата заправки"

        // --- Поля для FuelPage2 (история и фильтрация) ---
        [ObservableProperty] private string? selectedUserForView; // Для Picker "Выберите пользователя"
        [ObservableProperty] private ObservableCollection<string> usersForView; // Заглушка
        [ObservableProperty] private DateTime fromDate; // Для DatePicker "С:"
        [ObservableProperty] private DateTime toDate; // Для DatePicker "По:"
        [ObservableProperty] private FuelRecord? _selectedFuelRecord; // Для выбранной записи в истории заправок

        // --- Опционально: Ограничение дат для Min/Max для DatePicker'ов ---
        [ObservableProperty]
        private DateTime minimumFromDate; // Минимальная дата для FromDatePicker
        [ObservableProperty]
        private DateTime maximumFromDate; // Максимальная дата для FromDatePicker

        [ObservableProperty]
        private DateTime minimumToDate; // Минимальная дата для ToDatePicker
        [ObservableProperty]
        private DateTime maximumToDate; // Максимальная дата для ToDatePicker


        // Коллекция для отображения записей (будет загружаться из API)
        [ObservableProperty]
        private ObservableCollection<FuelRecord> fuelRecords;


        private readonly ApiService _apiService;
        private int _currentUserId; // Поле для хранения UserId текущего пользователя

        public FuelViewModel(ApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            
            // Инициализация коллекций
            CarModels = new ObservableCollection<string> { "Volkswagen Golf", "Toyota Corolla", "Honda Civic", "BMW 3 Series", "Lada Niva" };
            FuelTypes = new ObservableCollection<string> { "92", "95", "98", "100", "ДТ" };
            UsersForView = new ObservableCollection<string> { "Иванов Иван", "Петров Петр", "Сидоров Сидор", "Смирнов Сергей", "Кузнецов Антон" };
            FuelRecords = new ObservableCollection<FuelRecord>();
            
            // Инициализация дат
            FromDate = DateTime.Today.AddDays(-30);
            ToDate = DateTime.Today;
            
            // Инициализация ограничений дат
            MinimumFromDate = new DateTime(2024, 1, 1);
            MaximumFromDate = new DateTime(2100, 12, 31);
            MinimumToDate = new DateTime(2024, 1, 1);
            MaximumToDate = new DateTime(2100, 12, 31);
            
            // Инициализация базовых данных без API вызовов
            NewFuelDate = DateTime.Today;
            NewLicensePlate = string.Empty; // Инициализация NewLicensePlate
            SelectedCarModel = CarModels.FirstOrDefault();
            SelectedFuelType = FuelTypes.FirstOrDefault();
            SelectedUserForView = UsersForView.FirstOrDefault();
            _selectedFuelRecord = null; // Инициализация nullable свойства
            
            // Инициализируем данные асинхронно
            // Task.Run(InitializeDataAsync);
        }

        // Метод для инициализации данных ViewModel
        public async Task InitializeDataAsync()
        {
            try
            {
                var userIdString = await SecureStorage.GetAsync("user_id");
                if (int.TryParse(userIdString, out int userId) && userId > 0)
                {
                    _currentUserId = userId;
                }
                else
                {
                    // Если user_id не найден после попытки входа, перенаправляем на LoginPage.
                    System.Diagnostics.Debug.WriteLine("WARNING: user_id not found in SecureStorage. Redirecting to LoginPage.");
                    await Application.Current.MainPage.DisplayAlert("Ошибка авторизации", "ID пользователя не найден. Пожалуйста, войдите снова.", "OK");
                    await Shell.Current.GoToAsync("//LoginPage");
                    return; // Важно выйти, чтобы избежать дальнейших операций
                }

                // Инициализация диапазонов Max/Min дат для Picker'ов
                MaximumFromDate = ToDate; // Максимальная для "С" - это текущая "По"
                MinimumToDate = FromDate; // Минимальная для "По" - это текущая "С"

                // Загрузка записей для FuelPage2 сразу после инициализации
                if (FuelRecords != null)
                {
                    await LoadFuelRecordsAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in InitializeDataAsync: {ex.Message}");
                // Не показываем ошибку пользователю, так как это может быть нормально при первом запуске
            }
        }

        // --- Обработчики изменения дат для From/To DatePicker'ов ---
        void OnFromDateChanged(DateTime oldValue, DateTime newValue)
        {
            MinimumToDate = newValue; // Обновляем минимальную дату для ToDatePicker
            LoadFuelRecordsCommand?.Execute(null); // Перезагружаем данные при изменении даты
        }

        void OnToDateChanged(DateTime oldValue, DateTime newValue)
        {
            MaximumFromDate = newValue; // Обновляем максимальную дату для FromDatePicker
            LoadFuelRecordsCommand?.Execute(null); // Перезагружаем данные при изменении даты
        }


        // --- Команды для FuelPage (добавление) ---

        [RelayCommand]
        public async Task AddFuelRecordAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SelectedCarModel) || string.IsNullOrWhiteSpace(SelectedFuelType) ||
                    NewFuelVolume <= 0 || NewFuelMileage <= 0 || NewFuelDate == default || string.IsNullOrWhiteSpace(NewLicensePlate))
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка", "Заполните все поля для добавления записи", "OK");
                    return;
                }

                if (!await IsUserLoggedInAndUserIdValid()) return;

                // API вызов для добавления новой записи заправки
                var requestData = new { Date = NewFuelDate, Volume = NewFuelVolume, Cost = NewFuelVolume * 50m, Mileage = NewFuelMileage, FuelType = SelectedFuelType, LicensePlate = NewLicensePlate }; // UserId будет взят из JWT на сервере
                var apiResponse = await _apiService.PostAsync<ApiResponse<FuelRecord>>("/api/v1/fuel", requestData);
                if (apiResponse.IsSuccess && apiResponse.Data != null)
                {
                    // FuelRecords.Add(apiResponse.Data); // Добавляем новую запись
                    await Application.Current.MainPage.DisplayAlert("Успех", apiResponse.Message, "OK");
                    ResetAddForm(); // Сбрасываем форму
                    await LoadFuelRecordsAsync(); // Перезагружаем записи для обновления списка
                }
                else { await Application.Current.MainPage.DisplayAlert("Ошибка", apiResponse.Message ?? "Не удалось добавить заправку.", "OK"); }
            }
            catch (UnauthorizedException ex) { await HandleAuthError(ex); }
            catch (HttpRequestException ex) { await HandleNetworkError(ex); }
            catch (Exception ex) { await HandleGenericError(ex); }
        }

        private void ResetAddForm()
        {
            NewFuelDate = DateTime.Today;
            SelectedCarModel = CarModels.FirstOrDefault();
            SelectedFuelType = FuelTypes.FirstOrDefault();
            NewFuelVolume = 0;
            NewFuelMileage = 0;
            NewLicensePlate = string.Empty; // Сбрасываем NewLicensePlate
        }

        // --- Команды для FuelPage2 (история и управление) ---

        // Команда для загрузки записей заправок (для FuelPage2)
        [RelayCommand]
        public async Task LoadFuelRecordsAsync()
        {
            try
            {
                if (!await IsUserLoggedInAndUserIdValid()) return;

                // FuelRecords не может быть null из-за инициализации в конструкторе
                FuelRecords.Clear(); // Очищаем текущие записи

                // API вызов для загрузки записей заправок
                var query = $"/api/v1/fuel?startDate={FromDate:yyyy-MM-dd}&endDate={ToDate:yyyy-MM-dd}";
                // Обратите внимание: SelectedUserForView - это заглушка для UI, сервер не использует userId из запроса.
                // if (!string.IsNullOrEmpty(SelectedUserForView)) query += $"&worker={SelectedUserForView}"; // Если бы была фильтрация по имени работника

                var apiResponse = await _apiService.GetAsync<ApiResponse<List<FuelRecord>>>(query);
                if (apiResponse.IsSuccess && apiResponse.Data != null && FuelRecords != null)
                {
                    foreach (var record in apiResponse.Data) 
                    { 
                        FuelRecords.Add(record); 
                    }
                }
                else { await Application.Current.MainPage.DisplayAlert("Ошибка", apiResponse.Message ?? "Не удалось загрузить заправки.", "OK"); }
            }
            catch (UnauthorizedException ex) { await HandleAuthError(ex); }
            catch (HttpRequestException ex) { await HandleNetworkError(ex); }
            catch (Exception ex) { await HandleGenericError(ex); }
        }

        // Команда для редактирования записи (вызывается из ItemTemplate в FuelPage2)
        [RelayCommand]
        public async Task EditFuelRecordAsync(FuelRecord? record)
        {
            _selectedFuelRecord = record; // Устанавливаем выбранную запись
            await Application.Current.MainPage.DisplayAlert("Редактирование", $"Выбрана запись ID: {record?.Id}. В реальном приложении здесь будет логика для открытия формы редактирования.", "OK");
            // TODO: Здесь должна быть логика для открытия формы редактирования, возможно, переход на новую страницу или отображение всплывающего окна.
        }

        // Команда для удаления записи (вызывается из ItemTemplate в FuelPage2)
        [RelayCommand]
        public async Task DeleteFuelRecordAsync(int id)
        {
            if (!await IsUserLoggedInAndUserIdValid()) return;

            var confirm = await Application.Current.MainPage.DisplayAlert("Подтверждение", $"Вы уверены, что хотите удалить заправку ID:{id}?", "Да", "Нет");
            if (!confirm) return;

            try
            {
                // API вызов для удаления
                var apiResponse = await _apiService.DeleteAsync($"/api/v1/fuel/{id}"); // userId берется из JWT
                if (apiResponse.IsSuccess)
                {
                    if (FuelRecords != null)
                    {
                        var recordToRemove = FuelRecords.FirstOrDefault(r => r.Id == id);
                        if (recordToRemove != null) { FuelRecords.Remove(recordToRemove); }
                    }
                    await Application.Current.MainPage.DisplayAlert("Успех", apiResponse.Message, "OK");
                }
                else { await Application.Current.MainPage.DisplayAlert("Ошибка", apiResponse.Message ?? "Не удалось удалить заправку.", "OK"); }
            }
            catch (UnauthorizedException ex) { await HandleAuthError(ex); }
            catch (HttpRequestException ex) { await HandleNetworkError(ex); }
            catch (Exception ex) { await HandleGenericError(ex); }
        }

        // Команда для обновления существующей записи (если форма редактирования будет реализована)
        [RelayCommand]
        public async Task UpdateExistingFuelRecordAsync()
        {
            if (!await IsUserLoggedInAndUserIdValid()) return;

            if (SelectedFuelRecord == null) { await Application.Current.MainPage.DisplayAlert("Ошибка", "Запись для обновления не выбрана.", "OK"); return; }

            try
            {
                // Собираем данные для обновления из SelectedFuelRecord
                var updateData = new
                {
                    Date = SelectedFuelRecord.Date,
                    Volume = SelectedFuelRecord.Volume,
                    Cost = SelectedFuelRecord.Cost,
                    Mileage = SelectedFuelRecord.Mileage,
                    FuelType = SelectedFuelRecord.FuelType
                };

                // API вызов для обновления
                var apiResponse = await _apiService.PutAsync<ApiResponse<FuelRecord>>($"/api/v1/fuel/{SelectedFuelRecord.Id}", updateData); // userId берется из JWT
                if (apiResponse.IsSuccess && apiResponse.Data != null)
                {
                    // Обновляем запись в коллекции ObservableCollection
                    // FuelRecords не может быть null из-за инициализации в конструкторе
                    var index = FuelRecords.IndexOf(FuelRecords.FirstOrDefault(f => f.Id == SelectedFuelRecord.Id));
                    if (index != -1)
                    {
                        FuelRecords[index] = apiResponse.Data;
                    }
                    await Application.Current.MainPage.DisplayAlert("Успех", apiResponse.Message, "OK");
                    await LoadFuelRecordsAsync(); // Перезагружаем для актуализации
                }
                else { await Application.Current.MainPage.DisplayAlert("Ошибка", apiResponse.Message ?? "Не удалось обновить заправку.", "OK"); }
            }
            catch (UnauthorizedException ex) { await HandleAuthError(ex); }
            catch (HttpRequestException ex) { await HandleNetworkError(ex); }
            catch (Exception ex) { await HandleGenericError(ex); }
        }

        // --- Вспомогательные методы (для общей логики) ---

        // Упрощенный метод для проверки авторизации и UserId
        private async Task<bool> IsUserLoggedInAndUserIdValid()
        {
            System.Diagnostics.Debug.WriteLine($"[FuelViewModel] IsUserLoggedInAndUserIdValid: Entering. _currentUserId = {_currentUserId}");
            // Если _currentUserId 0, попробуем загрузить его из SecureStorage
            if (_currentUserId <= 0)
            {
                System.Diagnostics.Debug.WriteLine("[FuelViewModel] IsUserLoggedInAndUserIdValid: _currentUserId is 0. Attempting to load from SecureStorage...");
                var userIdString = await SecureStorage.GetAsync(SecureStorageKeys.UserId);
                System.Diagnostics.Debug.WriteLine($"[FuelViewModel] IsUserLoggedInAndUserIdValid: SecureStorage returned '{userIdString ?? "null"}'. TryParse result: {int.TryParse(userIdString, out int parsedUserId) && parsedUserId > 0}");
                if (int.TryParse(userIdString, out int userId) && userId > 0)
                {
                    _currentUserId = userId;
                    System.Diagnostics.Debug.WriteLine($"[FuelViewModel] IsUserLoggedInAndUserIdValid: Successfully loaded UserId {userId} from SecureStorage. _currentUserId now: {_currentUserId}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[FuelViewModel] IsUserLoggedInAndUserIdValid: Failed to get valid UserId from SecureStorage. Navigating to LoginPage.");
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await Application.Current.MainPage.DisplayAlert("Ошибка авторизации", "ID пользователя не найден. Пожалуйста, войдите снова.", "OK");
                        await Shell.Current.GoToAsync("//LoginPage");
                    });
                    return false;
                }
            }
            System.Diagnostics.Debug.WriteLine($"[FuelViewModel] IsUserLoggedInAndUserIdValid: Exiting. Final _currentUserId = {_currentUserId}");
            return true; // Теперь достаточно проверки _currentUserId, так как API сервис сам обрабатывает 401 и перенаправляет.
        }

        // Унифицированные обработчики ошибок (можно вынести в базовый класс ViewModel)
        private async Task HandleAuthError(UnauthorizedException ex)
        {
            await Application.Current.MainPage.DisplayAlert("Ошибка авторизации", ex.Message, "OK");
            // ApiService уже перенаправляет на LoginPage, здесь дублировать не нужно.
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
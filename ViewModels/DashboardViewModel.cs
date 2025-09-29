using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Threading.Tasks;
using VEPS_Plus.Services;

namespace VEPS_Plus.ViewModels
{
    public class Dashboard
    {
        public int TotalHours { get; set; }
        public decimal TotalFuelCost { get; set; }
        public int TotalMileage { get; set; }
        public int UnreadNotifications { get; set; }
        public int WorkingHoursInMonth { get; set; }
    }

    public partial class DashboardViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _totalHours; 
        [ObservableProperty]
        private decimal _totalFuelCost; 
        [ObservableProperty]
        private int _totalMileage; 
        [ObservableProperty]
        private int _unreadNotifications;

        private readonly ApiService _apiService;

        public DashboardViewModel(ApiService apiService)
        {
            _apiService = apiService;
        }

        [RelayCommand]
        private async Task LoadDashboardAsync()
        {
            try
            {
                var token = await SecureStorage.GetAsync("jwt_token");
                if (string.IsNullOrEmpty(token))
                {
                    await Shell.Current.GoToAsync("//LoginPage");
                    return;
                }

                var apiResponse = await _apiService.GetAsync<ApiResponse<Dashboard>>("/api/v1/dashboard");
                if (apiResponse?.IsSuccess == true && apiResponse.Data != null)
                {
                    TotalHours = apiResponse.Data.TotalHours;
                    TotalFuelCost = apiResponse.Data.TotalFuelCost;
                    TotalMileage = apiResponse.Data.TotalMileage;
                    UnreadNotifications = apiResponse.Data.UnreadNotifications;
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка", apiResponse?.Message ?? "Не удалось загрузить дашборд", "OK");
                    return;
                }

                await Application.Current.MainPage.DisplayAlert("Успех", "Дашборд загружен", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", $"Не удалось загрузить дашборд: {ex.Message}", "OK");
            }
        }
    }
}

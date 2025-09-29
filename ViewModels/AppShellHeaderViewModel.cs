using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Storage;
using System.Threading.Tasks;
using VEPS_Plus.Services;

namespace VEPS_Plus.ViewModels
{
    public partial class AppShellHeaderViewModel : ObservableObject
    {
        [ObservableProperty]
        private CurrentUser _shellCurrentUser;

        private readonly ApiService _apiService;

        public AppShellHeaderViewModel(ApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            // Инициализация CurrentUser, чтобы избежать NullReferenceException до загрузки данных
            _shellCurrentUser = new CurrentUser { Username = "Загрузка...", Initials = "..", UnreadCount = 0 };
        }

        public async void LoadUserData()
        {
            try
            {
                var username = await SecureStorage.GetAsync("username");
                var role = await SecureStorage.GetAsync("user_role") ?? string.Empty;

                var initials = ComputeInitials(username);

                int unread = 0;
                try
                {
                    // Пытаемся получить счетчик непрочитанных
                    var apiResponse = await _apiService.GetAsync<ApiResponse<Dashboard>>("/api/v1/dashboard");
                    if (apiResponse?.IsSuccess == true && apiResponse.Data != null)
                    {
                        unread = apiResponse.Data.UnreadNotifications;
                    }
                }
                catch { /* игнорируем, оставляем 0 */ }

                ShellCurrentUser = new CurrentUser { Username = username ?? "Гость", Initials = initials, UnreadCount = unread, Role = role };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LoadUserData: {ex.Message}");
                // Устанавливаем значения по умолчанию при ошибке
                ShellCurrentUser = new CurrentUser { Username = "Гость", Initials = "..", UnreadCount = 0, Role = "user" };
            }
        }

        private string ComputeInitials(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username)) return "..";
                var cleaned = username.Replace("@veps-spb.ru", string.Empty);
                var parts = cleaned.Split(new[] { '.', ' ', '_' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    return ($"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}");
                }
                return char.ToUpper(cleaned[0]).ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ComputeInitials: {ex.Message}");
                return "..";
            }
        }
    }

    // Перемещено сюда или оставлено в AppShell.xaml.cs, в зависимости от того, где оно используется.
    // Если CurrentUser используется только здесь, то оно должно быть здесь.
    // Если оно используется также в AppShell.xaml.cs (вне ViewModel), тогда его придется оставить в AppShell.xaml.cs.
    // В данном случае, так как оно используется в ObservableProperty, оно должно быть здесь.
    public partial class CurrentUser : ObservableObject
    {
        [ObservableProperty]
        private string username;

        [ObservableProperty]
        private string initials;

        [ObservableProperty]
        private int unreadCount;

        [ObservableProperty]
        private string role;
    }
}

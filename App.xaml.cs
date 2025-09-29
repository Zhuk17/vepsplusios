using VEPS_Plus.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Storage;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using VEPS_Plus.Services;
using VEPS_Plus.ViewModels;
using VEPS_Plus.Constants;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System;
using System.Linq;
using Microsoft.Maui.Controls;

namespace VEPS_Plus
{
    public partial class App : Application
    {
        public static IServiceProvider? Services { get; private set; }
        private readonly ILogger<App> _logger;
        private readonly IVpnService _vpnService;

        public App(IServiceProvider serviceProvider, ILogger<App> logger, IVpnService vpnService)
        {
            InitializeComponent();
            Services = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger;
            _vpnService = vpnService;

            if (Application.Current != null)
            {
                Application.Current.UserAppTheme = AppTheme.Dark;
                Application.Current.RequestedThemeChanged += (s, e) => { Application.Current.UserAppTheme = AppTheme.Dark; };
            }
            
            var appShell = Services.GetService<AppShell>();
            if (appShell != null)
            {
                MainPage = appShell;
            }
            else
            {
                _logger.LogError("AppShell не может быть разрешен через ServiceProvider. Убедитесь, что он зарегистрирован в MauiProgram.cs.");
                throw new InvalidOperationException("AppShell не может быть разрешен через ServiceProvider. Убедитесь, что он зарегистрирован в MauiProgram.cs.");
            }

            var apiService = Services.GetService<ApiService>();

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (apiService != null)
                {
                    try
                    {
                        await apiService.InitializeAuthAsync();

                        // Добавляем вызов для получения WireGuard конфига после инициализации аутентификации
                        // var wireGuardConfig = await apiService.GetWireGuardConfigAsync();
                        // if (!string.IsNullOrEmpty(wireGuardConfig))
                        // {
                        //     await SecureStorage.SetAsync(SecureStorageKeys.WireGuardConfig, wireGuardConfig);
                        //     _logger.LogInformation("[App] WireGuard config successfully fetched and stored in SecureStorage.");
                        // }
                        // else
                        // {
                        //     _logger.LogWarning("[App] Failed to fetch WireGuard config or config was empty.");
                        // }
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogError(ex, "[App] Network error during initial API calls.");
                        if (Application.Current?.MainPage != null) await Application.Current.MainPage.DisplayAlert("Ошибка сети", "Не удалось подключиться к серверу при запуске. Пожалуйста, проверьте ваше интернет-соединение.", "OK");
                        if (Shell.Current != null) await Shell.Current.GoToAsync("///LoginPage", false);
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[App] Unexpected error during initial API calls.");
                        if (Application.Current?.MainPage != null) await Application.Current.MainPage.DisplayAlert("Ошибка", "Произошла непредвиденная ошибка при запуске приложения.", "OK");
                        if (Shell.Current != null) await Shell.Current.GoToAsync("///LoginPage", false);
                        return;
                    }
                }

                // 1. Проверяем, активен ли VPN. Если нет, пытаемся его запустить через внешнее приложение WireGuard.
                if (!_vpnService.IsVpnActive())
                {
                    _logger.LogInformation("[App] VPN not active. Attempting to start WireGuard tunnel 'veps'...");
                    bool vpnConnected = await _vpnService.Connect("veps");
                    if (!vpnConnected)
                    {
                        await Application.Current.MainPage.DisplayAlert("Ошибка WireGuard", "Не удалось запустить WireGuard VPN. Пожалуйста, убедитесь, что приложение WireGuard установлено и туннель с именем \"veps\" сконфигурирован.", "OK");
                        // Если VPN не запустился, перенаправляем на страницу входа и прерываем дальнейшие действия
                        await Shell.Current.GoToAsync("///LoginPage", false);
                        return;
                    }
                    _logger.LogInformation("[App] Request to connect VPN sent. Checking status...");
                    await Task.Delay(5000); // Увеличиваем задержку, чтобы внешнее приложение WireGuard успело отреагировать

                    if (!_vpnService.IsVpnActive())
                    {
                         await Application.Current.MainPage.DisplayAlert("Ошибка WireGuard", "VPN не активен после попытки запуска. Пожалуйста, проверьте состояние WireGuard приложения вручную.", "OK");
                         await Shell.Current.GoToAsync("///LoginPage", false);
                         return;
                    }
                    _logger.LogInformation("[App] VPN is active. Continuing with app initialization.");
                }

                var isLoggedInString = await SecureStorage.GetAsync(SecureStorageKeys.IsUserLoggedIn);
                var isLoggedIn = isLoggedInString == "true";

                var currentRoute = Shell.Current?.CurrentState.Location.OriginalString;
                _logger.LogDebug("[App] Initializing navigation. IsLoggedIn: {IsLoggedIn}, CurrentRoute: {CurrentRoute}", isLoggedIn, currentRoute);
                if (isLoggedIn)
                {
                    // Добавляем небольшую задержку для полной инициализации Shell
                    await Task.Delay(100);
                    
                    // Получаем AppShell после того, как MainPage установлен как CurrentItem
                    AppShell currentAppShell = (AppShell)Shell.Current; // Renamed to avoid conflict
                    // Устанавливаем CurrentItem на MainPage, чтобы принудительно активировать вкладку и отобразить TabBar
                    // Находим ShellContent для MainPage по его маршруту
                    var mainShellContent = currentAppShell.Items
                        .SelectMany(item => item.Items)
                        .SelectMany(shellSection => shellSection.Items)
                        .OfType<ShellContent>()
                        .FirstOrDefault(content => content.Route == "MainPage");

                    if (mainShellContent != null)
                    {
                        currentAppShell.CurrentItem = mainShellContent; // Устанавливаем найденный ShellContent как текущий элемент
                        _logger.LogDebug("[App] Set AppShell.CurrentItem to MainPage via route. CurrentItem is now: {CurrentItemRoute}", mainShellContent.Route);
                    }
                    else
                    {
                        _logger.LogError("[App] Could not find ShellContent for MainPage by route. TabBar may not display correctly.");
                    }
                    
                    // Можно добавить GoToAsync, если нужно, чтобы страница внутри TabBar была на вершине стека
                    // await Shell.Current.GoToAsync("MainPage", true);
                    if (Shell.Current is AppShell appShell)
                    {
                        await appShell.LoadHeaderData();
                        _logger.LogDebug("[App] Navigated to TimesheetPage. Header data loaded.");

                        var timesheetViewModel = Services.GetService<TimesheetViewModel>();
                        if (timesheetViewModel != null)
                        {
                            await timesheetViewModel.LoadInitialDataAsync();
                            _logger.LogDebug("[App] TimesheetViewModel.LoadInitialDataAsync called after login.");
                        }
                    }
                }
                else
                {
                    if (currentRoute != "///LoginPage")
                    {
                        if (Shell.Current != null) await Shell.Current.GoToAsync("//LoginPage", false);
                        _logger.LogDebug("[App] Navigated to LoginPage.");
                    }
                    else
                    {
                        _logger.LogDebug("[App] Already on LoginPage. No navigation needed.");
                    }
                }
            });
        }
    }
}
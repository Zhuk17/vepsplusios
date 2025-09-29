using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using VEPS_Plus.Pages;
using VEPS_Plus.Services;
using VEPS_Plus.ViewModels;
using CommunityToolkit.Maui;
using VEPS_Plus.Converters; // Убедитесь, что этот using есть

namespace VEPS_Plus
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .ConfigureMauiHandlers(handlers =>
                {
#if ANDROID
                    handlers.AddHandler(typeof(Shell), typeof(Platforms.Android.MyShellRenderer));
#endif
                });

            // --- Регистрация сервисов ---
            builder.Services.AddSingleton<ApiService>();
            builder.Services.AddSingleton<IUserService, UserService>();
            builder.Services.AddSingleton<App>();
            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddSingleton<IToastService, ToastService>();
            builder.Services.AddSingleton<IVpnService, DummyVpnService>(); // Register DummyVpnService as the implementation for IVpnService
            
            // Регистрация сервисов уведомлений
            builder.Services.AddSingleton<IPushNotificationService, Platforms.iOS.iOSPushNotificationService>();
            builder.Services.AddSingleton<ISignalRService, SignalRService>();

            builder.Services.AddLogging(configure =>
            {
#if DEBUG
                configure.AddDebug();
#endif
                configure.SetMinimumLevel(LogLevel.Debug);
            });

            // --- Регистрация ViewModels (Transient) ---
            builder.Services.AddTransient<LoginViewModel>(); // Changed to Transient as per Android project
            builder.Services.AddTransient<ProfileViewModel>();
            builder.Services.AddTransient<NotificationsViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<DashboardViewModel>();
            builder.Services.AddTransient<FuelViewModel>();
            builder.Services.AddSingleton<TimesheetViewModel>();
            builder.Services.AddTransient<AppShellHeaderViewModel>();

            // --- Регистрация Pages (Transient) ---
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<ProfilePage>();
            builder.Services.AddTransient<NotificationsPage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<FuelPage>();
            builder.Services.AddTransient<TimesheetPage>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<FuelPage2>();

            // --- Регистрация конвертеров ---
            builder.Services.AddSingleton<IsReadToBackgroundColorConverter>();
            builder.Services.AddSingleton<InvertedBoolConverter>();
            builder.Services.AddSingleton<VEPS_Plus.Converters.IsNotNullConverter>();
            builder.Services.AddSingleton<GreaterThanZeroToBoolConverter>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            Microsoft.Maui.Handlers.ToolbarHandler.Mapper.AppendToMapping("CustomNavigationView", (handler, view) =>
            {
#if ANDROID
                handler.PlatformView.ContentInsetStartWithNavigation = 0;
                handler.PlatformView.SetContentInsetsAbsolute(0, 0);
#endif
            });

            return builder.Build();
        }
    }
}

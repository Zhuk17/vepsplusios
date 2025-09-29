using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using VEPS_Plus.Pages;
using Microsoft.Maui.ApplicationModel;
using CommunityToolkit.Mvvm.ComponentModel;
using VEPS_Plus.ViewModels;
using Microsoft.Extensions.Logging;
using VEPS_Plus.Constants;

namespace VEPS_Plus
{
    public partial class AppShell : Shell
    {
        private AppShellHeaderViewModel _shellHeaderViewModel;
        private readonly ILogger<AppShell> _logger;

        public AppShell(AppShellHeaderViewModel shellHeaderViewModel, ILogger<AppShell> logger)
        {
            InitializeComponent();

            _shellHeaderViewModel = shellHeaderViewModel ?? throw new ArgumentNullException(nameof(shellHeaderViewModel));
            this.BindingContext = _shellHeaderViewModel;
            _logger = logger;

            Routing.RegisterRoute("LoginPage", typeof(LoginPage));
            Routing.RegisterRoute("MainPage", typeof(MainPage));
            Routing.RegisterRoute("TimesheetPage", typeof(TimesheetPage));
            Routing.RegisterRoute("FuelPage", typeof(FuelPage));
            Routing.RegisterRoute("FuelPage2", typeof(FuelPage2));
            Routing.RegisterRoute("SettingsPage", typeof(SettingsPage));
            Routing.RegisterRoute("ProfilePage", typeof(ProfilePage));
            Routing.RegisterRoute("NotificationsPage", typeof(NotificationsPage));

            try
            {
                // _shellHeaderViewModel.LoadUserData(); // Удалено, так как может быть вызвано слишком рано
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user data in AppShell constructor.");
            }
        }

        private async void OnProfileTapped(object sender, EventArgs e)
        {
            try
            {
                await GoToAsync("///ProfilePage");
                _logger.LogDebug("Navigated to ProfilePage.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to ProfilePage.");
            }
        }

        private async void OnBellTapped(object sender, EventArgs e)
        {
            try
            {
                await GoToAsync("///NotificationsPage");
                _logger.LogDebug("Navigated to NotificationsPage.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to NotificationsPage.");
            }
        }

        public static void UpdateShellHeader()
        {
            try
            {
                if (Shell.Current is AppShell appShell && appShell._shellHeaderViewModel != null)
                {
                    appShell._shellHeaderViewModel.LoadUserData();
                    appShell._logger.LogDebug("AppShell header data updated.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateShellHeader: {ex.Message}");
            }
        }

        public async Task LoadHeaderData()
        {
            try
            {
                if (_shellHeaderViewModel != null)
                {
                    _shellHeaderViewModel.LoadUserData();
                    _logger.LogDebug("AppShell LoadHeaderData called.");
                    await ApplyRoleVisibility(); // Вызываем ApplyRoleVisibility после загрузки данных пользователя
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LoadHeaderData.");
            }
        }

        private async Task ApplyRoleVisibility()
        {
            try
            {
                var role = await SecureStorage.GetAsync(SecureStorageKeys.UserRole) ?? string.Empty;
                if (this.Items != null)
                {
                    foreach (var shellItem in this.Items)
                    {
                        if (shellItem.Items != null)
                        {
                            foreach (var section in shellItem.Items)
                            {
                                if (section.Items != null)
                                {
                                    foreach (var content in section.Items)
                                    {
                                        if (content.Route == "FuelPage" || content.Route == "FuelPage2")
                                        {
                                            content.IsVisible = true; // Временно делаем FuelPage видимой
                                        }
                                        else if (content.Route == "TimesheetPage")
                                        {
                                            content.IsVisible = (string.IsNullOrEmpty(role)) || VEPS_Plus.Constants.UserRoles.HasPermission(role, VEPS_Plus.Constants.UserRoles.User);
                                        }
                                        else if (content.Route == "MainPage")
                                        {
                                            content.IsVisible = (string.IsNullOrEmpty(role)) || VEPS_Plus.Constants.UserRoles.HasPermission(role, VEPS_Plus.Constants.UserRoles.User);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ApplyRoleVisibility.");
            }
        }
    }
}

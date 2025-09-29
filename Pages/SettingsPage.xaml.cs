using VEPS_Plus.ViewModels; 
using Microsoft.Maui.Controls; 

namespace VEPS_Plus.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel; 

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel; 
        BindingContext = _viewModel; 
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCurrentUserIdAndSettingsAsync();
    }

    private void OnDarkThemeChanged(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value)
        {
            Application.Current.UserAppTheme = AppTheme.Dark;
        }
        else
        {
            Application.Current.UserAppTheme = AppTheme.Light;
        }
    }

    private async void OnTestPushClicked(object sender, EventArgs e)
    {
        await Application.Current.MainPage.DisplayAlert("Тест", "Push-уведомление получено!", "OK");
    }
}
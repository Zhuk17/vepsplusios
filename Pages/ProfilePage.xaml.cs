using VEPS_Plus.ViewModels;
using Microsoft.Maui.Controls;

namespace VEPS_Plus.Pages;

public partial class ProfilePage : ContentPage
{
    private readonly ProfileViewModel _viewModel;

    public ProfilePage(ProfileViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    private async void OnArrowTapped(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[ProfilePage] OnArrowTapped: Attempting to navigate back. Current Shell.Current.CurrentState.Location: {Shell.Current.CurrentState.Location}");
        try
        {
            await Shell.Current.GoToAsync("//MainPage");
            System.Diagnostics.Debug.WriteLine("[ProfilePage] OnArrowTapped: Successfully navigated back.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProfilePage] OnArrowTapped: Error navigating back: {ex.Message}. StackTrace: {ex.StackTrace}");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadProfileAsync();
    }
}




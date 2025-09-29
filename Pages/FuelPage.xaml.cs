using VEPS_Plus.ViewModels;

namespace VEPS_Plus.Pages;

public partial class FuelPage : ContentPage
{
    private readonly FuelViewModel _viewModel;

    public FuelPage(FuelViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeDataAsync();
    }
}
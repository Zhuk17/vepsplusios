using VEPS_Plus.ViewModels; 
using Microsoft.Maui.Controls; 
using System; 

namespace VEPS_Plus.Pages; 

public partial class TimesheetPage : ContentPage 
{
    private readonly TimesheetViewModel _viewModel;

    public TimesheetPage(TimesheetViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadInitialDataAsync();
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        _viewModel.IsNotificationsBlockVisible = !_viewModel.IsNotificationsBlockVisible;
        _viewModel.NotificationsBlockHeight = _viewModel.IsNotificationsBlockVisible ? 200 : 50;
        _viewModel.NotificationsRollButtonRotation = _viewModel.IsNotificationsBlockVisible ? 180 : 0;
    }

    private void btn_history_clicked(object sender, EventArgs e)
    {
        _viewModel.IsHistoryBlockVisible = !_viewModel.IsHistoryBlockVisible;
        _viewModel.HistoryBlockHeight = _viewModel.IsHistoryBlockVisible ? 570 : 50;
        _viewModel.HistoryRollButtonRotation = _viewModel.IsHistoryBlockVisible ? 180 : 0;
    }
}
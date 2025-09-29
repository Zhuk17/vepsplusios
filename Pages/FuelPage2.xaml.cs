using VEPS_Plus.ViewModels;
using Microsoft.Maui.Controls;
using System;

namespace VEPS_Plus.Pages;

public partial class FuelPage2 : ContentPage
{
    private readonly FuelViewModel _viewModel;

    public FuelPage2(FuelViewModel viewModel)
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
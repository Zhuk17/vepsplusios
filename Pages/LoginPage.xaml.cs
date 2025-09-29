using VEPS_Plus.ViewModels;
using Microsoft.Maui.Storage; // Added for SecureStorage
using System.Threading.Tasks; // Added for Task.Delay
using System.Diagnostics; // Added for Debug.WriteLine

namespace VEPS_Plus.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
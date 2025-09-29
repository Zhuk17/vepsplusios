using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace VEPS_Plus.Services
{
    public class ToastService : IToastService
    {
        public async void ShowToast(string message, bool isError = false)
        {
            var snackbarOptions = new SnackbarOptions
            {
                CornerRadius = new CornerRadius(10),
                BackgroundColor = isError ? Colors.Red : Colors.Green,
                TextColor = Colors.White,
                ActionButtonTextColor = Colors.Yellow,
            };

            var snackbar = Snackbar.Make(message, null, "OK", TimeSpan.FromSeconds(3), snackbarOptions);
            await snackbar.Show();
        }
    }
}

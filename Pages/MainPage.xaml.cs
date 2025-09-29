namespace VEPS_Plus.Pages;

public partial class MainPage : ContentPage
{
    // Удаляем все поля и методы, связанные с UI-логикой счетчика
    // int count = 0;
    // private void OnCounterClicked(object sender, EventArgs e) { ... }

    // ИСПРАВЛЕНИЕ: Конструктор теперь базовый, без ViewModel и BindingContext
    public MainPage()
    {
        InitializeComponent();
        // BindingContext = this; // Эту строку УДАЛЯЕМ, так как нет ViewModel
    }

    private async void OnGoTimesheet(object sender, EventArgs e)
    {
        try { await Shell.Current.GoToAsync("///TimesheetPage"); } catch { }
    }

    private async void OnGoFuel(object sender, EventArgs e)
    {
        try { await Shell.Current.GoToAsync("///FuelPage"); } catch { }
    }

    private async void OnGoProfile(object sender, EventArgs e)
    {
        try { await Shell.Current.GoToAsync("///ProfilePage"); } catch { }
    }
}



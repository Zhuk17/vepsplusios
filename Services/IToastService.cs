namespace VEPS_Plus.Services
{
    public interface IToastService
    {
        void ShowToast(string message, bool isError = false);
    }
}

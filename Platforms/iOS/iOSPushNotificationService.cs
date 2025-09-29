using Foundation;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using UserNotifications;
using VEPS_Plus.Services;

namespace VEPS_Plus.Platforms.iOS
{
    public class iOSPushNotificationService : PushNotificationService
    {
        private readonly ILogger<iOSPushNotificationService> _logger;

        public iOSPushNotificationService(ILogger<iOSPushNotificationService> logger, ApiService apiService) 
            : base(logger, apiService)
        {
            _logger = logger;
        }

        public override async Task<string?> GetTokenAsync()
        {
            try
            {
                _logger.LogInformation("[iOSPushNotificationService] Getting APNS token...");
                
                // Для iOS нужно получить токен через UNUserNotificationCenter
                var center = UNUserNotificationCenter.Current;
                var (granted, error) = await center.RequestAuthorizationAsync(
                    UNAuthorizationOptions.Alert | 
                    UNAuthorizationOptions.Badge | 
                    UNAuthorizationOptions.Sound);

                if (!granted)
                {
                    _logger.LogWarning("[iOSPushNotificationService] Push notification permission not granted");
                    return null;
                }

                // В реальном приложении здесь нужно получить токен через Firebase iOS SDK
                // Для упрощения возвращаем null - токен будет получен в AppDelegate
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[iOSPushNotificationService] Failed to get APNS token");
                return null;
            }
        }

        public static void ShowNotification(string title, string body)
        {
            try
            {
                var content = new UNMutableNotificationContent()
                {
                    Title = title,
                    Body = body,
                    Sound = UNNotificationSound.Default,
                    Badge = 1
                };

                var request = UNNotificationRequest.FromIdentifier(
                    Guid.NewGuid().ToString(), 
                    content, 
                    UNTimeIntervalNotificationTrigger.CreateTrigger(0.1, false));

                UNUserNotificationCenter.Current.AddNotificationRequest(request, (error) =>
                {
                    if (error != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[iOSPushNotificationService] Error showing notification: {error.LocalizedDescription}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[iOSPushNotificationService] Error showing notification: {ex.Message}");
            }
        }
    }
}

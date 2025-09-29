using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using VEPS_Plus.ViewModels;
using VEPS_Plus.Constants;
using Microsoft.Maui.Storage;

namespace VEPS_Plus.Services
{
    public class SignalRService : ISignalRService, IDisposable
    {
        private readonly ILogger<SignalRService> _logger;
        private HubConnection? _hubConnection;
        private string? _authToken;

        public event EventHandler<Notification>? NotificationReceived;

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

        public SignalRService(ILogger<SignalRService> logger)
        {
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            try
            {
                // Получаем токен из SecureStorage
                _authToken = await SecureStorage.GetAsync(SecureStorageKeys.JwtToken);
                
                if (string.IsNullOrEmpty(_authToken))
                {
                    _logger.LogWarning("[SignalRService] No auth token found, cannot initialize SignalR");
                    return;
                }

                // Создаем соединение с SignalR Hub
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl("https://10.0.2.2:7001/notificationHub", options => // Для Android эмулятора
                    {
                        options.AccessTokenProvider = () => Task.FromResult(_authToken);
                    })
                    .WithAutomaticReconnect()
                    .Build();

                // Подписываемся на получение уведомлений
                _hubConnection.On<object>("ReceiveNotification", (notificationData) =>
                {
                    try
                    {
                        // Парсим данные уведомления
                        var json = System.Text.Json.JsonSerializer.Serialize(notificationData);
                        var notification = System.Text.Json.JsonSerializer.Deserialize<Notification>(json, new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (notification != null)
                        {
                            _logger.LogInformation("[SignalRService] Received notification: {Title}", notification.Title);
                            NotificationReceived?.Invoke(this, notification);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[SignalRService] Error parsing received notification");
                    }
                });

                // Обработчики событий соединения
                _hubConnection.Reconnecting += (error) =>
                {
                    _logger.LogWarning("[SignalRService] SignalR connection lost, attempting to reconnect...");
                    return Task.CompletedTask;
                };

                _hubConnection.Reconnected += (connectionId) =>
                {
                    _logger.LogInformation("[SignalRService] SignalR reconnected with connection ID: {ConnectionId}", connectionId);
                    return Task.CompletedTask;
                };

                _hubConnection.Closed += (error) =>
                {
                    _logger.LogWarning(error, "[SignalRService] SignalR connection closed");
                    return Task.CompletedTask;
                };

                _logger.LogInformation("[SignalRService] SignalR initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SignalRService] Error initializing SignalR");
            }
        }

        public async Task ConnectAsync()
        {
            try
            {
                if (_hubConnection == null)
                {
                    await InitializeAsync();
                }

                if (_hubConnection != null && _hubConnection.State == HubConnectionState.Disconnected)
                {
                    await _hubConnection.StartAsync();
                    _logger.LogInformation("[SignalRService] SignalR connected successfully");
                    
                    // Присоединяемся к группе уведомлений
                    await _hubConnection.InvokeAsync("JoinNotificationGroup");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SignalRService] Error connecting to SignalR");
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
                {
                    await _hubConnection.InvokeAsync("LeaveNotificationGroup");
                    await _hubConnection.StopAsync();
                    _logger.LogInformation("[SignalRService] SignalR disconnected successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SignalRService] Error disconnecting from SignalR");
            }
        }

        public async Task SetAuthTokenAsync(string token)
        {
            _authToken = token;
            
            // Переподключаемся с новым токеном
            if (_hubConnection != null)
            {
                await DisconnectAsync();
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }
            
            await InitializeAsync();
            await ConnectAsync();
        }

        public void Dispose()
        {
            try
            {
                _hubConnection?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SignalRService] Error disposing SignalR connection");
            }
        }
    }
}

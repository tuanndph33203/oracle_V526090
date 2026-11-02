using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AnFood.Application.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MQTTnet;

namespace AnFood.Application.Services.Mqtt
{
    public class MqttSpeakerService : IMqttSpeakerService, IDisposable
    {
        private readonly MqttSettings _settings;
        private readonly ILogger<MqttSpeakerService> _logger;
        private readonly IMessageGatewayService _messageGateway;
        private readonly IMqttClient _mqttClient;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private bool _isDisposed;

        public MqttSpeakerService(IConfiguration configuration, ILogger<MqttSpeakerService> logger, IMessageGatewayService messageGateway)
        {
            _logger = logger;
            _messageGateway = messageGateway;
            _settings = configuration.GetSection("MqttSettings").Get<MqttSettings>() ?? new MqttSettings();
            var factory = new MqttClientFactory();
            _mqttClient = factory.CreateMqttClient();
        }

        private async Task EnsureConnectedAsync()
        {
            if (!_settings.EnableSpeaker || string.IsNullOrWhiteSpace(_settings.Server))
                return;

            if (_mqttClient.IsConnected)
                return;

            await _lock.WaitAsync();
            try
            {
                if (_mqttClient.IsConnected)
                    return;

                var optionsBuilder = new MqttClientOptionsBuilder()
                    .WithTcpServer(_settings.Server, _settings.Port)
                    .WithCleanSession()
                    .WithTimeout(TimeSpan.FromSeconds(5));

                if (_settings.UseTls == true || _settings.Port == 8883)
                {
                    optionsBuilder.WithTlsOptions(o => o.UseTls());
                }

                var clientId = string.IsNullOrWhiteSpace(_settings.ClientId)
                    ? "ANPOS_" + Guid.NewGuid().ToString("N")[..8]
                    : _settings.ClientId;

                optionsBuilder.WithClientId(clientId);

                if (!string.IsNullOrWhiteSpace(_settings.User))
                {
                    optionsBuilder.WithCredentials(_settings.User, _settings.Password);
                }

                var options = optionsBuilder.Build();
                await _mqttClient.ConnectAsync(options, CancellationToken.None);
                _logger.LogInformation("MQTT Speaker Connected to {Server}:{Port}", _settings.Server, _settings.Port);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MQTT Speaker connection failed");
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task PublishSpeakerAsync(object payload, string? customTopic = null)
        {
            var topic = string.IsNullOrWhiteSpace(customTopic) ? _settings.TopicSpeaker : customTopic;
            if (!_settings.EnableSpeaker)
            {
                if (topic == _settings.TopicOrder)
                    _logger.LogWarning("MQTT Speaker/Order skipped: speaker is disabled");
                return;
            }

            if (topic == _settings.TopicOrder)
                _logger.LogInformation("MQTT Speaker/Order sending table {Table}", payload);

            try
            {
                await EnsureConnectedAsync();
                if (!_mqttClient.IsConnected)
                {
                    _logger.LogWarning("MQTT Speaker publish skipped: not connected. Topic {Topic}", topic);
                    return;
                }

                var rawPayload = payload is string str ? str : JsonSerializer.Serialize(payload);

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(rawPayload)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                await _mqttClient.PublishAsync(message, CancellationToken.None);
                _logger.LogInformation("MQTT Speaker published to {Topic}: {Payload}", topic, rawPayload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish MQTT speaker message to {Topic}", topic);
            }
        }

        public async Task NotifyKitchenOrderAsync(string table, string area, int itemCount, IReadOnlyCollection<int> areaTypes, string orderCode, string? note = null)
        {
            if (areaTypes.Count == 0) return;
            var notification = $"{DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)):HH:mm:ss} {orderCode}: Có phiếu báo món gửi bếp";
            await Task.WhenAll(NotifyKitchenOrderSpeakerAsync(areaTypes), _messageGateway.SendAsync(notification));
        }

        public Task NotifyKitchenOrderSpeakerAsync(IReadOnlyCollection<int> areaTypes)
        {
            return areaTypes.Contains(1) || areaTypes.Contains(3)
                ? PublishActionTriggerAsync()
                : Task.CompletedTask;
        }

        public Task NotifyKitchenCancellationAsync(string orderCode) => Task.WhenAll(PublishActionTriggerAsync(), _messageGateway.SendAsync($"{DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)):HH:mm:ss} {orderCode}: Có phiếu huỷ món gửi bếp"));

        public Task NotifyBillCancellationAsync() => PublishActionTriggerAsync();

        private Task PublishActionTriggerAsync()
        {
            var trigger = string.IsNullOrWhiteSpace(_settings.PrintAgentPayloadMessage) ? "2" : _settings.PrintAgentPayloadMessage;
            return PublishSpeakerAsync(trigger);
        }

        public async Task NotifyQrCallWaiterAsync(string table, string? branchCode = null)
        {
            var notification = $"{DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)):HH:mm:ss} Bàn {table?.Trim()} gọi nhân viên";
            await NotifyTableAsync(table, _settings.TopicService, "gọi nhân viên", notification);
        }

        public async Task NotifyQrCallPaymentAsync(string table, string? branchCode = null)
        {
            var notification = $"{DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)):HH:mm:ss} Bàn {table?.Trim()} gọi thanh toán";
            await NotifyTableAsync(table, _settings.TopicPayment, "gọi thanh toán", notification);
        }

        public async Task NotifyQrNewOrderAsync(string table, int itemCount, string? branchCode = null, string? notificationContent = null)
        {
            await NotifyTableAsync(table, _settings.TopicOrder, "gọi món", notificationContent);
        }

        private Task NotifyTableAsync(string tableName, string topic, string action, string? notificationContent = null)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                _logger.LogWarning("Skipping table notification to {Topic} because table name is empty", topic);
                return Task.CompletedTask;
            }

            var actualTableName = tableName.Trim();
            return Task.WhenAll(
                PublishTableNotificationAsync(actualTableName, topic),
                _messageGateway.SendAsync(notificationContent ?? $"Bàn {actualTableName} {action}"));
        }

        private Task PublishTableNotificationAsync(string table, string topic)
        {
            if (string.IsNullOrWhiteSpace(table))
            {
                _logger.LogWarning("Skipping MQTT table notification to {Topic} because table name is empty", topic);
                return Task.CompletedTask;
            }

            return PublishSpeakerAsync(table.Trim().ToUpperInvariant(), topic);
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            try
            {
                if (_mqttClient.IsConnected)
                {
                    _mqttClient.DisconnectAsync().GetAwaiter().GetResult();
                }
                _mqttClient.Dispose();
                _lock.Dispose();
            }
            catch {}
        }
    }
}

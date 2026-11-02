using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AnFood.Application.Services.Mqtt
{
    public interface IMessageGatewayService
    {
        Task SendAsync(string content);
    }

    public class MessageGatewayService : IMessageGatewayService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MessageGatewayService> _logger;

        public MessageGatewayService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<MessageGatewayService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendAsync(string content)
        {
            var settings = _configuration.GetSection("NotificationGateway");
            if (!settings.GetValue<bool>("Enabled")) return;

            var url = settings["Url"];
            var apiKey = settings["ApiKey"];
            var recipient = settings["Recipient"];
            var templateId = settings["TemplateId"];
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrWhiteSpace(recipient) || string.IsNullOrWhiteSpace(templateId))
            {
                _logger.LogWarning("Notification gateway skipped: Url, ApiKey, Recipient or TemplateId is missing");
                return;
            }

            var requestId = "ANFOOD-" + Guid.NewGuid().ToString("N");
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("x-api-key", apiKey);
                request.Content = JsonContent.Create(new
                {
                    partnerRequestId = requestId,
                    recipient,
                    templateId,
                    templateParams = new { content, threadType = settings["ThreadType"] ?? "group" },
                    messageType = settings.GetValue<int>("MessageType", 2),
                    preferredChannel = settings.GetValue<int>("PreferredChannel", 5)
                });

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var response = await _httpClientFactory.CreateClient().SendAsync(request, timeout.Token);
                if (response.IsSuccessStatusCode)
                    _logger.LogInformation("Notification gateway sent {RequestId}: {Content}", requestId, content);
                else
                    _logger.LogWarning("Notification gateway rejected {RequestId}: HTTP {StatusCode}", requestId, (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Notification gateway failed for {RequestId}", requestId);
            }
        }
    }
}

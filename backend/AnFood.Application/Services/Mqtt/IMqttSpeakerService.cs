using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnFood.Application.Services.Mqtt
{
    public interface IMqttSpeakerService
    {
        Task PublishSpeakerAsync(object payload, string? customTopic = null);
        Task NotifyKitchenOrderAsync(string table, string area, int itemCount, IReadOnlyCollection<int> areaTypes, string orderCode, string? note = null);
        Task NotifyKitchenOrderSpeakerAsync(IReadOnlyCollection<int> areaTypes);
        Task NotifyKitchenCancellationAsync(string orderCode);
        Task NotifyBillCancellationAsync();
        Task NotifyQrCallWaiterAsync(string table, string? branchCode = null);
        Task NotifyQrCallPaymentAsync(string table, string? branchCode = null);
        Task NotifyQrNewOrderAsync(string table, int itemCount, string? branchCode = null, string? notificationContent = null);
    }
}

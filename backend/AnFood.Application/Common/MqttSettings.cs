namespace AnFood.Application.Common
{
    public class MqttSettings
    {
        public string Server { get; set; } = "broker.hivemq.com";
        public int Port { get; set; } = 1883;
        public string? User { get; set; }
        public string? Password { get; set; }
        public string? ClientId { get; set; }
        public string TopicSpeaker { get; set; } = "test_loa/state";
        public string TopicOrder { get; set; } = "Speaker/Order";
        public string TopicService { get; set; } = "Speaker/Service";
        public string TopicPayment { get; set; } = "Speaker/Payment";
        public bool EnableSpeaker { get; set; } = true;
        public bool? UseTls { get; set; }
        public string PrintAgentPayloadMessage { get; set; } = "2";
    }
}
